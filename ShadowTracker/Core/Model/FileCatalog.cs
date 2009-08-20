using System;
using System.IO;
using System.Linq;

using Shadow.Agent;

namespace Shadow.Model
{
	/// <summary>
	/// A catalog built from the file system.
	/// </summary>
	public class FileCatalog : Catalog
	{
		#region Constants

		/// <summary>
		/// Represents the FileAttributes which get tracked.
		/// </summary>
		private const FileAttributes AttribMask = FileAttributes.ReadOnly|FileAttributes.Archive|FileAttributes.Directory;

		/// <summary>
		/// Represents the FileAttributes of files which are not tracked.
		/// </summary>
		private const FileAttributes FilteredFiles = FileAttributes.Hidden|FileAttributes.System|FileAttributes.Temporary;

		#endregion Constants

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="rootPath"></param>
		public FileCatalog(string rootPath)
		{
			if (String.IsNullOrEmpty(rootPath))
			{
				throw new ArgumentNullException("Root is invalid.");
			}

			rootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar);

			var files = FileIterator.GetFiles(rootPath, true).Where(FileCatalog.IsFiltered);
			foreach (FileSystemInfo node in files)
			{
				CatalogEntry entry = FileCatalog.CreateNode(rootPath, node);

				this.Entries.Add(entry);
			}
		}

		#endregion Init

		#region Methods

		private static CatalogEntry CreateNode(string root, FileSystemInfo file)
		{
			return new CatalogEntry
			{
				Path = FileCatalog.NormalizePath(root, file.FullName),
				Attributes = FileCatalog.ScrubAttributes(file.Attributes),
				CreatedDate = file.CreationTime,
				ModifiedDate = file.LastWriteTime,
				Signature = (file is FileInfo) ?
					FileHash.ComputeHash((FileInfo)file) :
					null
			};
		}

		#endregion Methods

		#region Utility Methods

		private static bool IsFiltered(FileSystemInfo node)
		{
			return ((node.Attributes&FileCatalog.FilteredFiles) != 0);
		}

		private static FileAttributes ScrubAttributes(FileAttributes attributes)
		{
			return attributes&FileCatalog.AttribMask;
		}

		/// <summary>
		/// Makes paths root-relative and converts to URL type directory delim (for more compact encoding in C-style languages).
		/// </summary>
		/// <param name="fullPath"></param>
		/// <returns>root-relative paths</returns>
		private static string NormalizePath(string rootPath, string fullPath)
		{
			if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Unexpected path format.");
			}

			return fullPath.Substring(rootPath.Length).Replace(Path.DirectorySeparatorChar, '/');
		}

		#endregion Utility Methods
	}
}
