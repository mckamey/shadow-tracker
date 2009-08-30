using System;
using System.Collections.Generic;
using System.IO;

namespace Shadow.Model
{
	public class MemoryUnitOfWork : IUnitOfWork
	{
		#region Fields

		private MemoryTable<CatalogEntry> Entries;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <remarks>Defaults to in-memory backing storage</remarks>
		public MemoryUnitOfWork()
		{
			this.Entries = new MemoryTable<CatalogEntry>(CatalogEntry.PathComparer);
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="entries">initial items</param>
		public MemoryUnitOfWork(IEnumerable<CatalogEntry> entries)
		{
			this.Entries = new MemoryTable<CatalogEntry>(entries, CatalogEntry.PathComparer); ;
		}

		#endregion Init

		#region IUnitOfWork Members

		public void SubmitChanges()
		{
			// NOOP
		}

		public void SetDiagnosticsLog(TextWriter writer)
		{
			// NOOP
		}

		public ITable<CatalogEntry> GetEntries()
		{
			return this.Entries;
		}

		#endregion IUnitOfWork Members
	}
}
