﻿using System;
using System.Collections.Generic;
using System.IO;

using Shadow.Model;

namespace Shadow.Agent
{
	internal static class FileIterator
	{
		#region Methods

		public static IEnumerable<FileSystemInfo> GetFiles(string root, FileAttributes filteredFiles, bool listEmptyDirs)
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
				if (listEmptyDirs && files.Length == 0)
				{
					// create a node for empty directories
					yield return dir;
				}

				foreach (FileSystemInfo info in files)
				{
					if (info is DirectoryInfo)
					{
						stack.Push((DirectoryInfo)info);
						continue;
					}

					if (IsFiltered(info.Attributes, filteredFiles))
					{
						continue;
					}

					if (info is FileInfo)
					{
						yield return info;
					}
				}
			}
		}

		#endregion Methods

		#region Utility Methods

		private static bool IsFiltered(FileAttributes attributes, FileAttributes filteredFiles)
		{
			return ((attributes&filteredFiles) != 0);
		}

		#endregion Utility Methods
	}
}