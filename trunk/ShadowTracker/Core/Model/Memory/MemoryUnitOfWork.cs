using System;
using System.Collections.Generic;
using System.Linq;

namespace Shadow.Model.Memory
{
	public class MemoryUnitOfWork : IUnitOfWork
	{
		#region Fields

		private readonly IDictionary<Type, object> Storage = new Dictionary<Type, object>(3);
		private readonly IDictionary<Type, object> Tables = new Dictionary<Type, object>(3);
		private readonly IDictionary<Type, object> Comparers = new Dictionary<Type, object>(3);

		#endregion Fields

		#region IUnitOfWork Members

		public void Save()
		{
			// "save" each table
			foreach (Type type in this.Tables.Keys)
			{
				if (this.Tables.ContainsKey(type))
				{
					// save contents to "storage"
					this.Storage[type] = this.Tables[type];
				}

				// reset change tracking
				this.Tables[type] = null;
			}
		}

		public ITable<T> GetTable<T>() where T : class
		{
			ITable<T> table =
				this.Tables.ContainsKey(typeof(T)) ?
				this.Tables[typeof(T)] as ITable<T> :
				null;

			if (table == null)
			{
				IEnumerable<T> storage =
					this.Storage.ContainsKey(typeof(T)) ?
					this.Storage[typeof(T)] as IEnumerable<T> :
					null;

				if (storage == null)
				{
					this.Storage[typeof(T)] = storage = Enumerable.Empty<T>();
				}

				IEqualityComparer<T> comparer =
					this.Comparers.ContainsKey(typeof(T)) ?
					this.Comparers[typeof(T)] as IEqualityComparer<T> :
					null;

				if (comparer == null)
				{
					this.Comparers[typeof(T)] = comparer = EqualityComparer<T>.Default;
				}

				this.Tables[typeof(T)] = table = new MemoryTable<T>(comparer, storage);
			}

			return table;
		}

		#endregion IUnitOfWork Members

		#region Test Methods

		public void PopulateTable<T>(IEnumerable<T> items)
		{
			this.PopulateTable(EqualityComparer<T>.Default, items);
		}

		public void PopulateTable<T>(IEqualityComparer<T> comparer, IEnumerable<T> items)
		{
			this.Comparers[typeof(T)] = comparer;
			this.Storage[typeof(T)] = items;
		}

		#endregion Test Methods
	}
}
