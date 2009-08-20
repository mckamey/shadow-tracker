using System;
using System.Linq;

namespace Shadow.Model
{
	/// <summary>
	/// Repository of DataNodes with ability to lookup by path or signature.
	/// </summary>
	public interface ICatalogRepository
	{
		IQueryable<CatalogEntry> Entries { get; }

		CatalogEntry GetEntryAtPath(string path);

		string GetPathOfEntryBySignature(string signature);

		bool ContainsPath(string path);

		bool ContainsSignature(string signature);

		void AddEntry(CatalogEntry entry);

		void UpdateEntry(CatalogEntry entry);

		void RemoveEntry(CatalogEntry entry);
	}
}
