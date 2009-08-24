using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Shadow.Model;

namespace Shadow.Agent
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// FileSystemWatcher: http://stackoverflow.com/questions/449993/vb-net-filesystemwatcher-multiple-change-events
	/// Timers: http://msdn.microsoft.com/en-us/magazine/cc164015.aspx
	/// </remarks>
	public class FileTracker
	{
		#region Constants

		private const int UpdateDelay = 1000;//milliseconds
		private const NotifyFilters AllNotifyFilters = NotifyFilters.Attributes|NotifyFilters.CreationTime|NotifyFilters.DirectoryName|NotifyFilters.FileName|NotifyFilters.LastAccess|NotifyFilters.LastWrite|NotifyFilters.Security|NotifyFilters.Size;

		#endregion Constants

		#region Fields

		private CatalogRepository catalog;
		private readonly FileSystemWatcher Watcher = new FileSystemWatcher();
		private readonly Dictionary<string, Timer> Timers = new Dictionary<string, Timer>(StringComparer.OrdinalIgnoreCase);
		private Func<FileSystemInfo, bool> fileFilter;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public FileTracker()
		{
			this.Watcher.IncludeSubdirectories = true;
			this.Watcher.NotifyFilter = FileTracker.AllNotifyFilters;

			this.Watcher.Created += new FileSystemEventHandler(this.OnFileCreated);
			this.Watcher.Changed += new FileSystemEventHandler(this.OnFileChanged);
			this.Watcher.Renamed += new RenamedEventHandler(this.OnFileRenamed);
			this.Watcher.Deleted += new FileSystemEventHandler(this.OnFileDeleted);
			this.Watcher.Error += new ErrorEventHandler(this.OnError);
		}

		#endregion Init

		#region Events

		private void UpdateTimerCallback(object state)
		{
			FileSystemEventArgs e;
			lock (this.Timers)
			{
				e = state as FileSystemEventArgs;
				if (e == null)
				{
					// TODO: log as error
					Console.Error.WriteLine("UpdateTimerCallback state was not FileSystemEventArgs");
					return;
				}

				if (!this.Timers.ContainsKey(e.FullPath))
				{
					Console.Error.WriteLine(e.ChangeType+" Timer empty: "+e.FullPath);
					return;
				}

				Timer timer = this.Timers[e.FullPath];
				try
				{
					timer.Change(Timeout.Infinite, Timeout.Infinite);
					this.ApplyChange(e);
					this.Timers.Remove(e.FullPath);
				}
				catch (IOException /*ex*/)
				{
					//"The process cannot access the file 'XYZ' because it is being used by another process."
					// http://stackoverflow.com/questions/1314958

					//Console.Error.WriteLine(ex.Message);

					// queue up for another check
					//Console.WriteLine(e.ChangeType+" Timer reset: "+e.FullPath);
					timer.Change(UpdateDelay, Timeout.Infinite);
					return;
				}
			}
		}

		private void OnFileCreated(object sender, FileSystemEventArgs e)
		{
			if (this.IsFiltered(e.FullPath))
			{
				return;
			}

			lock (this.Timers)
			{
				if (this.Timers.ContainsKey(e.FullPath))
				{
					//Console.WriteLine(e.ChangeType+" Timer exists: "+e.FullPath);
					return;
				}

				this.Timers[e.FullPath] = new Timer(this.UpdateTimerCallback, e, UpdateDelay, Timeout.Infinite);
			}
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			if (this.IsFiltered(e.FullPath))
			{
				return;
			}

			lock (this.Timers)
			{
				if (this.Timers.ContainsKey(e.FullPath))
				{
					//Console.WriteLine(e.ChangeType+" Timer exists: "+e.FullPath);
					return;
				}

				this.Timers[e.FullPath] = new Timer(this.UpdateTimerCallback, e, UpdateDelay, Timeout.Infinite);
			}
		}

		private void OnFileDeleted(object sender, FileSystemEventArgs e)
		{
			if (this.IsFiltered(e.FullPath))
			{
				return;
			}

			this.ApplyChange(e);
		}

		private void OnFileRenamed(object sender, RenamedEventArgs e)
		{
			if (this.IsFiltered(e.OldFullPath))
			{
				this.OnFileCreated(sender, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(e.FullPath), Path.GetFileName(e.FullPath)));
				return;
			}

			if (this.IsFiltered(e.FullPath))
			{
				this.OnFileDeleted(sender, new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(e.OldFullPath), Path.GetFileName(e.OldFullPath)));
				return;
			}

			lock (this.Timers)
			{
				if (this.Timers.ContainsKey(e.FullPath))
				{
					//Console.WriteLine(e.ChangeType+" Timer exists: "+e.FullPath);
					return;
				}

				this.Timers[e.FullPath] = new Timer(this.UpdateTimerCallback, e, UpdateDelay, Timeout.Infinite);
			}
		}

		private void OnError(object sender, ErrorEventArgs e)
		{
			// TODO: log error
			throw e.GetException();
		}

		private bool IsFiltered(string fullPath)
		{
			if (this.fileFilter == null)
			{
				return false;
			}

			FileSystemInfo info;
			if (Directory.Exists(fullPath))
			{
				// is a directory
				info = new DirectoryInfo(fullPath);
			}
			else
			{
				info = new FileInfo(fullPath);
			}

			return !this.fileFilter(info);
		}

		private string NormalizePath(string path)
		{
			return FileUtility.NormalizePath(this.Watcher.Path, path);
		}

		private CatalogEntry CreateEntry(string path)
		{
			FileSystemInfo info;
			if (Directory.Exists(path))
			{
				// is a directory
				info = new DirectoryInfo(path);
			}
			else
			{
				info = new FileInfo(path);
			}

			return FileUtility.CreateEntry(this.Watcher.Path, info);
		}

		private void ApplyChange(FileSystemEventArgs e)
		{
			//Console.WriteLine(e.ChangeType + ": " + e.FullPath);
			switch (e.ChangeType)
			{
				case WatcherChangeTypes.Deleted:
				{
					this.catalog.DeleteEntryByPath(this.NormalizePath(e.FullPath));
					break;
				}
				case WatcherChangeTypes.Renamed:
				{
					RenamedEventArgs e2 = e as RenamedEventArgs;
					if (e2 == null)
					{
						// TODO: log as error
						Console.Error.WriteLine("UpdateTimerCallback state was not FileSystemEventArgs");
						return;
					}

					try
					{
						this.catalog.RenameEntry(this.NormalizePath(e2.OldFullPath), this.NormalizePath(e2.FullPath));
					}
					catch (ArgumentException ex)
					{
						// TODO: log as error
						Console.Error.WriteLine(ex.Message);

						// recover by simply adding
						goto case WatcherChangeTypes.Created;
					}
					break;
				}
				case WatcherChangeTypes.Created:
				case WatcherChangeTypes.Changed:
				default:
				{
					CatalogEntry entry = this.CreateEntry(e.FullPath);
					this.catalog.ApplyChanges(entry);
					break;
				}
			}
		}

		#endregion Events

		#region Methods

		/// <summary>
		/// Sends updates to a catalog
		/// </summary>
		/// <param name="catalog"></param>
		/// <param name="watchFolder"></param>
		/// <param name="pathFilter"></param>
		public void Start(CatalogRepository catalog, string watchFolder, string pathFilter)
		{
			this.Start(catalog, watchFolder, pathFilter, null);
		}

		/// <summary>
		/// Sends updates to a catalog
		/// </summary>
		/// <param name="catalog"></param>
		/// <param name="watchFolder"></param>
		/// <param name="watchFilter"></param>
		public void Start(CatalogRepository catalog, string watchFolder, string pathFilter, Func<FileSystemInfo, bool> fileFilter)
		{
			this.catalog = catalog;
			this.fileFilter = fileFilter;

			this.Watcher.Path = watchFolder;
			this.Watcher.Filter = pathFilter;

			this.Watcher.EnableRaisingEvents = true;
		}

		public void Stop()
		{
			this.Watcher.EnableRaisingEvents = true;
		}

		#endregion Methods
	}
}
