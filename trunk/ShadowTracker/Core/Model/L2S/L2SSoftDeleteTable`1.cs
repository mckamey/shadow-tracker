using System;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;

namespace Shadow.Model.L2S
{
	public interface ISoftDeleteEntity
	{
		DateTime? DeletedDate { get; set; }

		string Signature { get; }

		void CopyValuesFrom(object item);
	}

	/// <summary>
	/// A table which doesn't actually delete but instead flags as deleted
	/// </summary>
	/// <typeparam name="T">must implement ISoftDeleteEntity</typeparam>
	internal class L2SSoftDeleteTable<T> :
		L2STable<T>
		where T : class//, ISoftDeleteEntity
	{
		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="db">DataContext</param>
		public L2SSoftDeleteTable(DataContext db)
			: base(db)
		{
			if (!typeof(ISoftDeleteEntity).IsAssignableFrom(typeof(T)))
			{
				throw new InvalidOperationException("L2SSoftDeleteTable only supports ISoftDeleteEntity");
			}
		}

		#endregion Init

		#region L2STable<T> Methods

		protected override IQueryable<T> GetQueryable(Table<T> items)
		{
			// add a permanent where clause that filters soft-deleted items
			return
				from ISoftDeleteEntity n in base.GetQueryable(items)
				where !n.DeletedDate.HasValue
				select (T)n;
		}

		/// <summary>
		/// Adds by first checking if a deleted version exists
		/// </summary>
		/// <param name="item"></param>
		public override void Add(T item)
		{
			string signature = ((ISoftDeleteEntity)item).Signature;

			// first look for most recent deleted item with signature
			ISoftDeleteEntity match =
				(from ISoftDeleteEntity n in this
				 where
					 n.DeletedDate.HasValue &&
					 n.Signature == signature

				 orderby n.DeletedDate descending
				 select n).FirstOrDefault();

			if (match != default(T))
			{
				// if found just undelete and update
				match.CopyValuesFrom(item);
			}
			else
			{
				// otherwise add the new item
				base.Add(item);
			}
		}

		public override void Remove(T item)
		{
			// update as soft-deleted
			((ISoftDeleteEntity)item).DeletedDate = DateTime.UtcNow;
		}

		public override void RemoveWhere(Expression<Func<T, bool>> match)
		{
			DateTime now = DateTime.UtcNow;

			foreach (ISoftDeleteEntity item in this.Where(match))
			{
				item.DeletedDate = now;
			}
		}

		#endregion L2STable<TEntity, TKey> Methods
	}
}
