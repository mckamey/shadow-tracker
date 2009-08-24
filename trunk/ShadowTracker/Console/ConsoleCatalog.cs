using System;
using System.Collections.Generic;
using System.Data.Linq;

using Shadow.Model;

namespace Shadow.ConsoleTest
{
	public class ConsoleCatalog : CatalogRepository
	{
		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <remarks>Defaults to in-memory backing storage</remarks>
		public ConsoleCatalog()
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="entries">initial items</param>
		public ConsoleCatalog(IEnumerable<CatalogEntry> entries)
			: base(entries)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="entries"></param>
		public ConsoleCatalog(ITable<CatalogEntry> entries)
			: base(entries)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="db">LINQ-to-SQL DataContext</param>
		public ConsoleCatalog(DataContext db)
			: base(db)
		{
#if DEBUG
			this.SetUnitOfWorkLog(Console.Error);
#endif

		}

		#endregion Init

		#region Action Methods

		public override void AddEntry(CatalogEntry entry)
		{
			Console.WriteLine("ADD \"{0}\" at \"{1}\"", entry.Signature, entry.Path);

			base.AddEntry(entry);
		}

		public override void CloneEntry(CatalogEntry entry, CatalogEntry data)
		{
			Console.WriteLine("CLONE: \"{0}\" to \"{1}\"", data.Signature, entry.Path);

			base.CloneEntry(entry, data);
		}

		public override void DeleteEntryByPath(string path)
		{
			Console.WriteLine("REMOVE: \"{0}\"", path);

			base.DeleteEntryByPath(path);
		}

		public override void RenameEntry(string oldPath, string newPath)
		{
			Console.WriteLine("RENAME: \"{0}\" to \"{1}\"", oldPath, newPath);

			base.RenameEntry(oldPath, newPath);
		}

		public override void UpdateData(CatalogEntry entry, CatalogEntry original, CatalogEntry data)
		{
			Console.WriteLine("DATA: \"{0}\" to \"{1}\"", entry.Signature, entry.Path);

			base.UpdateData(entry, original, data);
		}

		public override void UpdateMeta(CatalogEntry entry, CatalogEntry original)
		{
			Console.WriteLine("META: \"{0}\"", entry.Path);

			base.UpdateMeta(entry, original);
		}

		#endregion Action Methods
	}
}
