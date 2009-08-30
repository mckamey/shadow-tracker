using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.IO;

namespace Shadow.Model
{
	public class DBUnitOfWork : IUnitOfWork
	{
		#region Fields

		private static string ConnectionString = null;
		private static MappingSource Mappings;
		private readonly DataContext DB;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public DBUnitOfWork()
		{
			this.DB = new DataContext(DBUnitOfWork.ConnectionString, DBUnitOfWork.Mappings);
		}

		#endregion Init

		#region Settings Methods

		/// <summary>
		/// Initializes the global connection and mapping settings.
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="mappings"></param>
		public static void InitSettings(string connection, string mappings)
		{
			DBUnitOfWork.ConnectionString = connection;
			DBUnitOfWork.Mappings = XmlMappingSource.FromUrl(mappings);
		}

		#endregion Settings Methods

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

		void IUnitOfWork.SubmitChanges()
		{
			this.DB.SubmitChanges(ConflictMode.ContinueOnConflict);
		}

		public ITable<CatalogEntry> GetEntries()
		{
			return new TableAdapter<CatalogEntry>(this.DB);
		}

		public void SetDiagnosticsLog(TextWriter writer)
		{
			this.DB.Log = writer;
		}

		#endregion IUnitOfWork Members
	}
}
