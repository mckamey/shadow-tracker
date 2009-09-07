using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shadow.Agent;

namespace Shadow.Model
{
	/// <summary>
	/// Implements a repository pattern for CatalogEntry which
	/// can be backed by a number of different storage mechanisms.
	/// </summary>
	public class CatalogRepository
	{
		#region Fields

		private readonly IUnitOfWork UnitOfWork;
		private readonly Catalog Catalog;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="unitOfWork">unit of work</param>
		public CatalogRepository(IUnitOfWork unitOfWork, string name, string rootPath)
		{
			if (unitOfWork == null)
			{
				throw new ArgumentNullException("unitOfWork", "IUnitOfWork was null.");
			}
			if (String.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("name", "Catalog name was empty.");
			}
			if (String.IsNullOrEmpty(rootPath))
			{
				throw new ArgumentNullException("rootPath", "Catalog path was empty.");
			}

			this.UnitOfWork = unitOfWork;

			rootPath = rootPath.ToLower();
			this.Catalog = CatalogRepository.EnsureCatalog(unitOfWork, name, rootPath);
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="unitOfWork">unit of work</param>
		public CatalogRepository(IUnitOfWork unitOfWork, Catalog catalog)
		{
			if (unitOfWork == null)
			{
				throw new ArgumentNullException("unitOfWork", "IUnitOfWork was null.");
			}
			if (catalog == null)
			{
				throw new ArgumentNullException("catalog", "Catalog was null.");
			}

			this.UnitOfWork = unitOfWork;
			this.Catalog = catalog;
		}

		#endregion Init

		#region Action Methods

		/// <summary>
		/// Action where does not exist (requires expensive bit transfer).
		/// </summary>
		/// <param name="entry"></param>
		public virtual void AddEntry(CatalogEntry entry)
		{
			entry.CatalogID = this.Catalog.ID;
			this.UnitOfWork.Entries.Add(entry);
		}

		/// <summary>
		/// Action where bits exist, but need to add or update entry (no transfer required).
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="match"></param>
		public virtual void CloneEntry(CatalogEntry entry, CatalogEntry data)
		{
			// TODO: expose this differently
			this.AddEntry(entry);
		}

		public virtual void DeleteEntryByPath(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException("path", "Path is empty");
			}

			string parent, name;
			FileUtility.SplitPath(path, out parent, out name);

			this.DeleteEntryByPath(parent, name);
		}

		/// <summary>
		/// Action where file is being removed (no transfer required).
		/// </summary>
		/// <param name="path"></param>
		public virtual void DeleteEntryByPath(string parent, string name)
		{
			ITable<CatalogEntry> entries = this.UnitOfWork.Entries;
			entries.RemoveWhere(n =>
				(n.CatalogID == this.Catalog.ID) &&
				(n.Parent.ToLower() == parent.ToLower()) &&
				(n.Name.ToLower() == name.ToLower()));

			// if is directory with children then remove them as well
			string asDir = parent+name+'/';

			entries.RemoveWhere(n => n.Parent.ToLower().StartsWith(asDir.ToLower()));
		}

		/// <summary>
		/// Action where is simple move or rename (no transfer required).
		/// </summary>
		/// <param name="oldPath"></param>
		/// <param name="newPath"></param>
		public virtual void RenameEntry(string oldPath, string newPath)
		{
			ITable<CatalogEntry> entries = this.UnitOfWork.Entries;

			string oldParent, oldName, newParent, newName;
			FileUtility.SplitPath(oldPath, out oldParent, out oldName);

			CatalogEntry entry = entries.FirstOrDefault(n =>
				(n.CatalogID == this.Catalog.ID) &&
				(n.Name == oldName) &&
				(n.Parent == oldParent));

			if (entry == null)
			{
				// TODO: log error
				throw new ArgumentException("Entry was not found: "+oldPath, "oldPath");
			}

			FileUtility.SplitPath(newPath, out newParent, out newName);
			entry.Parent = newParent;
			entry.Name = newName;
		}

		/// <summary>
		/// Action where bits are same but metadata different (no transfer required).
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="data">the original entry</param>
		public virtual void UpdateMeta(CatalogEntry entry, CatalogEntry original)
		{
			ITable<CatalogEntry> entries = this.UnitOfWork.Entries;
			if (original == null || original.ID < 1)
			{
				entries.Update(entry);
			}
			else
			{
				original.CopyValuesFrom(entry);

				//entries.Update(original);
			}
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

		#endregion Action Methods

		#region Query Methods

		public static Catalog EnsureCatalog(IUnitOfWork unitOfWork, string name, string path)
		{
			if (String.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("name", "Catalog name was empty.");
			}
			if (String.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException("path", "Catalog path was empty.");
			}

			path = Shadow.Agent.FileUtility.EnsureTrailingSlash(path);

			// TODO do the ranked order by for this query so only query once
			Catalog catalog = unitOfWork.Catalogs.Where(c => c.Name == name.ToLower()).FirstOrDefault();
			if (catalog != null)
			{
				if (!StringComparer.OrdinalIgnoreCase.Equals(catalog.Path, path))
				{
					// update the root to match
					catalog.Path = path;
					unitOfWork.Save();
				}
			}
			else
			{
				catalog = unitOfWork.Catalogs.Where(c => c.Path == path.ToLower()).FirstOrDefault();
				if (catalog != null)
				{
					// update the name to match
					catalog.Name = name;
					unitOfWork.Save();
				}
			}

			if (catalog == null)
			{
				catalog = new Catalog();
				catalog.Name = name;
				catalog.Path = path;
				unitOfWork.Catalogs.Add(catalog);
				unitOfWork.Save();
			}
			return catalog;
		}

		/// <summary>
		/// Allows simple checking for existance.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public bool Exists(Expression<Func<CatalogEntry, bool>> predicate)
		{
			ITable<CatalogEntry> entries = this.UnitOfWork.Entries;
			return entries
				.Where(n => n.CatalogID == this.Catalog.ID)
				.Any(predicate);
		}

		public IEnumerable<string> GetChildPaths(string parent)
		{
			if (String.IsNullOrEmpty(parent))
			{
				return this.GetExistingPaths();
			}

			parent = parent.ToLowerInvariant();
			if (!parent.EndsWith("/"))
			{
				parent += "/";
			}

			ITable<CatalogEntry> entries = this.UnitOfWork.Entries;
			return entries
				.Where(n => (n.CatalogID == this.Catalog.ID) && n.Parent.ToLower().StartsWith(parent))
				.Select(n => n.Parent+n.Name);
		}

		public IQueryable<string> GetExistingPaths()
		{
			ITable<CatalogEntry> entries = this.UnitOfWork.Entries;
			return entries
				.Where(n => n.CatalogID == this.Catalog.ID)
				.Select(n => n.Parent+n.Name);
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
			string name = target.Name != null ? target.Name.ToLowerInvariant() : null;
			string parent = target.Parent != null ? target.Parent.ToLowerInvariant() : null;
			string path = parent+name;
			string hash = target.Signature != null ? target.Signature.ToLowerInvariant() : null;
			long catalogID = this.Catalog.ID;
			ITable<CatalogEntry> entries = this.UnitOfWork.Entries;

			var query =
				(from entry in entries
				 where entry.CatalogID == catalogID
				 let rank =
					(int)(entry.Parent.ToLower()+entry.Name.ToLower() == path ? MatchRank.Path : MatchRank.None) |
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

		/// <summary>
		/// Checks for changes.
		/// </summary>
		/// <param name="entry"></param>
		/// <returns>true if changes were found</returns>
		public bool ApplyChanges(CatalogEntry entry)
		{
			CatalogEntry data, meta;

			switch (this.CalcEntryDelta(entry, out meta, out data))
			{
				case DeltaAction.Add:
				{
					// does not exist (requires expensive bit transfer)
					this.AddEntry(entry);
					return true;
				}
				case DeltaAction.Clone:
				{
					// bits exist need to add or update entry (no transfer required)
					this.CloneEntry(entry, data);
					return true;
				}
				case DeltaAction.Delete:
				{
					// NOTE: this actually would not be detected here

					// file is being removed
					this.DeleteEntryByPath(entry.Name);
					return true;
				}
				case DeltaAction.Meta:
				{
					// bits are same but metadata different
					if (meta == null)
					{
						this.AddEntry(entry);
					}
					else
					{
						this.UpdateMeta(entry, meta);
					}
					return true;
				}
				case DeltaAction.Update:
				{
					// path exists but bits are different (requires expensive bit transfer)
					this.UpdateData(entry, meta, data);
					return true;
				}
				default:
				case DeltaAction.None:
				{
					// no change required
					return false;
				}
			}
		}

		#endregion Delta Methods

		#region Catalog Sync Methods

		public void Sync(CatalogRepository that)
		{
			// TODO: reconcile this with trickle-updates

			// apply any deltas since last sync
			foreach (CatalogEntry entry in that.UnitOfWork.Entries.Where(n => n.CatalogID == this.Catalog.ID))
			{
				this.ApplyChanges(entry);
			}

			// NOTE: always perform deletes last, so that
			// moves or renames can be expressed as a clone/delete

			foreach (string path in this.GetExistingPaths())
			{
				// extras are any local entries not contained in other
				if (that.Exists(n => n.Name.ToLower() == path.ToLower()))
				{
					continue;
				}

				this.DeleteEntryByPath(path);
			}
		}

		#endregion Catalog Sync Methods
	}
}
