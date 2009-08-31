using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;

namespace Shadow.Model.L2S
{
	/// <summary>
	/// A queryable table adapter for LINQ-to-SQL tables.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class L2STable<T> :
		ITable<T> where T:class
	{
		#region Fields

		private readonly Table<T> Items;
		private readonly IQueryable<T> Queryable;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="source">initial items</param>
		public L2STable(DataContext db)
		{
			this.Items = db.GetTable<T>();
			this.Queryable = this.Items;
		}

		#endregion Init

		#region ITable<TItem> Members

		public void Add(T item)
		{
			this.Items.InsertOnSubmit(item);
		}

		public void Update(T item)
		{
			this.Items.Attach(item, true);
		}

		public void Remove(T item)
		{
			this.Items.DeleteOnSubmit(item);
		}

		public void RemoveWhere(Expression<Func<T,bool>> match)
		{
			this.Items.DeleteAllOnSubmit(this.Items.Where(match));
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
