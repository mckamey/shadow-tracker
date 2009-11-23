using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

using IgnorantPersistence;
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
		private ITable<Catalog> catalogs;
		private ITable<CatalogEntry> entries;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="unitOfWork">unit of work</param>
		public CatalogRepository(IUnitOfWork unitOfWork)
		{
			if (unitOfWork == null)
			{
				throw new ArgumentNullException("unitOfWork", "IUnitOfWork was null.");
			}

			this.UnitOfWork = unitOfWork;
		}

		#endregion Init

		#region Properties

		public ITable<Catalog> Catalogs
		{
			get
			{
				if (this.catalogs == null)
				{
					this.catalogs = this.UnitOfWork.GetTable<Catalog>();
				}
				return this.catalogs;
			}
		}

		public ITable<CatalogEntry> Entries
		{
			get
			{
				if (this.entries == null)
				{
					this.entries = this.UnitOfWork.GetTable<CatalogEntry>();
				}
				return this.entries;
			}
		}

		#endregion Properties

		#region Action Methods

		public virtual void DeleteEntryByPath(long catalogID, string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException("path", "Path is empty");
			}

			string parent, name;
			FileUtility.SplitPath(path, out parent, out name);

			this.DeleteEntryByPath(catalogID, parent, name);
		}

		/// <summary>
		/// Action where file is being removed (no transfer required).
		/// </summary>
		/// <param name="path"></param>
		public virtual void DeleteEntryByPath(long catalogID, string parent, string name)
		{
			this.Entries.RemoveWhere(n =>
				(n.CatalogID == catalogID) &&
				(n.Parent.ToLower() == parent.ToLower()) &&
				(n.Name.ToLower() == name.ToLower()));

			// if is directory with children then remove them as well
			string asDir = (parent+name+'/').ToLowerInvariant();

			this.Entries.RemoveWhere(n => n.Parent.ToLower().StartsWith(asDir));
		}

		/// <summary>
		/// Action where is simple move or rename (no transfer required).
		/// </summary>
		/// <param name="oldPath"></param>
		/// <param name="newPath"></param>
		public virtual void RenameEntry(long catalogID, string oldPath, string newPath)
		{
			string oldParent, oldName;
			FileUtility.SplitPath(oldPath, out oldParent, out oldName);

			CatalogEntry entry = this.Entries.FirstOrDefault(n =>
				(n.CatalogID == catalogID) &&
				(n.Name == oldName) &&
				(n.Parent == oldParent));

			if (entry == null)
			{
				// TODO: log error
				throw new ArgumentException("Entry was not found: "+oldPath, "oldPath");
			}

			string newParent, newName;
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
			if (original == null || original.ID < 1)
			{
				this.Entries.Update(entry);
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

		/// <summary>
		/// Finds or creates a matching catalog
		/// </summary>
		/// <param name="unitOfWork"></param>
		/// <param name="name"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public Catalog FindOrCreateCatalog(string name, string path)
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
				from c in this.Catalogs
				let rank =
					(int)((c.Name.ToLower() == name) ? MatchRank.Name : MatchRank.None) |
					(int)(c.Path.ToLower() == path ? MatchRank.Path : MatchRank.None)
				where rank > (int)MatchRank.None
				orderby rank descending
				select c;

			Catalog catalog = query.FirstOrDefault();
			if (catalog == null)
			{
				catalog = new Catalog();
				catalog.Name = name;
				catalog.Path = path;
				this.Catalogs.Add(catalog);
				this.UnitOfWork.Save();
			}
			else if (!StringComparer.OrdinalIgnoreCase.Equals(catalog.Path, path))
			{
				// update to match
				catalog.Path = path;
				this.UnitOfWork.Save();
			}
			else if (!StringComparer.OrdinalIgnoreCase.Equals(catalog.Name, name))
			{
				// update to match
				catalog.Name = name;
				this.UnitOfWork.Save();
			}

			return catalog;
		}

		/// <summary>
		/// Finds the first matching CatalogEntry.
		/// </summary>
		/// <param name="predicate">an expression to look up</param>
		/// <returns></returns>
		public CatalogEntry FindEntry(long catalogID, Expression<Func<CatalogEntry, bool>> predicate)
		{
			return
				(from n in this.Entries
				 where n.CatalogID == catalogID
				 select n).FirstOrDefault(predicate);
		}

		/// <summary>
		/// Finds the first matching CatalogEntry.
		/// </summary>
		/// <param name="path">the full path to look up</param>
		/// <returns></returns>
		public CatalogEntry FindEntry(long catalogID, string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException("path", "path was empty");
			}

			path = path.ToLowerInvariant();

			return this.FindEntry(catalogID, n => n.Parent.ToLower()+n.Name.ToLower() == path);
		}

		/// <summary>
		/// Allows simple checking for existance.
		/// </summary>
		/// <param name="predicate">an expression to check existance</param>
		/// <returns></returns>
		public bool EntryExists(long catalogID, Expression<Func<CatalogEntry, bool>> predicate)
		{
			return
				(from n in this.Entries
				 where n.CatalogID == catalogID
				 select n).Any(predicate);
		}

		/// <summary>
		/// Allows simple checking for existance by path.
		/// </summary>
		/// <param name="path">the full path to look up</param>
		/// <returns></returns>
		public bool EntryExists(long catalogID, string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException("path", "path was empty");
			}

			path = path.ToLowerInvariant();

			return this.EntryExists(catalogID, n => n.Parent.ToLower()+n.Name.ToLower() == path);
		}

		public IEnumerable<string> GetChildPaths(long catalogID, string parent)
		{
			if (String.IsNullOrEmpty(parent))
			{
				return this.GetExistingPaths(catalogID);
			}

			parent = parent.ToLowerInvariant();
			if (!parent.EndsWith("/"))
			{
				parent += "/";
			}

			return
				from n in this.Entries
				where
					catalogID == n.CatalogID &&
					n.Parent.ToLower().StartsWith(parent)
				select n.Parent + n.Name;
		}

		public IQueryable<string> GetExistingPaths(long catalogID)
		{
			return
				from n in this.Entries
				where catalogID == n.CatalogID
				select n.Parent + n.Name;
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
			CatalogEntry original = this.FindEntry(entry.CatalogID, entry.FullPath);
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
				this.Entries.Add(entry);
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
			foreach (Catalog catalog in that.Catalogs)
			{
				// apply any deltas since last sync
				foreach (CatalogEntry entry in that.Entries.Where(n => n.CatalogID == catalog.ID))
				{
					this.ApplyChanges(entry);
				}

				// NOTE: always perform deletes last, so that
				// moves or renames can be expressed as a clone/delete

				foreach (string path in this.GetExistingPaths(catalog.ID))
				{
					// extras are any local entries not contained in other
					if (that.EntryExists(catalog.ID, path))
					{
						continue;
					}

					this.DeleteEntryByPath(catalog.ID, path);
				}
			}
		}

		#endregion Catalog Sync Methods

		#region Version Methods

		public VersionHistory GetVersionInfo()
		{
			return
				(from v in this.UnitOfWork.GetTable<VersionHistory>()
				 orderby v.ID descending
				 select v).FirstOrDefault();
		}

		public void StoreVersionInfo()
		{
			this.UnitOfWork.GetTable<VersionHistory>().Add(VersionHistory.Create());
		}

		#endregion Version Methods

		#region UnitOfWork.Save

		public void Save()
		{
			this.UnitOfWork.Save();
		}

		#endregion UnitOfWork.Save
	}
}
