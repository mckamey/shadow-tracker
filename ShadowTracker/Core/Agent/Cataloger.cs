using System;
using System.IO;

using Shadow.Model;

namespace Shadow.Agent
{
	public class Cataloger
	{
		#region Constants

		private const FileAttributes FilteredFiles = FileAttributes.Hidden|FileAttributes.System|FileAttributes.Temporary;

		#endregion Constants

		#region Fields

		private readonly string RootPath;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="rootPath"></param>
		public Cataloger(string rootPath)
		{
			if (String.IsNullOrEmpty(rootPath))
			{
				throw new ArgumentNullException("Root is invalid.");
			}

			this.RootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar);
		}

		#endregion Init

		#region Methods

		public Catalog CreateCatalog()
		{
			Catalog catalog = new Catalog();

			foreach (CatalogEntry entry in FileIterator.GetFiles(this.RootPath, Cataloger.FilteredFiles, true))
			{
				catalog.AddEntry(entry);
			}

			return catalog;
		}

		#endregion Methods
	}
}
