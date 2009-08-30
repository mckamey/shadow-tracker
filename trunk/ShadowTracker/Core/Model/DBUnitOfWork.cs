using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.IO;

namespace Shadow.Model
{
	public class DBUnitOfWork : DataContext, IUnitOfWork
	{
		#region Fields

		private static string ConnectionString = null;

		private static MappingSource Mappings;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public DBUnitOfWork()
			: base(DBUnitOfWork.ConnectionString, DBUnitOfWork.Mappings)
		{
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

		#region IUnitOfWork Members

		void IUnitOfWork.SubmitChanges()
		{
			base.SubmitChanges(ConflictMode.ContinueOnConflict);
		}

		public ITable<CatalogEntry> GetEntries()
		{
			return new TableAdapter<CatalogEntry>(this);
		}

		public void SetDiagnosticsLog(TextWriter writer)
		{
			this.Log = writer;
		}

		#endregion IUnitOfWork Members
	}
}
