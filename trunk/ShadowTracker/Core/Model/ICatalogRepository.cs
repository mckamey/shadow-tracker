using System;
using System.Linq;

namespace Shadow.Model
{
	/// <summary>
	/// Repository of DataNodes with ability to lookup by path or signature.
	/// </summary>
	public interface ICatalogRepository
	{
		ITable<CatalogEntry> Entries { get; }

		/// <summary>
		/// Check if an entry exists for the given path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		bool ContainsPath(string path);

		/// <summary>
		/// Gets the entry for a given path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		CatalogEntry GetEntryAtPath(string path);

		/// <summary>
		/// Check if bits exist at any location.
		/// </summary>
		/// <param name="signature"></param>
		/// <returns></returns>
		bool ContainsSignature(string hash);

		/// <summary>
		/// Gets the entry for a given signature.
		/// </summary>
		/// <param name="signature"></param>
		/// <returns></returns>
		string GetPathOfEntryBySignature(string hash);

		/// <summary>
		/// Create a new entry for a path/bits combination.
		/// </summary>
		/// <param name="entry"></param>
		void AddEntry(CatalogEntry entry);

		/// <summary>
		/// Update the bits for a particular entry.
		/// </summary>
		/// <param name="entry"></param>
		void UpdateEntry(CatalogEntry entry);

		/// <summary>
		/// Removes the specified entry for the given path.
		/// </summary>
		/// <param name="path"></param>
		void DeleteEntry(string path);
	}
}
