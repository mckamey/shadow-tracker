using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace Shadow.Model
{
	/// <summary>
	/// A unit-of-work adapter for LINQ-to-SQL DataContexts.
	/// </summary>
	public class L2SUnitOfWork : IUnitOfWork
	{
		#region Fields

		private readonly DataContext DB;

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
			this.DB.SubmitChanges(ConflictMode.ContinueOnConflict);
		}

		public ITable<CatalogEntry> Entries
		{
			get
			{
				return new L2STable<CatalogEntry>(this.DB);
			}
		}

		#endregion IUnitOfWork Members
	}
}
