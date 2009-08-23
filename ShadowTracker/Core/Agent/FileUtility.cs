using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

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

		/// <summary>
		/// Trickle update rate
		/// </summary>
		private const int DefaultTrickleRate = 100;//milliseconds

		#endregion Constants

		#region Methods

		public static void SyncCatalog(Catalog catalog, string rootPath)
		{
			FileUtility.SyncCatalog(catalog, rootPath, FileUtility.DefaultTrickleRate);
		}

		/// <summary>
		/// Syncs an existing catalog with file system.
		/// </summary>
		/// <param name="rootPath"></param>
		/// <param name="timerDelay">number of milliseconds to wait between each file processed (for trickle updates)</param>
		public static void SyncCatalog(Catalog catalog, string rootPath, int trickleRate)
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

			var files = FileIterator.GetFiles(rootPath, true).Where(FileUtility.FilterFiles);

			if (trickleRate > 0)
			{
				var enumerator = files.GetEnumerator();

				// perform loop with a timer to allow trickle updates
				// use a closure as the callback to allow access to local vars
				Timer timer = null;
				timer = new Timer(
					delegate(object state)
					{
						// check if any files left
						if (!enumerator.MoveNext())
						{
							// free closure references
							timer.Change(Timeout.Infinite, Timeout.Infinite);
							timer = null;
							files = null;
							return;
						}

						// sync next node
						CatalogEntry entry = FileUtility.CreateEntry(rootPath, enumerator.Current);
						catalog.ApplyChanges(entry);

						// queue up next iteration
						timer.Change(trickleRate, Timeout.Infinite);
					},
					null,
					trickleRate,
					Timeout.Infinite);
			}
			else
			{
				foreach (FileSystemInfo file in files)
				{
					// sync each node
					CatalogEntry entry = FileUtility.CreateEntry(rootPath, file);
					catalog.ApplyChanges(entry);
				}
			}

			// TODO: remove any extra files after
			// TODO: try-catch around work and create a retry queue
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
