using System;
using System.Collections.Generic;
using System.IO;

namespace Shadow.Agent
{
	internal static class FileIterator
	{
		#region Methods

		public static IEnumerable<FileSystemInfo> GetFiles(string rootPath)
		{
			return FileIterator.GetFiles(rootPath, true);
		}

		public static IEnumerable<FileSystemInfo> GetFiles(string rootPath, bool listDirs)
		{
			DirectoryInfo root = new DirectoryInfo(rootPath);
			if (!root.Exists)
			{
				yield break;
			}

			Queue<DirectoryInfo> queue = new Queue<DirectoryInfo>(100);
			queue.Enqueue(root);

			while (queue.Count > 0)
			{
				DirectoryInfo parent = queue.Dequeue();
				if (!parent.Exists)
				{
					continue;
				}

				FileSystemInfo[] children = parent.GetFileSystemInfos();
				foreach (FileSystemInfo info in children)
				{
					if (info is DirectoryInfo)
					{
						// queue up subtree
						queue.Enqueue((DirectoryInfo)info);
						if (listDirs)
						{
							// create a node for directories
							yield return info;
						}
					}
					else if (info is FileInfo)
					{
						yield return info;
					}
				}
			}
		}

		#endregion Methods
	}
}
