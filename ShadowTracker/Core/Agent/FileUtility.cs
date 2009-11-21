using System;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.Practices.ServiceLocation;
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
			FileUtility.SyncCatalog(name, rootPath, fileFilter, -1, null, null);
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
			Action<Catalog> completedCallback,
			Action<Catalog, Exception> failureCallback)
		{
			if (String.IsNullOrEmpty(rootPath))
			{
				throw new ArgumentNullException("rootPath", "Root path is invalid.");
			}
			if (!Directory.Exists(rootPath))
			{
				throw new ArgumentException("Root path is invalid.", "rootPath");
			}

			rootPath = FileUtility.EnsureTrailingSlash(rootPath);
			Catalog catalog = CatalogRepository.EnsureCatalog(ServiceLocator.Current.GetInstance<IUnitOfWork>(), name, rootPath);

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
						try
						{
							// check if any files left
							if (!enumerator.MoveNext())
							{
								// free closure references
								timer.Change(Timeout.Infinite, Timeout.Infinite);
								timer = null;
								files = null;

								// remove any extra files after, so more clones can happen
								FileUtility.RemoveExtras(catalog, trickleRate, completedCallback, failureCallback);
								return;
							}

							// sync next node
							CheckForChanges(catalog, enumerator.Current);
						}
						catch (Exception ex)
						{
							if (failureCallback == null)
							{
								throw;
							}
							failureCallback(catalog, ex);

							// TODO: create a retry queue
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
				foreach (FileSystemInfo file in files)
				{
					try
					{
						// sync each node
						CheckForChanges(catalog, file);
					}
					catch (Exception ex)
					{
						if (failureCallback == null)
						{
							throw;
						}
						failureCallback(catalog, ex);

						// TODO: create a retry queue
					}
				}

				// remove any extra files after, so more clones can happen
				FileUtility.RemoveExtras(catalog, trickleRate, completedCallback, failureCallback);
			}
		}

		private static void CheckForChanges(Catalog catalog, FileSystemInfo file)
		{
			IUnitOfWork unitOfWork = ServiceLocator.Current.GetInstance<IUnitOfWork>();
			CatalogRepository repos = new CatalogRepository(unitOfWork, catalog);

			CatalogEntry entry = FileUtility.CreateEntry(catalog, file, !catalog.IsIndexed);
			if (repos.ApplyChanges(entry, file as FileInfo))
			{
				unitOfWork.Save();
			}
		}

		private static void RemoveExtras(
			Catalog catalog,
			int trickleRate,
			Action<Catalog> completedCallback,
			Action<Catalog, Exception> failureCallback)
		{
			IUnitOfWork unitOfWork = ServiceLocator.Current.GetInstance<IUnitOfWork>();
			CatalogRepository repos = new CatalogRepository(unitOfWork, catalog);
			if (trickleRate > 0)
			{
				var enumerator = repos.GetExistingPaths().GetEnumerator();

				// perform loop with a timer to allow trickle updates
				// use a closure as the callback to allow access to local vars
				Timer timer = null;
				timer = new Timer(
					delegate(object state)
					{
						try
						{
							// check if any files left
							if (!enumerator.MoveNext())
							{
								// free closure references
								timer.Change(Timeout.Infinite, Timeout.Infinite);
								timer = null;

								// flag catalog as indexed
								if (!catalog.IsIndexed)
								{
									catalog.IsIndexed = true;
									unitOfWork.Catalogs.Update(catalog);
									unitOfWork.Save();
								}

								// signal sync is complete
								if (completedCallback != null)
								{
									completedCallback(catalog);
								}
								return;
							}

							// extras are any local entries not contained on disk
							string path = enumerator.Current;
							FileUtility.CheckIfMissing(catalog, path);
						}
						catch (Exception ex)
						{
							if (failureCallback == null)
							{
								throw;
							}
							failureCallback(catalog, ex);

							// TODO: create a retry queue
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
				foreach (string path in repos.GetExistingPaths())
				{
					try
					{
						// extras are any local entries not contained on disk
						FileUtility.CheckIfMissing(catalog, path);
					}
					catch (Exception ex)
					{
						if (failureCallback == null)
						{
							throw;
						}
						failureCallback(catalog, ex);

						// TODO: create a retry queue
					}
				}

				// flag catalog as indexed
				if (!catalog.IsIndexed)
				{
					catalog.IsIndexed = true;
					unitOfWork.Catalogs.Update(catalog);
					unitOfWork.Save();
				}

				// signal sync is complete
				if (completedCallback != null)
				{
					completedCallback(catalog);
				}
			}
		}

		private static void CheckIfMissing(Catalog catalog, string path)
		{
			string fullPath = FileUtility.DenormalizePath(catalog.Path, path);
			if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
			{
				IUnitOfWork unitOfWork = ServiceLocator.Current.GetInstance<IUnitOfWork>();
				CatalogRepository repos = new CatalogRepository(unitOfWork, catalog);

				repos.DeleteEntryByPath(path);
				unitOfWork.Save();
			}
		}

		/// <summary>
		/// Builds a CatalogEntry from a file system descriptor. (Requires read access)
		/// </summary>
		/// <param name="catalog"></param>
		/// <param name="file"></param>
		/// <exception cref="System.UnauthorizedAccessException">The path is read-only or is a directory.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
		/// <exception cref="System.IO.IOException">The file is already open.</exception>
		public static CatalogEntry CreateEntry(Catalog catalog, FileSystemInfo file)
		{
			return FileUtility.CreateEntry(catalog, file, true);
		}

		/// <summary>
		/// Builds a CatalogEntry from a file system descriptor. (Requires read access)
		/// </summary>
		/// <param name="catalog"></param>
		/// <param name="file"></param>
		/// <param name="calcHash"></param>
		/// <exception cref="System.UnauthorizedAccessException">The path is read-only or is a directory.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
		/// <exception cref="System.IO.IOException">The file is already open.</exception>
		private static CatalogEntry CreateEntry(Catalog catalog, FileSystemInfo file, bool calcHash)
		{
			FileInfo fileInfo = file as FileInfo;

			DirectoryInfo parent =
				fileInfo != null ?
				fileInfo.Directory :
				((DirectoryInfo)file).Parent;

			CatalogEntry entry = new CatalogEntry
			{
				Attributes = (file.Attributes&FileUtility.AttribMask),
				CatalogID = catalog.ID,
				CreatedDate = file.CreationTimeUtc,
				Length =
					(fileInfo != null) ?
					fileInfo.Length :
					0L,
				ModifiedDate = file.LastWriteTimeUtc,
				Name = file.Name,
				Parent = FileUtility.NormalizePath(catalog.Path, parent.FullName)
			};

			if (calcHash && fileInfo != null)
			{
				entry.Signature = FileHash.ComputeHash(fileInfo);
			}

			return entry;
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
				// is a file
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

			parent = path.Substring(0, index+1);
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
