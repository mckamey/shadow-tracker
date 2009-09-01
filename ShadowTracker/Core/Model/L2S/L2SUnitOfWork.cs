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
				foreach (var insert in changes.Inserts)
				{
					hasChanges = true;

					CatalogEntry entry = insert as CatalogEntry;
					if (entry != null)
					{
						Console.WriteLine("ADD \"{0}\" at \"{1}\"", entry.Signature, entry.Path);
					}
					else
					{
						Console.WriteLine("ADD "+insert);
					}
				}
				foreach (var update in changes.Updates)
				{
					hasChanges = true;

					CatalogEntry entry = update as CatalogEntry;
					if (entry != null)
					{
						Console.WriteLine("UPDATE \"{0}\"", entry.Path);
					}
					else
					{
						Console.WriteLine("UPDATE "+update);
					}
				}
				foreach (var delete in changes.Deletes)
				{
					hasChanges = true;

					CatalogEntry entry = delete as CatalogEntry;
					if (entry != null)
					{
						Console.WriteLine("REMOVE \"{0}\"", entry.Path);
					}
					else
					{
						Console.WriteLine("REMOVE "+delete);
					}
				}
				if (!hasChanges)
				{
					//Console.WriteLine("NO CHANGES");
				}
			}

			this.DB.SubmitChanges(ConflictMode.ContinueOnConflict);
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
