using System;
using System.Collections.Generic;
using System.Linq;

namespace Shadow.Model
{
	/// <summary>
	/// An in-memory implementation of DataNode Catalog.
	/// </summary>
	public class Catalog : ICatalogRepository
	{
		#region Fields

		private readonly Dictionary<string, CatalogEntry> Paths = new Dictionary<string, CatalogEntry>(100, StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, CatalogEntry> Signatures = new Dictionary<string, CatalogEntry>(100, StringComparer.OrdinalIgnoreCase);

		#endregion Fields

		#region ICatalogRepository Members

		/// <summary>
		/// Gets and sets the sequence of data nodes
		/// </summary>
		public IQueryable<CatalogEntry> Entries
		{
			get { return this.Paths.Values.AsQueryable(); }
		}

		public CatalogEntry GetEntryAtPath(string path)
		{
			return this.Paths[path];
		}

		public bool ContainsPath(string path)
		{
			return this.Paths.ContainsKey(path);
		}

		public bool ContainsSignature(string signature)
		{
			return this.Signatures.ContainsKey(signature);
		}

		public string GetPathOfEntryBySignature(string signature)
		{
			CatalogEntry node = this.Signatures[signature];
			if (node == null)
			{
				return null;
			}

			return node.Path;
		}

		public void AddEntry(CatalogEntry entry)
		{
			if (String.IsNullOrEmpty(entry.Path))
			{
				throw new ArgumentOutOfRangeException("entry", entry, "CatalogEntry does not specify path.");
			}

			this.Paths[entry.Path] = entry;

			if (entry.HasSignature)
			{
				this.Signatures[entry.Signature] = entry;
			}
		}

		public void UpdateEntry(CatalogEntry entry)
		{
			throw new NotImplementedException();
		}

		public void RemoveEntry(CatalogEntry entry)
		{
			throw new NotImplementedException();
		}

		#endregion ICatalogRepository Members
	}
}
