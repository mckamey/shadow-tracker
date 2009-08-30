using System;
using System.Collections.Generic;
using System.Linq;

namespace Shadow.Model
{
	public class MemoryUnitOfWork : IUnitOfWork
	{
		#region Fields

		private IEnumerable<CatalogEntry> Storage;
		private MemoryTable<CatalogEntry> IdentityMap;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="comparer"></param>
		public MemoryUnitOfWork()
			: this(null)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="comparer"></param>
		/// <param name="entities">initial items</param>
		public MemoryUnitOfWork(IEnumerable<CatalogEntry> entities)
		{
			this.Storage = entities != null ? entities : Enumerable.Empty<CatalogEntry>();
		}

		#endregion Init

		#region IUnitOfWork Members

		public void Save()
		{
			if (this.IdentityMap != null)
			{
				// save contents to "storage"
				this.Storage = this.IdentityMap.AsEnumerable();
			}

			// reset change tracking
			this.IdentityMap = null;
		}

		public ITable<CatalogEntry> Entries
		{
			get
			{
				if (this.IdentityMap == null)
				{
					this.IdentityMap = new MemoryTable<CatalogEntry>(CatalogEntry.PathComparer, this.Storage);
				}
				return this.IdentityMap;
			}
		}

		#endregion IUnitOfWork Members
	}
}
