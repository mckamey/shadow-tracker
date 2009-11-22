using System;

namespace Shadow.Model
{
	public interface IUnitOfWork
	{
		ITable<T> GetTable<T>()
			where T : class;

		void Save();
	}
}
