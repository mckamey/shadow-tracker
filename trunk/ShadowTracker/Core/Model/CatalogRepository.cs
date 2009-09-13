using System;
using System.Collections.Generic;
using System.IO;
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
			string asDir = (parent+name+'/').ToLowerInvariant();

			entries.RemoveWhere(n => n.Parent.ToLower().StartsWith(asDir));
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
		public virtual void Update(CatalogEntry entry, CatalogEntry original)
		{
			ITable<CatalogEntry> entries = this.UnitOfWork.Entries;
			if (original == null || original.ID < 1)
			{
				entries.Update(entry);
			}
			else
			{
				original.CopyValuesFrom(entry);
			}
		}

		#endregion Action Methods

		#region Query Methods

		[Flags]
		private enum MatchRank : int
		{
			None = 0x00,
			Path = 0x01,
			Name = 0x02,
			Both = Name|Path
		}

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

			path = FileUtility.EnsureTrailingSlash(path);

			string nameLower = name.ToLowerInvariant();
			string pathLower = path.ToLowerInvariant();

			var query =
				from c in unitOfWork.Catalogs
				let rank =
					(int)((c.Name.ToLower() == name) ? MatchRank.Name : MatchRank.None) |
					(int)(c.Path.ToLower() == path ? MatchRank.Path : MatchRank.None)
				where rank > (int)MatchRank.None
				orderby rank descending
				select c;

			Catalog catalog = query.FirstOrDefault();
			if (catalog != null)
			{
				if (!StringComparer.OrdinalIgnoreCase.Equals(catalog.Path, path) ||
					!StringComparer.OrdinalIgnoreCase.Equals(catalog.Name, name))
				{
					// update to match
					catalog.Name = name;
					catalog.Path = path;
					unitOfWork.Save();
				}
			}
			else
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
		/// Finds the first matching CatalogEntry.
		/// </summary>
		/// <param name="predicate">an expression to look up</param>
		/// <returns></returns>
		public CatalogEntry FindEntry(Expression<Func<CatalogEntry, bool>> predicate)
		{
			long catalogID = this.Catalog.ID;
			ITable<CatalogEntry> entries = this.UnitOfWork.Entries;
			return entries
				.Where(n => n.CatalogID == catalogID)
				.FirstOrDefault(predicate);
		}

		/// <summary>
		/// Finds the first matching CatalogEntry.
		/// </summary>
		/// <param name="path">the full path to look up</param>
		/// <returns></returns>
		public CatalogEntry FindEntry(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException("path", "path was empty");
			}

			path = path.ToLowerInvariant();

			return this.FindEntry(n => n.Parent.ToLower()+n.Name.ToLower() == path);
		}

		/// <summary>
		/// Allows simple checking for existance.
		/// </summary>
		/// <param name="predicate">an expression to check existance</param>
		/// <returns></returns>
		public bool EntryExists(Expression<Func<CatalogEntry, bool>> predicate)
		{
			long catalogID = this.Catalog.ID;
			ITable<CatalogEntry> entries = this.UnitOfWork.Entries;
			return entries
				.Where(n => n.CatalogID == catalogID)
				.Any(predicate);
		}

		/// <summary>
		/// Allows simple checking for existance by path.
		/// </summary>
		/// <param name="path">the full path to look up</param>
		/// <returns></returns>
		public bool EntryExists(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException("path", "path was empty");
			}

			path = path.ToLowerInvariant();

			return this.EntryExists(n => n.Parent.ToLower()+n.Name.ToLower() == path);
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

		/// <summary>
		/// Checks for changes.
		/// </summary>
		/// <param name="entry"></param>
		/// <returns>true if changes were found</returns>
		internal bool ApplyChanges(CatalogEntry entry)
		{
			return this.ApplyChanges(entry, null);
		}

		/// <summary>
		/// Checks for changes.
		/// </summary>
		/// <param name="entry"></param>
		/// <returns>true if changes were found</returns>
		internal bool ApplyChanges(CatalogEntry entry, FileInfo file)
		{
			if (entry == null)
			{
				return false;
			}

			// look for entry with matching path
			CatalogEntry original = this.FindEntry(entry.FullPath);
			if (original == null)
			{
				// ensure hash has been calculated
				if (!entry.HasSignature)
				{
					if (file == null)
					{
						throw new ArgumentNullException("file", "FileInfo was missing for CatalogEntry without signature.");
					}

					entry.Signature = FileHash.ComputeHash(file);
				}

				// entry does not exist
				// if bits exist need to add or update entry (no transfer required)
				// else requires expensive bit transfer
				this.AddEntry(entry);
				return true;
			}

			if (entry.HasSignature)
			{
				if (CatalogEntry.ValueComparer.Equals(entry, original))
				{
					// no changes, identical
					return false;
				}
			}
			else
			{
				if (CatalogEntry.LiteValueComparer.Equals(entry, original))
				{
					// no changes, identical (except possibly Signature)
					return false;
				}

				// ensure hash has been calculated
				if (file == null)
				{
					throw new ArgumentNullException("file", "FileInfo was missing for CatalogEntry without signature.");
				}

				entry.Signature = FileHash.ComputeHash(file);
			}

			// file exists at correct path but metadata is different
			this.Update(entry, original);
			return true;
		}

		#endregion Delta Methods

		#region Catalog Sync Methods

		public void Sync(CatalogRepository that)
		{
			// TODO: reconcile this with trickle-updates?

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
				if (that.EntryExists(path))
				{
					continue;
				}

				this.DeleteEntryByPath(path);
			}
		}

		#endregion Catalog Sync Methods
	}
}
