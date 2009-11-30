using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

using IgnorantPersistence;
using Shadow.Agent;

namespace Shadow.Model
{
	internal static class CatalogEntryQueryExtensions
	{
		public static IQueryable<CatalogEntry> FindByPath(this IQueryable<CatalogEntry> table, long catalogID, string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException("path", "path was empty");
			}

			path = path.ToLowerInvariant();

			return
				(from n in table
				 where
					n.CatalogID == catalogID &&
					path == n.Parent.ToLower()+n.Name.ToLower()
				 select n);
		}
	}

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
			Trace.TraceInformation("Delete Entry: \"{0}{1}\"", parent, name);
			parent = parent.ToLowerInvariant();
			name = name.ToLowerInvariant();

			this.Entries.RemoveWhere(n =>
				(n.CatalogID == catalogID) &&
				(n.Parent.ToLower() == parent) &&
				(n.Name.ToLower() == name));

			// if is directory with children then remove them as well
			string asDir = parent+name+'/';

			Trace.TraceInformation("Delete Entries: \"{0}*\"", asDir);
			this.Entries.RemoveWhere(n => n.CatalogID == catalogID && n.Parent.ToLower().StartsWith(asDir));
		}

		/// <summary>
		/// Action where is simple move or rename (no transfer required).
		/// </summary>
		/// <param name="oldPath"></param>
		/// <param name="newPath"></param>
		public virtual bool MoveEntry(long catalogID, string oldPath, string newPath)
		{
			string oldParent, oldName;
			FileUtility.SplitPath(oldPath, out oldParent, out oldName);

			CatalogEntry entry = this.Entries.FirstOrDefault(n =>
				(n.CatalogID == catalogID) &&
				(n.Name == oldName) &&
				(n.Parent == oldParent));

			if (entry == null)
			{
				Trace.TraceError("Entry Missing: \"{0}\"", oldPath);
				//throw new ArgumentException("Entry was not found: "+oldPath, "oldPath");
				return false;
			}

			Trace.TraceInformation("Rename Entry: \"{0}\" to \"{1}\"", oldPath, newPath);

			string newParent, newName;
			FileUtility.SplitPath(newPath, out newParent, out newName);
			entry.Parent = newParent;
			entry.Name = newName;

			if (!entry.IsDirectory)
			{
				return true;
			}

			// if is directory with children then move them as well
			string asDir = oldPath+'/';

			var children =
				from n in this.Entries
				where
					n.CatalogID == catalogID &&
					n.Parent.ToLower().StartsWith(asDir)
				select n;

			Trace.TraceInformation("Rename Entries: \"{0}*\"", asDir);
			foreach (CatalogEntry child in children)
			{
				child.Parent = newPath+child.Parent.Substring(oldPath.Length);
			}
			return true;
		}

		/// <summary>
		/// Action where bits are same but metadata different (no transfer required).
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="data">the original entry</param>
		public virtual void Update(CatalogEntry entry, CatalogEntry original)
		{
			Trace.TraceInformation("Update Entry: \"{0}\"", entry.FullPath);

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

			const int NONE = 0x00;
			const int PATH = 0x01;
			const int NAME = 0x02;

			var query =
				from c in this.Catalogs
				let rank =
					((c.Name.ToLower() == name) ? NAME : NONE) |
					(c.Path.ToLower() == path ? PATH : NONE)
				where rank > NONE
				orderby rank descending
				select c;

			Catalog catalog = query.FirstOrDefault();
			if (catalog == null)
			{
				catalog = new Catalog();
				catalog.Name = name;
				catalog.Path = path;

				Trace.TraceInformation("Add Catalog: \"{0}\"", catalog.Path);
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
			return this.Entries.Where(n => n.CatalogID == catalogID).FirstOrDefault(predicate);
		}

		/// <summary>
		/// Finds the first matching CatalogEntry.
		/// </summary>
		/// <param name="path">the full path to look up</param>
		/// <returns></returns>
		public CatalogEntry FindEntry(long catalogID, string path)
		{
			return this.Entries.FindByPath(catalogID, path).FirstOrDefault();
		}

		/// <summary>
		/// Finds the closest matching entry
		/// </summary>
		/// <param name="entry"></param>
		/// <returns></returns>
		protected CatalogEntry FindDeletedEntry(CatalogEntry entry)
		{
			long catalogID = entry.CatalogID;
			string parent = (entry.Parent??String.Empty).ToLowerInvariant();
			string name = (entry.Name??String.Empty).ToLowerInvariant();
			string signature = entry.HasSignature && !entry.IsDirectory ? entry.Signature.ToLowerInvariant() : null;

			var softDelete = this.Entries as ISoftDeleteTable<CatalogEntry>;
			var table = (softDelete == null) ? this.Entries : softDelete.AllItems;

			const int NONE = 0x00; // no match
			const int PART = 0x01; // paths partially match
			const int NAME = 0x02; // paths partially match
			const int HASH = 0x04; // signatures match

			var query =
				from n in table
				where
					n.CatalogID == catalogID &&
					n.DeletedDate.HasValue
				let rank =
				    (signature != null && (n.Signature.ToLower() == signature) ? HASH : NONE) |
				    ((n.Parent.ToLower() == parent) ? PART : NONE) |
				    ((n.Name.ToLower() == name) ? NAME : NONE)
				where rank > NONE
				orderby
					rank descending,
					n.DeletedDate descending
				select n;

			return query.FirstOrDefault();
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
		internal bool AddOrUpdate(CatalogEntry entry)
		{
			return this.AddOrUpdate(entry, null);
		}

		/// <summary>
		/// Checks for changes.
		/// </summary>
		/// <param name="entry"></param>
		/// <returns>true if changes were found</returns>
		internal bool AddOrUpdate(CatalogEntry entry, FileInfo file)
		{
			if (entry == null)
			{
				return false;
			}

			// look for entry with matching path
			CatalogEntry original = this.FindEntry(entry.CatalogID, entry.FullPath);
			if (original == null)
			{
				original = this.FindDeletedEntry(entry);
				if (original != null)
				{
					Trace.TraceInformation("Found Deleted Entry: \"{0}\" at \"{1}\"", entry.FullPath, original.FullPath);
				}
			}
			if (original == null)
			{
				// ensure hash has been calculated
				if (!entry.HasSignature)
				{
					if (file == null)
					{
						throw new ArgumentNullException("file", "FileInfo was missing for CatalogEntry without signature.");
					}

					Trace.TraceInformation("Compute Hash: \"{0}\"", entry.FullPath);
					entry.Signature = FileHash.ComputeHash(file);
				}

				// entry does not exist
				// if bits exist need to add or update entry (no transfer required)
				// else requires expensive bit transfer
				Trace.TraceInformation("Add Entry: \"{0}\"", entry.FullPath);
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

				Trace.TraceInformation("Compute Hash: \"{0}\"", entry.FullPath);
				entry.Signature = FileHash.ComputeHash(file);
			}

			// file exists at correct path but metadata is different
			this.Update(entry, original);
			return true;
		}

		#endregion Delta Methods

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
			VersionHistory version = VersionHistory.Create();

			Trace.TraceInformation("Add VersionHistory: \"{0}\"", version.Label);
			this.UnitOfWork.GetTable<VersionHistory>().Add(version);
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
