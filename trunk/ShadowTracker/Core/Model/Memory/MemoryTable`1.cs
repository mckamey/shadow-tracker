using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Shadow.Model.Memory
{
	/// <summary>
	/// An in-memory representation of a queryable table.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <remarks>
	/// Note this contains object references so if the object is retrieved and updated,
	/// the changes will take effect immediately within the Table. This is different
	/// from remote persistent storage system such as a RDBMS.
	/// </remarks>
	public class MemoryTable<T> :
		ITable<T>
		where T : class
	{
		#region Fields

		private readonly HashSet<T> Items;
		private readonly IQueryable<T> Queryable;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="comparer"></param>
		public MemoryTable(IEqualityComparer<T> comparer)
			: this(comparer, null)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="source">initial items</param>
		/// <param name="comparer">determines if two objects represent the same item</param>
		public MemoryTable(IEqualityComparer<T> comparer, IEnumerable<T> items)
		{
			if (items == null)
			{
				if (comparer == null)
				{
					this.Items = new HashSet<T>();
				}
				else
				{
					this.Items = new HashSet<T>(comparer);
				}
			}
			else
			{
				if (comparer == null)
				{
					this.Items = new HashSet<T>(items);
				}
				else
				{
					this.Items = new HashSet<T>(items, comparer);
				}
			}

			this.Queryable = this.Items.AsQueryable();
		}

		#endregion Init

		#region ITable<TItem> Members

		public void Add(T item)
		{
			this.Items.Add(item);
		}

		public void Update(T item)
		{
			this.RemoveWhere(n => this.Items.Comparer.Equals(n, item));
			this.Items.Add(item);
		}

		public void Remove(T item)
		{
			this.Items.Remove(item);
		}

		public void RemoveWhere(Expression<Func<T, bool>> match)
		{
			Func<T,bool> predicate = match.Compile();
			this.Items.RemoveWhere(delegate(T item) { return predicate(item); });
		}

		#endregion ITable<TItem> Members

		#region IEnumerable<TItem> Members

		public IEnumerator<T> GetEnumerator()
		{
			return this.Items.GetEnumerator();
		}

		#endregion IEnumerable<TItem> Members

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.Items.GetEnumerator();
		}

		#endregion IEnumerable Members

		#region IQueryable Members

		public Type ElementType
		{
			get { return this.Queryable.ElementType; }
		}

		public Expression Expression
		{
			get { return this.Queryable.Expression; }
		}

		public IQueryProvider Provider
		{
			get { return this.Queryable.Provider; }
		}

		#endregion IQueryable Members
	}
}
