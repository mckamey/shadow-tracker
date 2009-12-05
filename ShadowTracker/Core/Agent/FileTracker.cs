using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.Practices.ServiceLocation;
using Shadow.Model;

namespace Shadow.Agent
{
	/// <summary>
	/// Monitors the file system and updates the CatalogRepository accordingly.
	/// </summary>
	/// <remarks>
	/// FileSystemWatcher: http://stackoverflow.com/questions/449993
	/// Timers: http://msdn.microsoft.com/en-us/magazine/cc164015.aspx
	/// </remarks>
	public class FileTracker
	{
		#region Constants

		private const int UpdateDelay = 1000;//milliseconds
		private const NotifyFilters AllNotifyFilters = NotifyFilters.Attributes|NotifyFilters.CreationTime|NotifyFilters.DirectoryName|NotifyFilters.FileName|NotifyFilters.LastAccess|NotifyFilters.LastWrite|NotifyFilters.Security|NotifyFilters.Size;

		#endregion Constants

		#region Fields

		private readonly FileSystemWatcher Watcher = new FileSystemWatcher();
		private readonly Dictionary<string, Timer> Timers = new Dictionary<string, Timer>(StringComparer.OrdinalIgnoreCase);
		private readonly IServiceLocator IoC;
		private Func<FileSystemInfo, bool> fileFilter;
		private Catalog catalog;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="ioc"></param>
		public FileTracker(IServiceLocator ioc)
		{
			this.IoC = ioc;

			this.Watcher.IncludeSubdirectories = true;
			this.Watcher.NotifyFilter = FileTracker.AllNotifyFilters;

			this.Watcher.Created += new FileSystemEventHandler(this.OnFileCreated);
			this.Watcher.Changed += new FileSystemEventHandler(this.OnFileChanged);
			this.Watcher.Renamed += new RenamedEventHandler(this.OnFileRenamed);
			this.Watcher.Deleted += new FileSystemEventHandler(this.OnFileDeleted);
			this.Watcher.Error += new ErrorEventHandler(this.OnError);
		}

		#endregion Init

		#region Properties

		public Catalog Catalog
		{
			get { return this.catalog; }
		}

		#endregion Properties

		#region Events

		public event ErrorEventHandler TrackerError;

		private void OnTrackerError(Exception ex)
		{
			if (this.TrackerError != null)
			{
				this.TrackerError(this, new ErrorEventArgs(ex));
			}
			else
			{
				Console.Error.WriteLine("Tracker Error:");
				Console.Error.WriteLine(ex);
			}
		}

		private void UpdateTimerCallback(object state)
		{
			FileSystemEventArgs e;
			lock (this.Timers)
			{
				e = state as FileSystemEventArgs;
				if (e == null)
				{
					this.OnTrackerError(new ArgumentNullException("state", "UpdateTimerCallback state was not FileSystemEventArgs"));
					return;
				}

				if (!this.Timers.ContainsKey(e.FullPath))
				{
					this.OnTrackerError(new InvalidOperationException(e.ChangeType+" Timer empty: "+e.FullPath));
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

					//this.OnTrackerError(ex);

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

			lock (this.Timers)
			{
				if (this.Timers.ContainsKey(e.FullPath))
				{
					//Console.WriteLine(e.ChangeType+" Timer exists: "+e.FullPath);
					return;
				}

				this.Timers[e.FullPath] = new Timer(this.UpdateTimerCallback, e, 2*UpdateDelay, Timeout.Infinite);
			}
		}

		private void OnFileRenamed(object sender, RenamedEventArgs e)
		{
			if (this.IsFiltered(e.OldFullPath))
			{
				// simulate create since old path was filtered
				this.OnFileCreated(sender, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(e.FullPath), Path.GetFileName(e.FullPath)));
				return;
			}

			if (this.IsFiltered(e.FullPath))
			{
				// simulate delete since new path is filtered
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
			if (this.TrackerError != null)
			{
				this.TrackerError(this, e);
			}
		}

		private bool IsFiltered(string fullPath)
		{
			if (this.fileFilter == null)
			{
				return false;
			}

			FileSystemInfo info = FileUtility.CreateFileSystemInfo(fullPath);

			bool filtered = !this.fileFilter(info);
			if (filtered)
			{
				Trace.TraceInformation("Filtered: \"{0}\"", fullPath);
			}
			return filtered;
		}

		[System.Diagnostics.DebuggerStepThrough]
		private string NormalizePath(string path)
		{
			return FileUtility.NormalizePath(this.Watcher.Path, path);
		}

		private void ApplyChange(FileSystemEventArgs e)
		{
			try
			{
				CatalogRepository repos = this.IoC.GetInstance<CatalogRepository>();

				switch (e.ChangeType)
				{
					case WatcherChangeTypes.Deleted:
					{
						repos.DeleteEntryByPath(this.catalog.ID, this.NormalizePath(e.FullPath));
						break;
					}
					case WatcherChangeTypes.Renamed:
					{
						RenamedEventArgs e2 = e as RenamedEventArgs;
						if (e2 == null)
						{
							this.OnTrackerError(new ArgumentNullException("state", "UpdateTimerCallback state was not FileSystemEventArgs"));
							return;
						}

						string oldPath = this.NormalizePath(e2.OldFullPath);
						string newPath = this.NormalizePath(e2.FullPath);
						if (this.IsFiltered(oldPath))
						{
							this.OnFileCreated(this, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(newPath), Path.GetFileName(newPath)));
							return;
						}

						if (this.IsFiltered(newPath))
						{
							this.OnFileDeleted(this, new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(oldPath), Path.GetFileName(oldPath)));
							return;
						}

						bool found = repos.MoveEntry(this.catalog.ID, oldPath, newPath);
						if (!found)
						{
							this.OnFileCreated(this, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(newPath), Path.GetFileName(newPath)));
						}
						break;
					}
					case WatcherChangeTypes.Created:
					case WatcherChangeTypes.Changed:
					default:
					{
						FileSystemInfo info = FileUtility.CreateFileSystemInfo(e.FullPath);
						CatalogEntry entry = FileUtility.CreateEntry(this.catalog.ID, this.catalog.Path, info);
						if (repos.AddOrUpdate(entry))
						{
							repos.Save();
						}

						// add any children
						if (info is DirectoryInfo &&
							e.ChangeType == WatcherChangeTypes.Created)
						{
							// TODO: determine best way to for WatcherChangeTypes.Changed

							Trace.TraceInformation("Add children \"{0}/*\"", entry.FullPath);

							foreach (FileSystemInfo child in FileIterator.GetFiles(info.FullName).Where(this.fileFilter))
							{
								entry = FileUtility.CreateEntry(this.catalog.ID, this.catalog.Path, child);
								if (repos.AddOrUpdate(entry))
								{
									repos.Save();
								}
							}
						}
						break;
					}
				}

				repos.Save();
			}
			catch (Exception ex)
			{
				this.OnTrackerError(ex);
			}
		}

		#endregion Events

		#region Methods

		/// <summary>
		/// Sends updates to a catalog
		/// </summary>
		/// <param name="watchFolder"></param>
		[System.Diagnostics.DebuggerStepThrough]
		public void Start(Catalog catalog)
		{
			this.Start(catalog, n => true);
		}

		/// <summary>
		/// Sends updates to a catalog
		/// </summary>
		/// <param name="watchFolder"></param>
		/// <param name="fileFilter"></param>
		public void Start(Catalog catalog, Func<FileSystemInfo, bool> fileFilter)
		{
			this.catalog = catalog;
			this.fileFilter = fileFilter;

			this.Watcher.Path = this.catalog.Path;
			this.Watcher.EnableRaisingEvents = true;
		}

		public void Stop()
		{
			this.Watcher.EnableRaisingEvents = true;
		}

		#endregion Methods
	}
}
