using System;
using System.IO;

using Shadow.Agent;

namespace Shadow.Model
{
	public class DataNodeFactory
	{
		#region Constants

		private const FileAttributes AttribMask = FileAttributes.ReadOnly|FileAttributes.Archive|FileAttributes.Directory;

		#endregion Constants

		#region Methods

		public static CatalogEntry CreateNode(string root, DirectoryInfo dir)
		{
			return new CatalogEntry
			{
				Path = DataNodeFactory.NormalizePath(root, dir.FullName),
				Attributes = DataNodeFactory.ScrubAttributes(dir.Attributes),
				CreatedDate = dir.CreationTime,
				ModifiedDate = dir.LastWriteTime
			};
		}

		public static CatalogEntry CreateNode(string root, FileInfo file)
		{
			return new CatalogEntry
			{
				Path = DataNodeFactory.NormalizePath(root, file.FullName),
				Attributes = DataNodeFactory.ScrubAttributes(file.Attributes),
				CreatedDate = file.CreationTime,
				ModifiedDate = file.LastWriteTime,
				Signature = FileHash.ComputeHash(file)
			};
		}

		#endregion Methods

		#region Utility Methods

		private static FileAttributes ScrubAttributes(FileAttributes attributes)
		{
			return attributes&DataNodeFactory.AttribMask;
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
