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

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public L2SUnitOfWork(string connection, MappingSource mappings)
		{
			this.DB = new DataContext(connection, mappings);
		}

		#endregion Init

		#region Properties

		public TextWriter Log
		{
			get;
			set;
		}

		#endregion Properties

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

		#region IUnitOfWork Members

		public void Save()
		{
			if (this.Log != null)
			{
				bool hasChanges = false;
				ChangeSet changes = this.DB.GetChangeSet();
				foreach (var inserted in changes.Inserts)
				{
					hasChanges = true;

					CatalogEntry entry = inserted as CatalogEntry;
					if (entry != null)
					{
						Console.WriteLine("ADD \"{0}\" at \"{1}\"", entry.Signature, entry.FullPath);
					}
					else if (inserted is Catalog)
					{
						Console.WriteLine("ADD Catalog at \"{0}\"", ((Catalog)inserted).Path);
					}
					else
					{
						Console.WriteLine("ADD "+inserted);
					}
				}
				foreach (var updated in changes.Updates)
				{
					hasChanges = true;

					CatalogEntry entry = updated as CatalogEntry;
					if (entry != null)
					{
						Console.WriteLine("UPDATE \"{0}\"", entry.FullPath);
					}
					else if (updated is Catalog)
					{
						Console.WriteLine("UPDATE Catalog \"{0}\"", ((Catalog)updated).Path);
					}
					else
					{
						Console.WriteLine("UPDATE "+updated);
					}
				}
				foreach (var deleted in changes.Deletes)
				{
					hasChanges = true;

					CatalogEntry entry = deleted as CatalogEntry;
					if (entry != null)
					{
						Console.WriteLine("REMOVE \"{0}\"", entry.FullPath);
					}
					else if (deleted is Catalog)
					{
						Console.WriteLine("REMOVE Catalog \"{0}\"", ((Catalog)deleted).Path);
					}
					else
					{
						Console.WriteLine("REMOVE "+deleted);
					}
				}
				if (!hasChanges)
				{
					//Console.WriteLine("NO CHANGES");
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

		#endregion IUnitOfWork Members
	}
}
