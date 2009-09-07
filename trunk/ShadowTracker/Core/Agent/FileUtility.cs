using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Shadow.Model;

namespace Shadow.Agent
{
	/// <summary>
	/// Utility for synchronizing a catalog with the file system.
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
		public const int DefaultTrickleRate = 250;//milliseconds

		#endregion Constants

		#region Methods

		/// <summary>
		/// Syncs an existing catalog with the file system.
		/// </summary>
		/// <param name="rootPath"></param>
		public static void SyncCatalog(string name, string rootPath)
		{
			FileUtility.SyncCatalog(name, rootPath, FileUtility.CreateFileFilter());
		}

		/// <summary>
		/// Syncs an existing catalog with the file system.
		/// </summary>
		/// <param name="rootPath"></param>
		/// <param name="fileFilter">function that returns true if passes, false if is to be filtered</param>
		public static void SyncCatalog(string name, string rootPath, Func<FileSystemInfo, bool> fileFilter)
		{
			FileUtility.SyncCatalog(name, rootPath, fileFilter, -1, null);
		}

		/// <summary>
		/// Syncs an existing catalog with the file system.
		/// </summary>
		/// <param name="rootPath"></param>
		/// <param name="fileFilter">function that returns true if passes, false if is to be filtered</param>
		/// <param name="trickleRate">number of milliseconds to wait between each file processed (for trickle updates)</param>
		public static void SyncCatalog(
			string name,
			string rootPath,
			Func<FileSystemInfo, bool> fileFilter,
			int trickleRate,
			Action<string> completedCallback)
		{
			if (String.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
			{
				throw new ArgumentNullException("rootPath", "Root path is invalid.");
			}

			rootPath = FileUtility.EnsureTrailingSlash(rootPath);
			Catalog catalog = CatalogRepository.EnsureCatalog(UnitOfWorkFactory.Create(), name, rootPath);

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
							FileUtility.RemoveExtras(catalog, trickleRate, completedCallback);
							return;
						}

						// sync next node
						CheckForChanges(catalog, enumerator.Current);

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
					CheckForChanges(catalog, file);
				}

				// remove any extra files after, so more clones can happen
				FileUtility.RemoveExtras(catalog, trickleRate, completedCallback);
			}

			// TODO: try-catch around work and create a retry queue
		}

		private static void CheckForChanges(Catalog catalog, FileSystemInfo file)
		{
			CatalogEntry entry = FileUtility.CreateEntry(catalog, file);

			IUnitOfWork unitOfWork = UnitOfWorkFactory.Create();
			CatalogRepository repos = new CatalogRepository(unitOfWork, catalog);

			if (repos.ApplyChanges(entry))
			{
				unitOfWork.Save();
			}
		}

		private static void RemoveExtras(
			Catalog catalog,
			int trickleRate,
			Action<string> completedCallback)
		{
			CatalogRepository repos = new CatalogRepository(UnitOfWorkFactory.Create(), catalog);
			if (trickleRate > 0)
			{
				var enumerator = repos.GetExistingPaths().GetEnumerator();

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
								completedCallback(catalog.Path);
							}
							return;
						}

						// extras are any local entries not contained on disk
						string path = enumerator.Current;
						CheckIfMissing(catalog, path);

						// queue up next iteration
						timer.Change(trickleRate, Timeout.Infinite);
					},
					null,
					trickleRate,
					Timeout.Infinite);
			}
			else
			{
				foreach (string path in repos.GetExistingPaths())
				{
					// extras are any local entries not contained on disk
					CheckIfMissing(catalog, path);
				}

				// signal sync is complete
				if (completedCallback != null)
				{
					completedCallback(catalog.Path);
				}
			}
		}

		private static void CheckIfMissing(Catalog catalog, string path)
		{
			string fullPath = FileUtility.DenormalizePath(catalog.Path, path);
			if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
			{
				IUnitOfWork unitOfWork = UnitOfWorkFactory.Create();
				CatalogRepository repos = new CatalogRepository(unitOfWork, catalog);

				repos.DeleteEntryByPath(path);
				unitOfWork.Save();
			}
		}

		/// <summary>
		/// Builds a CatalogEntry from a file system descriptor. (Requires read access)
		/// </summary>
		/// <exception cref="System.UnauthorizedAccessException">The path is read-only or is a directory.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
		/// <exception cref="System.IO.IOException">The file is already open.</exception>
		public static CatalogEntry CreateEntry(Catalog catalog, FileSystemInfo file)
		{
			DirectoryInfo parent =
				file is FileInfo ?
				((FileInfo)file).Directory :
				((DirectoryInfo)file).Parent;

			return new CatalogEntry
			{
				Attributes = (file.Attributes&FileUtility.AttribMask),
				CatalogID = catalog.ID,
				CreatedDate = file.CreationTimeUtc,
				Length = (file is FileInfo) ?
					((FileInfo)file).Length :
					0L,
				ModifiedDate = file.LastWriteTimeUtc,
				Name = file.Name,
				Parent = FileUtility.NormalizePath(catalog.Path, parent.FullName),
				Signature = (file is FileInfo) ?
					FileHash.ComputeHash((FileInfo)file) :
					null,
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
			rootPath = FileUtility.TrimTrailingSlash(rootPath);

			if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Unexpected path format.");
			}

			return fullPath.Substring(rootPath.Length).Replace(Path.DirectorySeparatorChar, '/');
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rootPath"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		private static string DenormalizePath(string rootPath, string path)
		{
			path = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

			return Path.Combine(rootPath, path);
		}

		public static void SplitPath(string path, out string parent, out string name)
		{
			int index = path.LastIndexOf('/');
			parent = 
				(index < 0) ? "/" :
				path.Substring(0, index)+'/';
			name = path.Substring(index+1);
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
			rootPath = FileUtility.EnsureTrailingSlash(rootPath);
			newRoot = FileUtility.EnsureTrailingSlash(newRoot);
			if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Unexpected path format.");
			}

			return Path.Combine(newRoot, fullPath.Substring(rootPath.Length));
		}

		public static string EnsureTrailingSlash(string path)
		{
			return path.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
		}

		public static string TrimTrailingSlash(string path)
		{
			return path.TrimEnd(Path.DirectorySeparatorChar);
		}

		#endregion Utility Methods
	}
}
