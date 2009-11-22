using System;
using System.Collections.Generic;
using System.Linq;

namespace Shadow.Model.Memory
{
	public class MemoryUnitOfWork : IUnitOfWork
	{
		#region Fields

		private readonly IDictionary<Type, object> Storage = new Dictionary<Type, object>(3);
		private readonly IDictionary<Type, object> Maps = new Dictionary<Type, object>(3);

		#endregion Fields

		#region IUnitOfWork Members

		public void Save()
		{
			foreach (Type type in this.Maps.Keys)
			{
				// "save" catalogs
				if (this.Maps.ContainsKey(type))
				{
					// save contents to "storage"
					this.Storage[type] = this.Maps[type];
				}

				// reset change tracking
				this.Maps[type] = null;
			}
		}

		public ITable<TEntity> GetTable<TEntity>() where TEntity : class
		{
			ITable<TEntity> map =
				this.Maps.ContainsKey(typeof(TEntity)) ?
				this.Maps[typeof(TEntity)] as ITable<TEntity> :
				null;

			if (map == null)
			{
				IEnumerable<TEntity> storage =
					this.Storage.ContainsKey(typeof(TEntity)) ?
					this.Storage[typeof(TEntity)] as IEnumerable<TEntity> :
					null;

				if (storage == null)
				{
					this.Storage[typeof(TEntity)] = storage = Enumerable.Empty<TEntity>();
				}

				// TODO: figure out how to switch comparers?
				// Catalog.PathComparer
				// CatalogEntry.PathComparer

				this.Maps[typeof(TEntity)] = map = new MemoryTable<TEntity>(EqualityComparer<TEntity>.Default, storage);
			}
			return map;
		}

		#endregion IUnitOfWork Members

		#region Test Methods

		public void PopulateTable<T>(IEnumerable<T> items)
		{
			this.Storage[typeof(T)] = items;
		}

		#endregion Test Methods
	}
}
