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
	internal class L2STable<T> :
		ITable<T> where T:class
	{
		#region Fields

		protected readonly Table<T> Items;
		private readonly IQueryable<T> Queryable;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="db">DataContext</param>
		public L2STable(DataContext db)
		{
			this.Items = db.GetTable<T>();

			this.Queryable = this.GetQueryable(this.Items);
		}

		#endregion Init

		#region Methods

		protected virtual IQueryable<T> GetQueryable(Table<T> items)
		{
			return items;
		}

		#endregion Methods

		#region ITable<TItem> Members

		public virtual void Add(T item)
		{
			this.Items.InsertOnSubmit(item);
		}

		public virtual void Update(T item)
		{
			this.Items.Attach(item, true);
		}

		public virtual void Remove(T item)
		{
			this.Items.DeleteOnSubmit(item);
		}

		public virtual void RemoveWhere(Expression<Func<T,bool>> match)
		{
			this.Items.DeleteAllOnSubmit(this.Items.Where(match));
		}

		#endregion ITable<TItem> Members

		#region IEnumerable<TItem> Members

		public IEnumerator<T> GetEnumerator()
		{
			return this.Queryable.GetEnumerator();
		}

		#endregion IEnumerable<TItem> Members

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.Queryable.GetEnumerator();
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
