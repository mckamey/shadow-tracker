using System;
using System.IO;

using Shadow.Model;

namespace Shadow.Agent
{
	public class Cataloger
	{
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

			catalog.Entries = FileIterator.GetFiles(this.RootPath);

			return catalog;
		}

		#endregion Methods
	}
}
