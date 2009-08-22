using System;
using System.IO;
using System.Linq;

using Shadow.Agent;

namespace Shadow.Model
{
	/// <summary>
	/// A catalog built initialized via the file system.
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

		#region Fields

		public readonly string RootPath;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="rootPath"></param>
		public FileCatalog(string rootPath, ITable<CatalogEntry> entries)
		{
			if (String.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
			{
				throw new ArgumentNullException("rootPath", "Root path is invalid.");
			}
			if (entries == null)
			{
				throw new ArgumentNullException("entries", "Entries table is null.");
			}

			this.Entries = entries;
			this.RootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar);

			var files = FileIterator.GetFiles(this.RootPath, true).Where(FileCatalog.FilterFiles);
			foreach (FileSystemInfo node in files)
			{
				CatalogEntry entry = FileCatalog.CreateNode(this.RootPath, node);

				this.Entries.Add(entry);
			}
		}

		#endregion Init

		#region Methods

		/// <summary>
		/// 
		/// </summary>
		/// <exception cref="System.UnauthorizedAccessException">The path is read-only or is a directory.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
		/// <exception cref="System.IO.IOException">The file is already open.</exception>
		public static CatalogEntry CreateNode(string root, FileSystemInfo file)
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

		/// <summary>
		/// Returns true if passes, false if is to be filtered.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private static bool FilterFiles(FileSystemInfo node)
		{
			return ((node.Attributes&FileCatalog.FilteredFiles) == 0);
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
		public static string NormalizePath(string rootPath, string fullPath)
		{
			rootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
			if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Unexpected path format.");
			}

			return fullPath.Substring(rootPath.Length).Replace(Path.DirectorySeparatorChar, '/');
		}

		#endregion Utility Methods
	}
}
