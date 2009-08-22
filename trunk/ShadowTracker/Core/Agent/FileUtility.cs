using System;
using System.IO;
using System.Linq;

using Shadow.Model;

namespace Shadow.Agent
{
	/// <summary>
	/// A catalog built initialized via the file system.
	/// </summary>
	public static class FileUtility
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

		#region Methods

		/// <summary>
		/// Syncs an existing catalog with file system.
		/// </summary>
		/// <param name="rootPath"></param>
		public static void SyncCatalog(Catalog catalog, string rootPath)
		{
			if (catalog == null)
			{
				throw new ArgumentNullException("catalog", "Catalog is null.");
			}
			if (String.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
			{
				throw new ArgumentNullException("rootPath", "Root path is invalid.");
			}

			rootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar);

			// TODO: perform this loop with a timer to allow trickle updates
			var files = FileIterator.GetFiles(rootPath, true).Where(FileUtility.FilterFiles);
			foreach (FileSystemInfo node in files)
			{
				CatalogEntry entry = FileUtility.CreateEntry(rootPath, node);

				catalog.ApplyChanges(entry);
			}

			// TODO: remove any extra files here
		}

		/// <summary>
		/// Builds a CatalogEntry from a file system descriptor. (Requires read access)
		/// </summary>
		/// <exception cref="System.UnauthorizedAccessException">The path is read-only or is a directory.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
		/// <exception cref="System.IO.IOException">The file is already open.</exception>
		public static CatalogEntry CreateEntry(string root, FileSystemInfo file)
		{
			return new CatalogEntry
			{
				Path = FileUtility.NormalizePath(root, file.FullName),
				Attributes = FileUtility.ScrubAttributes(file.Attributes),
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
			return ((node.Attributes&FileUtility.FilteredFiles) == 0);
		}

		private static FileAttributes ScrubAttributes(FileAttributes attributes)
		{
			return attributes&FileUtility.AttribMask;
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
