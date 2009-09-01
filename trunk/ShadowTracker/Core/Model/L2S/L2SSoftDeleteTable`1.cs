using System;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;

namespace Shadow.Model.L2S
{
	public interface IL2SSoftDeleteEntity
	{
		bool IsDeleted { get; set; }
	}

	/// <summary>
	/// A table which doesn't actually delete but instead flags as deleted
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class L2SSoftDeleteTable<T> :
		L2STable<T> where T : class, IL2SSoftDeleteEntity
	{
		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="db">DataContext</param>
		public L2SSoftDeleteTable(DataContext db) : base(db)
		{
		}

		#endregion Init

		#region L2STable<T> Methods

		protected override IQueryable<T> GetQueryable(Table<T> items)
		{
			// TODO: add where clause that filters soft-deleted here
			return base.GetQueryable(items);
		}

		public override void Add(T item)
		{
			// TODO: add ability to soft-undelete here

			// first look for closest deleted version of item
			// if found just undelete and update?

			base.Add(item);
		}

		public override void Remove(T item)
		{
			// TODO: update as soft-deleted

			base.Remove(item);
		}

		public override void RemoveWhere(Expression<Func<T, bool>> match)
		{
			// TODO: update as soft-deleted

			base.RemoveWhere(match);
		}

		#endregion L2STable<T> Methods
	}
}
