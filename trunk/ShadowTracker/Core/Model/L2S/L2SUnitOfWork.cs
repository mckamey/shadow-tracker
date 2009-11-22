using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.IO;

namespace Shadow.Model.L2S
{
	public delegate void CommitCallback(L2SUnitOfWork unitOfWork, ChangeSet changes);

	/// <summary>
	/// A unit-of-work adapter for LINQ-to-SQL DataContexts.
	/// </summary>
	public class L2SUnitOfWork : IUnitOfWork
	{
		#region Fields

		private readonly DataContext DB;
		private ITable<Catalog> catalogs;
		private ITable<CatalogEntry> entries;
		private ITable<VersionHistory> versions;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="db"></param>
		public L2SUnitOfWork(DataContext db)
		{
			this.DB = db;
		}

		#endregion Init

		#region Methods

		public bool CanConnect()
		{
			return this.DB.DatabaseExists();
		}

		public void InitializeDatabase()
		{
			this.DB.CreateDatabase();
		}

		#endregion Methods

		#region Events

		public event CommitCallback OnCommit;

		#endregion Events

		#region IUnitOfWork Members

		public void Save()
		{
			if (this.OnCommit != null)
			{
				ChangeSet changes = this.DB.GetChangeSet();

				if ((changes.Inserts.Count > 0) ||
					(changes.Updates.Count > 0) ||
					(changes.Deletes.Count > 0))
				{
					this.OnCommit(this, changes);
				}
			}

			this.DB.SubmitChanges(ConflictMode.ContinueOnConflict);
		}

		public ITable<Catalog> Catalogs
		{
			get
			{
				if (this.catalogs == null)
				{
					this.catalogs = new L2STable<Catalog>(this.DB);
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
					this.entries = new L2SSoftDeleteTable<CatalogEntry>(this.DB);
				}
				return this.entries;
			}
		}

		public ITable<VersionHistory> Versions
		{
			get
			{
				if (this.versions == null)
				{
					this.versions = new L2STable<VersionHistory>(this.DB);
				}
				return this.versions;
			}
		}

		#endregion IUnitOfWork Members
	}
}
