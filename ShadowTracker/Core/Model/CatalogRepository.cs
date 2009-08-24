using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;

namespace Shadow.Model
{
	/// <summary>
	/// Implements a repository pattern for Catalog Entries which
	/// can be backed by a number of different storage mechanisms.
	/// </summary>
	public class CatalogRepository
	{
		#region Fields

		private readonly ITable<CatalogEntry> Entries;

		// TODO: abstract out UnitOfWork from LINQ-to-SQL implementation
		private readonly DataContext UnitOfWork;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <remarks>Defaults to in-memory backing storage</remarks>
		public CatalogRepository()
		{
			this.Entries = new MemoryTable<CatalogEntry>(CatalogEntry.PathComparer);
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="entries">initial items</param>
		public CatalogRepository(IEnumerable<CatalogEntry> entries)
		{
			this.Entries = new MemoryTable<CatalogEntry>(entries, CatalogEntry.PathComparer); ;
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="entries">the backing CatalogEntry storage</param>
		public CatalogRepository(ITable<CatalogEntry> entries)
		{
			this.Entries = entries;
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="db">LINQ-to-SQL DataContext</param>
		public CatalogRepository(DataContext db)
		{
			// TODO: abstract out UnitOfWork from LINQ-to-SQL implementation
			this.UnitOfWork = db;
			this.Entries = new TableAdapter<CatalogEntry>(db);
		}

		#endregion Init

		#region Action Methods

		/// <summary>
		/// Action where does not exist (requires expensive bit transfer).
		/// </summary>
		/// <param name="entry"></param>
		public virtual void AddEntry(CatalogEntry entry)
		{
			this.Entries.Add(entry);
			this.SubmitChanges();
		}

		/// <summary>
		/// Action where bits exist, but need to add or update entry (no transfer required).
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="match"></param>
		public virtual void CloneEntry(CatalogEntry entry, CatalogEntry data)
		{
			this.AddEntry(entry);
		}

		/// <summary>
		/// Action where file is being removed (no transfer required).
		/// </summary>
		/// <param name="path"></param>
		public virtual void DeleteEntryByPath(string path)
		{
			if (!this.Exists(n => n.Path.ToLower() == path.ToLower()))
			{
				if (!path.EndsWith("/"))
				{
					path += "/";
				}

				this.Entries.RemoveWhere(n => n.Path.ToLower().StartsWith(path.ToLower()));
			}
			else
			{
				this.Entries.RemoveWhere(n => n.Path.ToLower() == path.ToLower());
			}

			this.SubmitChanges();
		}

		/// <summary>
		/// Action where is simple move or rename (no transfer required).
		/// </summary>
		/// <param name="oldPath"></param>
		/// <param name="newPath"></param>
		public virtual void RenameEntry(string oldPath, string newPath)
		{
			CatalogEntry entry = this.Entries.FirstOrDefault(n => n.Path == oldPath);
			if (entry == null)
			{
				// TODO: log error
				throw new ArgumentException("Entry was not found: "+oldPath, oldPath);
			}

			entry.Path = newPath;
			this.SubmitChanges();
		}

		/// <summary>
		/// Action where bits are same but metadata different (no transfer required).
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="data">the original entry</param>
		public virtual void UpdateMeta(CatalogEntry entry, CatalogEntry original)
		{
			if (original == null || original.ID < 1)
			{
				this.Entries.Update(entry);
			}
			else
			{
				// TODO: figure out better way to manage DataContext restrictions
				original.Attributes = entry.Attributes;
				original.CreatedDate = entry.CreatedDate;
				//original.ID = entry.ID;
				original.Length = entry.Length;
				original.ModifiedDate = entry.ModifiedDate;
				original.Path = entry.Path;
				original.Signature = entry.Signature;

				//this.Entries.Update(original);
			}

			this.SubmitChanges();
		}

		/// <summary>
		/// Action where bits are different but entry exists (requires expensive bit transfer).
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="data">the entry with source data</param>
		public virtual void UpdateData(CatalogEntry entry, CatalogEntry original, CatalogEntry data)
		{
			if (original == null)
			{
				this.CloneEntry(entry, data);
				return;
			}

			// TODO: handle data changes
			this.UpdateMeta(entry, original);
		}

		private void SubmitChanges()
		{
			// TODO: abstract out UnitOfWork from LINQ-to-SQL implementation
			if (this.UnitOfWork != null)
			{
				this.UnitOfWork.SubmitChanges(ConflictMode.ContinueOnConflict);
			}
		}

		#endregion Action Methods

		#region Query Methods

		/// <summary>
		/// Allows simple checking for existance.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public bool Exists(Expression<Func<CatalogEntry, bool>> predicate)
		{
			return this.Entries.Any(predicate);
		}

		public IQueryable<string> GetExistingPaths()
		{
			return this.Entries.Select(n => n.Path);
		}

		// TODO: abstract out UnitOfWork from LINQ-to-SQL implementation
		protected void SetUnitOfWorkLog(System.IO.TextWriter writer)
		{
			this.UnitOfWork.Log = writer;
		}

		#endregion Query Methods

		#region Delta Methods

		[Flags]
		private enum MatchRank : int
		{
			None = 0x00,
			Hash = 0x01,
			Path = 0x02,
			Both = Path|Hash
		}

		/// <summary>
		/// Finds the nearest matching existing CatalogEntry.
		/// Exact match (path & hash) is best.
		/// A matching path is next (however requires expensive copying of bits).
		/// Same file signature is next (can clone).
		/// </summary>
		/// <param name="path"></param>
		/// <param name="hash"></param>
		/// <param name="match"></param>
		/// <returns></returns>
		private MatchRank FindMatch(CatalogEntry target, out CatalogEntry meta, out CatalogEntry data)
		{
			string path = target.Path != null ? target.Path.ToLowerInvariant() : null;
			string hash = target.Signature != null ? target.Signature.ToLowerInvariant() : null;

			var query =
				(from entry in this.Entries
				 let rank =
					(int)(entry.Path.ToLower() == path ? MatchRank.Path : MatchRank.None) |
					(int)((entry.Signature != null && entry.Signature.ToLower() == hash) ? MatchRank.Hash : MatchRank.None)
				 where rank > (int)MatchRank.None
				 orderby rank descending
				 select new
				 {
					 Rank = (MatchRank)rank,
					 Entry = entry
				 }).Take(2);

			var result = query.ToArray();
			if (result == null || result.Length < 1)
			{
				meta = data = null;
				return MatchRank.None;
			}

			switch (result[0].Rank)
			{
				case MatchRank.Both:
				{
					meta = data = result[0].Entry;
					return MatchRank.Both;
				}
				case MatchRank.Path:
				{
					meta = result[0].Entry;
					data = (result.Length > 1) ? result[1].Entry : null;
					return MatchRank.Path;
				}
				case MatchRank.Hash:
				{
					meta = null;
					data = result[0].Entry;
					return MatchRank.Hash;
				}
				default:
				{
					// this should not happen
					throw new InvalidOperationException("Unexpected result from FindMatch query.");
				}
			}
		}

		protected DeltaAction CalcEntryDelta(CatalogEntry entry, out CatalogEntry meta, out CatalogEntry data)
		{
			if (entry == null)
			{
				meta = data = null;
				return DeltaAction.None;
			}

			// look for closest matching node
			switch(this.FindMatch(entry, out meta, out data))
			{
				case MatchRank.Both:
				{
					if (CatalogEntry.ValueComparer.Equals(entry, meta))
					{
						// no changes, identical
						return DeltaAction.None;
					}

					// correct bits exist at correct path but metadata is different
					return DeltaAction.Meta;
				}
				case MatchRank.Hash:
				{
					if (entry.IsDirectory)
					{
						// add empty directories (no data to clone)
						return DeltaAction.Add;
					}

					// equivalent file found
					return DeltaAction.Clone;
				}
				case MatchRank.Path:
				{
					// file exists but bits are different
					return DeltaAction.Update;
				}
				default:
				case MatchRank.None:
				{
					// completely missing file, need to add
					return DeltaAction.Add;
				}
			}
		}

		public void ApplyChanges(CatalogEntry entry)
		{
			CatalogEntry data, meta;
			switch (this.CalcEntryDelta(entry, out meta, out data))
			{
				case DeltaAction.Add:
				{
					// does not exist (requires expensive bit transfer)
					this.AddEntry(entry);
					break;
				}
				case DeltaAction.Clone:
				{
					// bits exist need to add or update entry (no transfer required)
					this.CloneEntry(entry, data);
					break;
				}
				case DeltaAction.Delete:
				{
					// NOTE: this actually would not be detected here

					// file is being removed
					this.DeleteEntryByPath(entry.Path);
					break;
				}
				case DeltaAction.Meta:
				{
					// bits are same but metadata different
					if (meta == null)
					{
						this.AddEntry(entry);
						break;
					}

					this.UpdateMeta(entry, meta);
					break;
				}
				case DeltaAction.Update:
				{
					// path exists but bits are different (requires expensive bit transfer)
					this.UpdateData(entry, meta, data);
					break;
				}
				default:
				case DeltaAction.None:
				{
					// no change required
					break;
				}
			}
		}

		#endregion Delta Methods

		#region Catalog Sync Methods

		public void Sync(CatalogRepository that)
		{
			// apply any deltas since last sync
			foreach (CatalogEntry entry in that.Entries)
			{
				this.ApplyChanges(entry);
			}

			// NOTE: always perform deletes last, so that
			// moves or renames can be expressed as a clone/delete

			foreach (string path in this.GetExistingPaths())
			{
				// extras are any local entries not contained in other
				if (that.Exists(n => n.Path.ToLower() == path.ToLower()))
				{
					continue;
				}

				this.DeleteEntryByPath(path);
			}
		}

		#endregion Catalog Sync Methods
	}
}
