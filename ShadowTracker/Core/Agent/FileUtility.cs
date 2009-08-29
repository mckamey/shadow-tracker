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

		/// Represents the only FileAttributes which get tracked.
		/// </summary>
		private const FileAttributes AttribMask = FileAttributes.ReadOnly|FileAttributes.Archive|FileAttributes.Directory;

		/// <summary>
		/// Represents the FileAttributes of files which are not tracked.
		/// </summary>
		private const FileAttributes DefaultFilteredAttribs = FileAttributes.Hidden|FileAttributes.System|FileAttributes.Temporary;

		/// <summary>
		/// Trickle update rate
		/// </summary>
		public const int DefaultTrickleRate = 100;//milliseconds

		#endregion Constants

		#region Methods

		/// <summary>
		/// Syncs an existing catalog with the file system.
		/// </summary>
		/// <param name="catalog"></param>
		/// <param name="rootPath"></param>
		public static void SyncCatalog(CatalogRepository catalog, string rootPath)
		{
			FileUtility.SyncCatalog(catalog, rootPath, FileUtility.CreateFileFilter());
		}

		/// <summary>
		/// Syncs an existing catalog with the file system.
		/// </summary>
		/// <param name="catalog"></param>
		/// <param name="rootPath"></param>
		/// <param name="fileFilter">function that returns true if passes, false if is to be filtered</param>
		public static void SyncCatalog(CatalogRepository catalog, string rootPath, Func<FileSystemInfo, bool> fileFilter)
		{
			FileUtility.SyncCatalog(catalog, rootPath, fileFilter, -1, null);
		}

		/// <summary>
		/// Syncs an existing catalog with the file system.
		/// </summary>
		/// <param name="catalog"></param>
		/// <param name="rootPath"></param>
		/// <param name="fileFilter">function that returns true if passes, false if is to be filtered</param>
		/// <param name="trickleRate">number of milliseconds to wait between each file processed (for trickle updates)</param>
		public static void SyncCatalog(
			CatalogRepository catalog,
			string rootPath,
			Func<FileSystemInfo, bool> fileFilter,
			int trickleRate,
			Action<CatalogRepository> completedCallback)
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

			var files = FileIterator.GetFiles(rootPath, true).Where(fileFilter);

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

							// remove any extra files after, so more clones can happen
							FileUtility.RemoveExtras(catalog, rootPath, trickleRate, completedCallback);
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

				// remove any extra files after, so more clones can happen
				FileUtility.RemoveExtras(catalog, rootPath, trickleRate, completedCallback);
			}

			// TODO: try-catch around work and create a retry queue
		}

		private static void RemoveExtras(
			CatalogRepository catalog,
			string rootPath,
			int trickleRate,
			Action<CatalogRepository> completedCallback)
		{
			if (trickleRate > 0)
			{
				var enumerator = catalog.GetExistingPaths().GetEnumerator();

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

							// signal sync is complete
							if (completedCallback != null)
							{
								completedCallback(catalog);
							}
							return;
						}

						// sync next node
						string path = enumerator.Current;
						if (!File.Exists(Path.Combine(rootPath, path)))
						{
							catalog.DeleteEntryByPath(path);
						}

						// queue up next iteration
						timer.Change(trickleRate, Timeout.Infinite);
					},
					null,
					trickleRate,
					Timeout.Infinite);
			}
			else
			{
				foreach (string path in catalog.GetExistingPaths())
				{
					// extras are any local entries not contained in other
					if (File.Exists(Path.Combine(rootPath, path)))
					{
						catalog.DeleteEntryByPath(path);
					}
				}

				// signal sync is complete
				if (completedCallback != null)
				{
					completedCallback(catalog);
				}
			}
		}

		/// <summary>
		/// Builds a CatalogEntry from a file system descriptor. (Requires read access)
		/// </summary>
		/// <exception cref="System.UnauthorizedAccessException">The path is read-only or is a directory.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
		/// <exception cref="System.IO.IOException">The file is already open.</exception>
		public static CatalogEntry CreateEntry(string rootPath, FileSystemInfo file)
		{
			return new CatalogEntry
			{
				Path = FileUtility.NormalizePath(rootPath, file.FullName),
				Attributes = (file.Attributes&FileUtility.AttribMask),
				CreatedDate = file.CreationTimeUtc,
				ModifiedDate = file.LastWriteTimeUtc,
				Signature = (file is FileInfo) ?
					FileHash.ComputeHash((FileInfo)file) :
					null,
				Length = (file is FileInfo) ?
					((FileInfo)file).Length :
					0L
			};
		}

		public static Func<FileSystemInfo, bool> CreateFileFilter(params string[] trackedExtensions)
		{
			return CreateFileFilter(FileUtility.DefaultFilteredAttribs, trackedExtensions);
		}

		public static Func<FileSystemInfo, bool> CreateFileFilter(FileAttributes filteredAttribs, params string[] trackedExtensions)
		{
			return delegate(FileSystemInfo node)
			{
				if (node is DirectoryInfo ||
					!node.Exists && String.IsNullOrEmpty(node.Extension))
				{
					return true;
				}

				if (node.Attributes > 0 && (node.Attributes&filteredAttribs) != 0)
				{
					return false;
				}

				if (trackedExtensions == null || trackedExtensions.Length == 0)
				{
					return true;
				}

				return trackedExtensions.Contains(node.Extension, StringComparer.OrdinalIgnoreCase);
			};
		}

		#endregion Methods

		#region Utility Methods

		public static FileSystemInfo CreateFileSystemInfo(string path)
		{
			if (Directory.Exists(path))
			{
				// is a directory
				return new DirectoryInfo(path);
			}
			else
			{
				return new FileInfo(path);
			}
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

		/// <summary>
		/// Replaces the root with another
		/// </summary>
		/// <param name="rootPath"></param>
		/// <param name="fullPath"></param>
		/// <param name="newRoot"></param>
		/// <returns></returns>
		public static string ReplaceRoot(string rootPath, string fullPath, string newRoot)
		{
			rootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
			newRoot = newRoot.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
			if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Unexpected path format.");
			}

			return Path.Combine(newRoot, fullPath.Substring(rootPath.Length));
		}

		#endregion Utility Methods
	}
}
