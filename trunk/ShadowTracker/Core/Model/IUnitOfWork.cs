using System;

namespace Shadow.Model
{
	public interface IUnitOfWork
	{
		ITable<TEntity> GetTable<TEntity>()
			where TEntity : class;

		void Save();
	}
}
