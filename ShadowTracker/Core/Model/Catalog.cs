using System;
using System.Collections.Generic;
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

		[Flags]
		private enum MatchRank
		{
			None = 0x00,
			Path = 0x01,
			Hash = 0x02,
			Both = Path|Hash
		}

		/// <summary>
		/// Finds the nearest matching existing CatalogEntry.
		/// Exact match (path & hash) is best.
		/// Same file signature is next (can clone).
		/// Finally a matching path (requires expensive copying of bits).
		/// </summary>
		/// <param name="path"></param>
		/// <param name="hash"></param>
		/// <param name="match"></param>
		/// <returns></returns>
		private MatchRank FindMatch(string path, string hash, out CatalogEntry match)
		{
			var query =
				from entry in this.Entries
				let rank =
					(entry.Path == path ? MatchRank.Path : MatchRank.None) |
					(entry.Signature == hash ? MatchRank.Hash : MatchRank.Path)
				where rank > 0
				orderby rank descending
				select new
				{
					Rank = rank,
					Entry = entry
				};

			var result = query.FirstOrDefault();
			if (result == null || result.Entry == null)
			{
				match = null;
				return MatchRank.None;
			}

			match = result.Entry;
			return result.Rank;
		}

		private CatalogEntry GetEntryAtPath(string path)
		{
			return this.Entries.FirstOrDefault(n => n.Path == path);
		}

		public virtual void DeleteEntryByPath(string path)
		{
			this.Entries.RemoveWhere(n => n.Path == path);
		}

		public virtual void FastMoveByPath(string oldPath, string newPath)
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

		protected DeltaAction CalcEntryDelta(CatalogEntry entry, out CatalogEntry match)
		{
			if (entry == null)
			{
				match = null;
				return DeltaAction.None;
			}

			// look for closest matching node
			switch(this.FindMatch(entry.Path, entry.Signature, out match))
			{
				case MatchRank.Hash:
				{
					if (entry.IsDirectory)
					{
						// need to add empty directory
						return DeltaAction.Add;
					}

					// equivalent file found
					return DeltaAction.Clone;
				}
				case MatchRank.Path:
				{
					// file exists but bits are different
					return DeltaAction.Update;
				}
				case MatchRank.Both:
				{
					if (entry.Equals(match))
					{
						// no changes, identical
						return DeltaAction.None;
					}

					// correct bits exist at correct path but metadata is different
					return DeltaAction.Meta;
				}
				default:
				case MatchRank.None:
				{
					// completely missing file, need to add
					return DeltaAction.Add;
				}
			}
		}

		public virtual void ApplyChanges(CatalogEntry entry)
		{
			CatalogEntry match;
			switch (this.CalcEntryDelta(entry, out match))
			{
				case DeltaAction.Add:
				{
					// does not exist (requires expensive bit transfer)
					Console.WriteLine("ADD \"{0}\" at \"{1}\"", entry.Signature, entry.Path);
					this.Entries.Add(entry);
					break;
				}
				case DeltaAction.Clone:
				{
					// bits exist need to add or update entry (no transfer required)
					Console.WriteLine("CLONE: \"{0}\" to \"{1}\"", entry.Signature, entry.Path);
					this.Entries.Add(entry);
					break;
				}
				case DeltaAction.Delete:
				{
					// this actually wouldn't be detected here
					Console.WriteLine("REMOVE: \"{0}\"", entry.Path);
					this.DeleteEntryByPath(entry.Path);
					break;
				}
				case DeltaAction.Meta:
				{
					// bits are same but metadata different
					Console.WriteLine("ATTRIB: \"{0}\"", entry.Path);
					this.Entries.Update(entry);
					break;
				}
				case DeltaAction.Update:
				{
					// bits are different but exists (requires expensive bit transfer)
					Console.WriteLine("REPLACE: \"{0}\" to \"{1}\"", entry.Signature, entry.Path);
					this.Entries.Update(entry);
					break;
				}
				default:
				case DeltaAction.None:
				{
					// no change required
					// Console.WriteLine("No Action: "+entry);
					break;
				}
			}
		}

		#endregion Methods

		#region Full Delta Methods

		public void SyncCatalog(Catalog that)
		{
			// apply any deltas since last sync
			foreach (CatalogEntry entry in that.Entries)
			{
				this.ApplyChanges(entry);
			}

			// NOTE: always perform deletes last, so that
			// moves or renames can be expressed as a clone/delete

			foreach (CatalogEntry entry in this.Entries)
			{
				// extras are any local entries not contained in other
				if (that.Entries.Where(n => n.Path == entry.Path).Any())
				{
					continue;
				}

				this.DeleteEntryByPath(entry.Path);
			}
		}

		#endregion Full Delta Methods
	}
}
