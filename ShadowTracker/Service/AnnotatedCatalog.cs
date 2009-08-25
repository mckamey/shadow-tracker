using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;

using Shadow.Model;

namespace Shadow.Service
{
	public class AnnotatedCatalog : CatalogRepository
	{
		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <remarks>Defaults to in-memory backing storage</remarks>
		public AnnotatedCatalog()
		{
			this.Log = this.Error = TextWriter.Null;
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="entries">initial items</param>
		public AnnotatedCatalog(IEnumerable<CatalogEntry> entries)
			: base(entries)
		{
			this.Log = this.Error = TextWriter.Null;
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="entries"></param>
		public AnnotatedCatalog(ITable<CatalogEntry> entries)
			: base(entries)
		{
			this.Log = this.Error = TextWriter.Null;
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="db">LINQ-to-SQL DataContext</param>
		public AnnotatedCatalog(DataContext db)
			: base(db)
		{
#if DEBUG
			//this.SetUnitOfWorkLog(Console.Error);
#endif
			this.Log = this.Error = TextWriter.Null;
		}

		#endregion Init

		#region Properties

		public TextWriter Log
		{
			get;
			set;
		}

		public TextWriter Error
		{
			get;
			set;
		}

		#endregion Properties

		#region Action Methods

		public override void AddEntry(CatalogEntry entry)
		{
			this.Log.WriteLine("ADD \"{0}\" at \"{1}\"", entry.Signature, entry.Path);

			base.AddEntry(entry);
		}

		public override void CloneEntry(CatalogEntry entry, CatalogEntry data)
		{
			this.Log.WriteLine("CLONE: \"{0}\" to \"{1}\"", data.Signature, entry.Path);

			base.CloneEntry(entry, data);
		}

		public override void DeleteEntryByPath(string path)
		{
			this.Log.WriteLine("REMOVE: \"{0}\"", path);

			base.DeleteEntryByPath(path);
		}

		public override void RenameEntry(string oldPath, string newPath)
		{
			this.Log.WriteLine("RENAME: \"{0}\" to \"{1}\"", oldPath, newPath);

			base.RenameEntry(oldPath, newPath);
		}

		public override void UpdateData(CatalogEntry entry, CatalogEntry original, CatalogEntry data)
		{
			this.Log.WriteLine("DATA: \"{0}\" to \"{1}\"", entry.Signature, entry.Path);

			base.UpdateData(entry, original, data);
		}

		public override void UpdateMeta(CatalogEntry entry, CatalogEntry original)
		{
			this.Log.WriteLine("META: \"{0}\"", entry.Path);

			base.UpdateMeta(entry, original);
		}

		#endregion Action Methods
	}
}
