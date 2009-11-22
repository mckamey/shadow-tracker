using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.IO;

namespace Shadow.Model.L2S
{
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

		public event Action<L2SUnitOfWork, ChangeSet> OnCommit;

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

		/// <summary>
		/// Returns a collection of objects of a particular type,
		/// where the type is defined by the TEntity parameter.
		/// </summary>
		/// <typeparam name="T">The type of the objects to be returned.</typeparam>
		/// <returns>A collection of objects.</returns>
		public ITable<TEntity> GetTable<TEntity>() where TEntity : class
		{
			if (typeof(ISoftDeleteEntity).IsAssignableFrom(typeof(TEntity)))
			{
				return new L2SSoftDeleteTable<TEntity>(this.DB);
			}

			return new L2STable<TEntity>(this.DB);
		}

		public ITable<Catalog> Catalogs
		{
			get
			{
				if (this.catalogs == null)
				{
					this.catalogs = this.GetTable<Catalog>();
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
					this.entries = this.GetTable<CatalogEntry>();
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
					this.versions = this.GetTable<VersionHistory>();
				}
				return this.versions;
			}
		}

		#endregion IUnitOfWork Members
	}
}
