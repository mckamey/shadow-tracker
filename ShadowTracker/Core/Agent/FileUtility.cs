using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

using MimeUtils;
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
		/// Builds a CatalogEntry from a file system descriptor. (Requires read access)
		/// </summary>
		/// <param name="catalogID"></param>
		/// <param name="catalogPath"></param>
		/// <param name="file"></param>
		/// <exception cref="System.UnauthorizedAccessException">The path is read-only or is a directory.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
		/// <exception cref="System.IO.IOException">The file is already open.</exception>
		[DebuggerStepThrough]
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
		internal static CatalogEntry CreateEntry(long catalogID, string catalogPath, FileSystemInfo file, bool calcHash)
		{
			file.Refresh();

			FileInfo fileInfo = file as FileInfo;

			DirectoryInfo parent =
				(fileInfo != null) ?
				fileInfo.Directory :
				((DirectoryInfo)file).Parent;

			long length = (fileInfo != null && fileInfo.Exists) ? fileInfo.Length : 0L;

			MimeType mimeType =
				(fileInfo != null) ?
				MimeTypes.GetByExtension(fileInfo.Extension) :
				MimeType.Empty;

			CatalogEntry entry = new CatalogEntry
			{
				Attributes = (file.Attributes&FileUtility.AttribMask),
				CatalogID = catalogID,
				ContentType = mimeType.ContentType,
				CreatedDate = file.CreationTimeUtc,
				Length = length,
				MimeCategory = (file is DirectoryInfo) ? MimeCategory.Folder : mimeType.Category,
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

		[DebuggerStepThrough]
		public static Func<FileSystemInfo, bool> CreateFileFilter(params string[] trackedExtensions)
		{
			return CreateFileFilter(FileUtility.DefaultFilteredAttribs, trackedExtensions);
		}

		public static Func<FileSystemInfo, bool> CreateFileFilter(FileAttributes filteredAttribs, params string[] trackedExtensions)
		{
			return delegate(FileSystemInfo node)
			{
				if (node is DirectoryInfo ||
					(!node.Exists && String.IsNullOrEmpty(node.Extension)))
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
			if (Directory.Exists(path) ||
				(String.IsNullOrEmpty(Path.GetExtension(path)) && !File.Exists(path)))
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
		/// Makes paths root-relative and converts to URL-style directory delim (for more compact encoding in C-style strings).
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
		public static string DenormalizePath(string rootPath, string path)
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
