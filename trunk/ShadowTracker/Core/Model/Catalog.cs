using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Shadow.Model
{
	/// <summary>
	/// Implements a repository pattern for Catalog Entries which
	/// can be backed by a number of different storage mechanisms.
	/// </summary>
	public class Catalog
	{
		#region Fields

		private readonly ITable<CatalogEntry> Entries;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="entries">the backing CatalogEntry storage</param>
		public Catalog(ITable<CatalogEntry> entries)
		{
			this.Entries = entries;
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
		}

		/// <summary>
		/// Action where bits exist, but need to add or update entry (no transfer required).
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="match"></param>
		public virtual void CloneEntry(CatalogEntry entry, CatalogEntry match)
		{
			this.Entries.Add(entry);
		}

		/// <summary>
		/// Action where bits are same but metadata different (no transfer required).
		/// </summary>
		/// <param name="entry"></param>
		public virtual void UpdateMetaData(CatalogEntry entry)
		{
			this.Entries.Update(entry);
		}

		/// <summary>
		/// Action where bits are different but entry exists (requires expensive bit transfer).
		/// </summary>
		/// <param name="entry"></param>
		public virtual void UpdateData(CatalogEntry entry)
		{
			this.Entries.Update(entry);
		}

		/// <summary>
		/// Action where file is being removed (no transfer required).
		/// </summary>
		/// <param name="path"></param>
		public virtual void DeleteEntryByPath(string path)
		{
			this.Entries.RemoveWhere(n => n.Path == path);
		}

		/// <summary>
		/// Action where is simple move or rename (no transfer required).
		/// </summary>
		/// <param name="oldPath"></param>
		/// <param name="newPath"></param>
		public virtual void FastMoveByPath(string oldPath, string newPath)
		{
			CatalogEntry entry = this.GetEntryAtPath(oldPath);
			if (entry == null)
			{
				// TODO: log error
				throw new ArgumentException("Entry was not found: "+oldPath, oldPath);
			}

			entry.Path = newPath;
			this.Entries.Update(entry);
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

		#endregion Query Methods

		#region Delta Methods

		[Flags]
		private enum MatchRank
		{
			None = 0x00,
			Path = 0x01,
			Hash = 0x02,
			Both = Path|Hash
		}

		/// <summary>
		/// Finds the nearest matching existing CatalogEntry.
		/// Exact match (path & hash) is best.
		/// Same file signature is next (can clone).
		/// Finally a matching path (requires expensive copying of bits).
		/// </summary>
		/// <param name="path"></param>
		/// <param name="hash"></param>
		/// <param name="match"></param>
		/// <returns></returns>
		private MatchRank FindMatch(string path, string hash, out CatalogEntry match)
		{
			var query =
				from entry in this.Entries
				let rank =
					(entry.Path == path ? MatchRank.Path : MatchRank.None) |
					(entry.Signature == hash ? MatchRank.Hash : MatchRank.Path)
				where rank > 0
				orderby rank descending
				select new
				{
					Rank = rank,
					Entry = entry
				};

			var result = query.FirstOrDefault();
			if (result == null || result.Entry == null)
			{
				match = null;
				return MatchRank.None;
			}

			match = result.Entry;
			return result.Rank;
		}

		private CatalogEntry GetEntryAtPath(string path)
		{
			return this.Entries.FirstOrDefault(n => n.Path == path);
		}

		protected DeltaAction CalcEntryDelta(CatalogEntry entry, out CatalogEntry match)
		{
			if (entry == null)
			{
				match = null;
				return DeltaAction.None;
			}

			// look for closest matching node
			switch(this.FindMatch(entry.Path, entry.Signature, out match))
			{
				case MatchRank.Hash:
				{
					if (entry.IsDirectory)
					{
						// need to add empty directory
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
				case MatchRank.Both:
				{
					if (entry.Equals(match))
					{
						// no changes, identical
						return DeltaAction.None;
					}

					// correct bits exist at correct path but metadata is different
					return DeltaAction.Meta;
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
			CatalogEntry match;
			switch (this.CalcEntryDelta(entry, out match))
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
					this.CloneEntry(entry, match);
					break;
				}
				case DeltaAction.Delete:
				{
					// NOTE: this actually wouldn't be detected here

					// file is being removed
					this.DeleteEntryByPath(entry.Path);
					break;
				}
				case DeltaAction.Meta:
				{
					// bits are same but metadata different
					this.UpdateMetaData(entry);
					break;
				}
				case DeltaAction.Update:
				{
					// bits are different but exists (requires expensive bit transfer)
					this.UpdateData(entry);
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

		public void Sync(Catalog that)
		{
			// apply any deltas since last sync
			foreach (CatalogEntry entry in that.Entries)
			{
				this.ApplyChanges(entry);
			}

			// NOTE: always perform deletes last, so that
			// moves or renames can be expressed as a clone/delete

			foreach (CatalogEntry entry in this.Entries)
			{
				// extras are any local entries not contained in other
				if (that.Entries.Where(n => n.Path == entry.Path).Any())
				{
					continue;
				}

				this.DeleteEntryByPath(entry.Path);
			}
		}

		#endregion Catalog Sync Methods
	}
}
