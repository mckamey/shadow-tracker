using System;

namespace Shadow.Model
{
	public interface IUnitOfWork
	{
		ITable<Catalog> Catalogs { get; }

		ITable<CatalogEntry> Entries { get; }

		void Save();
	}
}
