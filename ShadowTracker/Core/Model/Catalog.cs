using System;
using System.Linq;

namespace Shadow.Model
{
	/// <summary>
	/// An in-memory implementation of DataNode Catalog.
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
			return
				(from entry in this.Entries
				 where entry.Path == path
				 select entry).SingleOrDefault();
		}

		private bool ContainsSignature(string hash)
		{
			return
				(from entry in this.Entries
				 where entry.Signature == hash
				 select entry.Path).Count() > 0;
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
