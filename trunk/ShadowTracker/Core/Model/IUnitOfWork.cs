using System;

namespace Shadow.Model
{
	public interface IUnitOfWork
	{
		ITable<CatalogEntry> Entries { get; }

		void Save();
	}
}
