using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Shadow.Model
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
	public class MemoryTable<T> : ITable<T>
	{
		#region Fields

		private readonly HashSet<T> Items;
		private readonly IQueryable<T> Queryable;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public MemoryTable()
		{
			this.Items = new HashSet<T>();
			this.Queryable = this.Items.AsQueryable();
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="source">initial items</param>
		public MemoryTable(IEnumerable<T> items)
		{
			this.Items = new HashSet<T>(items);
			this.Queryable = this.Items.AsQueryable();
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="comparer">determines if two objects represent the same item</param>
		public MemoryTable(IEqualityComparer<T> comparer)
		{
			this.Items = new HashSet<T>(comparer);
			this.Queryable = this.Items.AsQueryable();
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="source">initial items</param>
		/// <param name="comparer">determines if two objects represent the same item</param>
		public MemoryTable(IEnumerable<T> items, IEqualityComparer<T> comparer)
		{
			this.Items = new HashSet<T>(items, comparer);
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
			this.Items.Add(item);
		}

		public void Remove(T item)
		{
			this.Items.Remove(item);
		}

		public void RemoveWhere(Predicate<T> match)
		{
			this.Items.RemoveWhere(match);
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
