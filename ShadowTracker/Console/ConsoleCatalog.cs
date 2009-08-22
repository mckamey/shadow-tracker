using System;

using Shadow.Model;

namespace Shadow.ConsoleTest
{
	public class ConsoleCatalog : Catalog
	{
		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="entries"></param>
		public ConsoleCatalog(ITable<CatalogEntry> entries) : base(entries)
		{
		}

		#endregion Init

		#region Action Methods

		public override void AddEntry(CatalogEntry entry)
		{
			Console.WriteLine("ADD \"{0}\" at \"{1}\"", entry.Signature, entry.Path);

			base.AddEntry(entry);
		}

		public override void CloneEntry(CatalogEntry entry, CatalogEntry match)
		{
			Console.WriteLine("CLONE: \"{0}\" to \"{1}\"", match.Signature, entry.Path);

			base.CloneEntry(entry, match);
		}

		public override void DeleteEntryByPath(string path)
		{
			Console.WriteLine("REMOVE: \"{0}\"", path);

			base.DeleteEntryByPath(path);
		}

		public override void FastMoveByPath(string oldPath, string newPath)
		{
			Console.WriteLine("FAST MOVE: \"{0}\" to \"{1}\"", oldPath, newPath);

			base.FastMoveByPath(oldPath, newPath);
		}

		public override void UpdateData(CatalogEntry entry)
		{
			Console.WriteLine("DATA: \"{0}\" to \"{1}\"", entry.Signature, entry.Path);

			base.UpdateData(entry);
		}

		public override void UpdateMetaData(CatalogEntry entry)
		{
			Console.WriteLine("META: \"{0}\"", entry.Path);

			base.UpdateMetaData(entry);
		}

		#endregion Action Methods
	}
}
