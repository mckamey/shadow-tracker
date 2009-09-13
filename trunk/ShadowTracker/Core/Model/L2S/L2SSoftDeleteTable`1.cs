using System;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;

namespace Shadow.Model.L2S
{
	public interface IL2SSoftDeleteEntity
	{
		DateTime? DeletedDate { get; set; }
		string Signature { get; }
		void CopyValuesFrom(IL2SSoftDeleteEntity item);
	}

	/// <summary>
	/// A table which doesn't actually delete but instead flags as deleted
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class L2SSoftDeleteTable<T> :
		L2STable<T> where T : class, IL2SSoftDeleteEntity
	{
		#region Fields

		private readonly bool OnlySoftDelete;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="db">DataContext</param>
		public L2SSoftDeleteTable(DataContext db)
			: this(db, true)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="db">DataContext</param>
		/// <param name="onlySoftDelete">determines if should soft-delete even entries without a signature</param>
		public L2SSoftDeleteTable(DataContext db, bool onlySoftDelete)
			: base(db)
		{
			this.OnlySoftDelete = onlySoftDelete;
		}

		#endregion Init

		#region L2STable<T> Methods

		protected override IQueryable<T> GetQueryable(Table<T> items)
		{
			// add a permanent where clause that filters soft-deleted here
			return base.GetQueryable(items).Where(n => !n.DeletedDate.HasValue);
		}

		/// <summary>
		/// Adds by first checking if a deleted version exists
		/// </summary>
		/// <param name="item"></param>
		public override void Add(T item)
		{
			T match;

			if (item.Signature != null)
			{
				// first look for most recent deleted version of item
				match =
				(from n in this.Items
				 where
					n.DeletedDate.HasValue &&
					n.Signature.ToLower() == item.Signature.ToLower()
				 orderby n.DeletedDate descending
				 select n).FirstOrDefault();
			}
			else
			{
				match = default(T);
			}

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
			if (!this.OnlySoftDelete && String.IsNullOrEmpty(item.Signature))
			{
				// cannot reliably undelete without a signature
				base.Remove(item);
				return;
			}

			// update as soft-deleted
			item.DeletedDate = DateTime.UtcNow;
		}

		public override void RemoveWhere(Expression<Func<T, bool>> match)
		{
			foreach (var item in this.Where(match))
			{
				if (!this.OnlySoftDelete && String.IsNullOrEmpty(item.Signature))
				{
					// cannot reliably undelete without a signature
					base.Remove(item);
					continue;
				}

				// update as soft-deleted
				item.DeletedDate = DateTime.UtcNow;
			}
		}

		#endregion L2STable<T> Methods
	}
}
