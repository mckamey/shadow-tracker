using System;

namespace Shadow.Model
{
	public interface IUnitOfWork
	{
		ITable<Catalog> Catalogs { get; }

		ITable<CatalogEntry> Entries { get; }

		ITable<VersionHistory> Versions { get; }

		ITable<TEntity> GetTable<TEntity>()
			where TEntity : class;

		void Save();
	}
}
