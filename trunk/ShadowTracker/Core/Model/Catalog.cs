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

		private readonly ITable<CatalogEntry> entries;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="entries">the backing CatalogEntry storage</param>
		public Catalog(ITable<CatalogEntry> entries)
		{
			this.entries = entries;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets the sequence of data nodes
		/// </summary>
		public ITable<CatalogEntry> Entries
		{
			get { return this.entries; }
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

		public virtual void DeleteEntryByPath(string path)
		{
			this.Entries.RemoveWhere(n => n.Path == path);
		}

		public virtual void MoveEntryPath(string oldPath, string newPath)
		{
			CatalogEntry entry = this.GetEntryAtPath(oldPath);
			if (entry == null)
			{
				// TODO: log error
				throw new ArgumentException("Entry was not found: "+oldPath, oldPath);
			}
			entry.Path = newPath;
			this.Entries.Update(entry);
		}

		public DeltaAction CalcNodeDelta(CatalogEntry entry)
		{
			if (entry == null)
			{
				return DeltaAction.None;
			}

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

		public virtual void ApplyChanges(CatalogEntry entry)
		{
			this.ApplyChanges(entry, this.CalcNodeDelta(entry));
		}

		public virtual void ApplyChanges(CatalogEntry entry, DeltaAction action)
		{
			switch (action)
			{
				case DeltaAction.Add:
				{
					Console.WriteLine("ADD \"{0}\" at \"{1}\"", entry.Signature, entry.Path);
					this.Entries.Add(entry);
					break;
				}
				case DeltaAction.Clone:
				{
					Console.WriteLine("COPY: \"{0}\" to \"{1}\"", entry, entry.Path);
					this.Entries.Add(entry);
					break;
				}
				case DeltaAction.Delete:
				{
					Console.WriteLine("REMOVE: \"{0}\"", entry.Path);
					this.DeleteEntryByPath(entry.Path);
					break;
				}
				case DeltaAction.Meta:
				{
					Console.WriteLine("ATTRIB: \"{0}\"", entry.Path);
					this.Entries.Update(entry);
					break;
				}
				case DeltaAction.Update:
				{
					Console.WriteLine("REPLACE: \"{0}\" to \"{1}\"", entry.Signature, entry.Path);
					this.Entries.Update(entry);
					break;
				}
				default:
				case DeltaAction.None:
				{
					Console.WriteLine("No Action: "+entry);
					break;
				}
			}
		}

		#endregion ICatalogRepository Members
	}
}
