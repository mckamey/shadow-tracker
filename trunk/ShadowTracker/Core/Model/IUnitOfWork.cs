using System;

namespace Shadow.Model
{
	public interface IUnitOfWork
	{
		ITable<Catalog> Catalogs { get; }

		ITable<CatalogEntry> Entries { get; }

		ITable<VersionHistory> Versions { get; }

		void Save();
	}
}
