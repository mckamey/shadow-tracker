using System;
using System.Collections.Generic;
using System.IO;

namespace Shadow.Agent
{
	internal static class FileIterator
	{
		#region Methods

		public static IEnumerable<FileSystemInfo> GetFiles(string root, bool listEmptyDirs)
		{
			DirectoryInfo dir = new DirectoryInfo(root);
			if (!dir.Exists)
			{
				yield break;
			}

			Queue<DirectoryInfo> queue = new Queue<DirectoryInfo>(100);
			queue.Enqueue(dir);

			while (queue.Count > 0)
			{
				dir = queue.Dequeue();

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
						queue.Enqueue((DirectoryInfo)info);
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
	}
}
