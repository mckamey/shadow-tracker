using System;
using System.Data.Linq;

namespace Shadow.Model.L2S
{
	/// <summary>
	/// A unit-of-work adapter for LINQ-to-SQL DataContexts.
	/// </summary>
	public class L2SUnitOfWork : IUnitOfWork
	{
		#region Fields

		private readonly DataContext DB;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="db"></param>
		public L2SUnitOfWork(DataContext db)
		{
			this.DB = db;
		}

		#endregion Init

		#region Methods

		public bool CanConnect()
		{
			return this.DB.DatabaseExists();
		}

		public void InitializeDatabase()
		{
			this.DB.CreateDatabase();
		}

		#endregion Methods

		#region Events

		public event Action<L2SUnitOfWork, ChangeSet> OnCommit;

		#endregion Events

		#region IUnitOfWork Members

		public void Save()
		{
			if (this.OnCommit != null)
			{
				ChangeSet changes = this.DB.GetChangeSet();

				if ((changes.Inserts.Count > 0) ||
					(changes.Updates.Count > 0) ||
					(changes.Deletes.Count > 0))
				{
					this.OnCommit(this, changes);
				}
			}

			this.DB.SubmitChanges(ConflictMode.ContinueOnConflict);
		}

		/// <summary>
		/// Returns a collection of objects of a particular type,
		/// where the type is defined by the TEntity parameter.
		/// </summary>
		/// <typeparam name="T">The type of the objects to be returned.</typeparam>
		/// <returns>A collection of objects.</returns>
		public ITable<TEntity> GetTable<TEntity>() where TEntity : class
		{
			if (typeof(ISoftDeleteEntity).IsAssignableFrom(typeof(TEntity)))
			{
				return new L2SSoftDeleteTable<TEntity>(this.DB);
			}

			return new L2STable<TEntity>(this.DB);
		}

		#endregion IUnitOfWork Members
	}
}
