using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.Practices.ServiceLocation;
using Shadow.Model;

namespace Shadow.Agent
{
	internal static class EnumerableExtensions
	{
		#region Methods

		public static void TrickleIterate<T>(
			this IEnumerable<T> items,
			int trickleRate,
			Action<T> doWork,
			Action onCompleted,
			Action<Exception> onFailure)
		{
			if (trickleRate > 0)
			{
				var enumerator = items.GetEnumerator();

				// perform loop with a timer to allow trickle iteration
				// use a closure as the callback to allow access to local vars
				Timer timer = null;
				timer = new Timer(
					delegate(object state)
					{
						try
						{
							// check if any left
							if (!enumerator.MoveNext())
							{
								if (onCompleted != null)
								{
									onCompleted();
								}
								return;
							}

							// perform work
							doWork(enumerator.Current);
						}
						catch (Exception ex)
						{
							if (onFailure == null)
							{
								throw;
							}
							onFailure(ex);

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
				foreach (T item in items)
				{
					try
					{
						// sync each node
						doWork(item);
					}
					catch (Exception ex)
					{
						if (onFailure == null)
						{
							throw;
						}
						onFailure(ex);

						// TODO: create a retry queue
					}
				}

				if (onCompleted != null)
				{
					onCompleted();
				}
			}
		}

		#endregion Methods
	}

	/// <summary>
	/// Utility for synchronizing a catalog with the file system.
	/// </summary>
	public class FileUtility
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

		#region Fields

		private readonly IServiceLocator IoC;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="ioc"></param>
		public FileUtility(IServiceLocator ioc)
		{
			this.IoC = ioc;
		}

		#endregion Init

		#region Methods

		/// <summary>
		/// Syncs an existing catalog with the file system.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="rootPath"></param>
		/// <param name="fileFilter">function that returns true if passes, false if is to be filtered</param>
		/// <param name="trickleRate">number of milliseconds to wait between each file processed (for trickle updates)</param>
		/// <param name="completedCallback"></param>
		/// <param name="failureCallback"></param>
		public void SyncCatalog(
			string name,
			string rootPath,
			Func<FileSystemInfo, bool> fileFilter,
			int trickleRate,
			Action<Catalog> onCompleted,
			Action<Catalog, Exception> onFailure)
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

			Catalog catalog = this.IoC.GetInstance<CatalogRepository>().FindOrCreateCatalog(name, rootPath);

			// remove any extra files first to ensure references stay intact
			this.RemoveExtras(
				catalog,
				trickleRate,
				delegate(Catalog c)
				{
					// find any new or updated files after
					this.FindChanged(catalog, rootPath, trickleRate, fileFilter, onCompleted, onFailure);
				},
				onFailure);
		}

		private void FindChanged(
			Catalog catalog,
			string rootPath,
			int trickleRate,
			Func<FileSystemInfo, bool> fileFilter,
			Action<Catalog> onCompleted,
			Action<Catalog, Exception> onFailure)
		{
			CatalogRepository repos = this.IoC.GetInstance<CatalogRepository>();
			var files = FileIterator.GetFiles(rootPath, true).Where(fileFilter);

			files.TrickleIterate(
				trickleRate,
				delegate(FileSystemInfo file)
				{
					CatalogEntry entry = FileUtility.CreateEntry(catalog.ID, catalog.Path, file, !catalog.IsIndexed);
					if (repos.AddOrUpdate(entry, file as FileInfo))
					{
						repos.Save();
					}
				},
				delegate()
				{
					if (onCompleted != null)
					{
						onCompleted(catalog);
					}
				},
				delegate(Exception ex)
				{
					if (onFailure == null)
					{
						throw ex;
					}
					onFailure(catalog, ex);
				});
		}

		private void RemoveExtras(
			Catalog catalog,
			int trickleRate,
			Action<Catalog> onCompleted,
			Action<Catalog, Exception> onFailure)
		{
			CatalogRepository repos = this.IoC.GetInstance<CatalogRepository>();
			var paths = repos.GetExistingPaths(catalog.ID);

			paths.TrickleIterate(
				trickleRate,
				delegate(string path)
				{
					string fullPath = FileUtility.DenormalizePath(catalog.Path, path);
					if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
					{
						repos.DeleteEntryByPath(catalog.ID, path);
						repos.Save();
					}
				},
				delegate()
				{
					// flag catalog as indexed
					if (!catalog.IsIndexed)
					{
						catalog.IsIndexed = true;
						repos.Catalogs.Update(catalog);
						repos.Save();
					}

					// signal extras are removed
					if (onCompleted != null)
					{
						onCompleted(catalog);
					}
				},
				delegate(Exception ex)
				{
					if (onFailure == null)
					{
						throw ex;
					}
					onFailure(catalog, ex);
				});
		}

		/// <summary>
		/// Builds a CatalogEntry from a file system descriptor. (Requires read access)
		/// </summary>
		/// <param name="catalogID"></param>
		/// <param name="catalogPath"></param>
		/// <param name="file"></param>
		/// <exception cref="System.UnauthorizedAccessException">The path is read-only or is a directory.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
		/// <exception cref="System.IO.IOException">The file is already open.</exception>
		[System.Diagnostics.DebuggerStepThrough]
		internal static CatalogEntry CreateEntry(long catalogID, string catalogPath, FileSystemInfo file)
		{
			return FileUtility.CreateEntry(catalogID, catalogPath, file, true);
		}

		/// <summary>
		/// Builds a CatalogEntry from a file system descriptor. (Requires read access)
		/// </summary>
		/// <param name="catalogID"></param>
		/// <param name="catalogPath"></param>
		/// <param name="file"></param>
		/// <param name="calcHash"></param>
		/// <exception cref="System.UnauthorizedAccessException">The path is read-only or is a directory.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
		/// <exception cref="System.IO.IOException">The file is already open.</exception>
		private static CatalogEntry CreateEntry(long catalogID, string catalogPath, FileSystemInfo file, bool calcHash)
		{
			FileInfo fileInfo = file as FileInfo;

			DirectoryInfo parent =
				(fileInfo != null) ?
				fileInfo.Directory :
				((DirectoryInfo)file).Parent;

			long length = (fileInfo != null && fileInfo.Exists) ? fileInfo.Length : 0L;

			CatalogEntry entry = new CatalogEntry
			{
				Attributes = (file.Attributes&FileUtility.AttribMask),
				CatalogID = catalogID,
				CreatedDate = file.CreationTimeUtc,
				Length = length,
				ModifiedDate = file.LastWriteTimeUtc,
				Name = file.Name,
				Parent = FileUtility.NormalizePath(catalogPath, parent.FullName)
			};

			if (calcHash && fileInfo != null)
			{
				entry.Signature = FileHash.ComputeHash(fileInfo);
			}

			return entry;
		}

		[System.Diagnostics.DebuggerStepThrough]
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

		internal static FileSystemInfo CreateFileSystemInfo(string path)
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
		internal static string NormalizePath(string rootPath, string fullPath)
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

		internal static void SplitPath(string path, out string parent, out string name)
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
		internal static string ReplaceRoot(string rootPath, string fullPath, string newRoot)
		{
			rootPath = FileUtility.EnsureTrailingSlash(rootPath);
			newRoot = FileUtility.EnsureTrailingSlash(newRoot);
			if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Unexpected path format.");
			}

			return Path.Combine(newRoot, fullPath.Substring(rootPath.Length));
		}

		internal static string EnsureTrailingSlash(string path)
		{
			return path.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
		}

		internal static string TrimTrailingSlash(string path)
		{
			return path.TrimEnd(Path.DirectorySeparatorChar);
		}

		#endregion Utility Methods
	}
}
