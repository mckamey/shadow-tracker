using System;
using System.Linq;

namespace Shadow.Model
{
	/// <summary>
	/// Implements a repository pattern for Catalog Entries which
	/// can be backed by a number of different storage mechanisms.
	/// </summary>
	public class Catalog
	{
		#region Fields

		private ITable<CatalogEntry> entries;

		#endregion Fields

		#region Properties

		/// <summary>
		/// Gets and sets the sequence of data nodes
		/// </summary>
		public ITable<CatalogEntry> Entries
		{
			get { return this.entries; }
			set { this.entries = value; }
		}

		#endregion Properties

		#region Methods

		private CatalogEntry GetEntryAtPath(string path)
		{
			return this.Entries.FirstOrDefault(n => n.Path == path);
		}

		private bool ContainsSignature(string hash)
		{
			return this.Entries.Any(n => n.Signature == hash);
		}

		private void DeleteEntryByPath(string path)
		{
			this.Entries.RemoveWhere(n => n.Path == path);
		}

		public DeltaAction CalcNodeDelta(CatalogEntry entry)
		{
			// look for existing node
			CatalogEntry local = this.GetEntryAtPath(entry.Path);

			if (local == null)
			{
				// file is missing, see if have a copy elsewhere (e.g. moved/renamed/copied)
				if (!entry.IsDirectory && this.ContainsSignature(entry.Signature))
				{
					// equivalent file found
					return DeltaAction.Clone;
				}

				// completely missing file, need to add
				return DeltaAction.Add;
			}

			if (entry.Equals(local))
			{
				// no changes, identical
				return DeltaAction.None;
			}

			if (StringComparer.OrdinalIgnoreCase.Equals(local.Signature, entry.Signature))
			{
				// correct bits exist at correct path but metadata is different
				return DeltaAction.Meta;
			}

			// bits are different, see if have a equivalent copy elsewhere
			if (!entry.IsDirectory && this.ContainsSignature(entry.Signature))
			{
				// equivalent file found
				return DeltaAction.Clone;
			}

			// file exists but bits are different
			return DeltaAction.Update;
		}

		#endregion ICatalogRepository Members
	}
}
