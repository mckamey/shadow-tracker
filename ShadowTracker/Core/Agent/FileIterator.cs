using System;
using System.Collections.Generic;
using System.IO;

using Shadow.Model;

namespace Shadow.Agent
{
	internal static class FileIterator
	{
		#region Constants

		private const FileAttributes AttribMask = FileAttributes.ReadOnly|FileAttributes.Archive|FileAttributes.Directory;
		private const FileAttributes FilteredFiles = FileAttributes.Hidden|FileAttributes.System|FileAttributes.Temporary;

		#endregion Constants

		#region Methods

		public static IEnumerable<DataNode> GetFiles(string root)
		{
			DirectoryInfo dir = new DirectoryInfo(root);
			if (!dir.Exists)
			{
				yield break;
			}

			Stack<DirectoryInfo> stack = new Stack<DirectoryInfo>(100);
			stack.Push(dir);

			while (stack.Count > 0)
			{
				dir = stack.Pop();

				FileSystemInfo[] files = dir.GetFileSystemInfos();
				if (files.Length == 0)
				{
					// emit a node for empty directories
					Console.Write('.');
					yield return new DataNode
					{
						Path = FileIterator.NormalizePath(root, dir.FullName),
						Attributes = FileIterator.ScrubAttributes(dir.Attributes),
						CreatedDate = dir.CreationTime,
						ModifiedDate = dir.LastWriteTime
					};
				}

				foreach (FileSystemInfo info in files)
				{
					if (info is DirectoryInfo)
					{
						stack.Push((DirectoryInfo)info);
						continue;
					}

					if (IsFiltered(info.Attributes))
					{
						continue;
					}

					FileInfo file = info as FileInfo;
					if (file != null)
					{
						Console.Write('.');
						yield return new DataNode
						{
							Path = FileIterator.NormalizePath(root, file.FullName),
							Attributes = FileIterator.ScrubAttributes(file.Attributes),
							CreatedDate = file.CreationTime,
							ModifiedDate = file.LastWriteTime,
							Signature = FileHash.ComputeHash(file)
						};
					}
				}
			}
		}

		#endregion Methods

		#region Utility Methods

		private static FileAttributes ScrubAttributes(FileAttributes attributes)
		{
			return attributes&FileIterator.AttribMask;
		}

		private static bool IsFiltered(FileAttributes attributes)
		{
			return ((attributes&FileIterator.FilteredFiles) != 0);
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
