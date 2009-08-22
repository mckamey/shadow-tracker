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

		private const int UpdateDelay = 100;//milliseconds
		private const NotifyFilters AllNotifyFilters = NotifyFilters.Attributes|NotifyFilters.CreationTime|NotifyFilters.DirectoryName|NotifyFilters.FileName|NotifyFilters.LastAccess|NotifyFilters.LastWrite|NotifyFilters.Security|NotifyFilters.Size;

		#endregion Constants

		#region Fields

		private Catalog catalog;
		private readonly FileSystemWatcher Watcher = new FileSystemWatcher();
		private readonly Dictionary<string, Timer> Timers = new Dictionary<string, Timer>(StringComparer.OrdinalIgnoreCase);

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

		#region Properties

		public Catalog Catalog
		{
			get { return this.catalog; }
		}

		#endregion Properties

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
					return;
				}

				if (!this.Timers.ContainsKey(e.FullPath))
				{
					return;
				}

				this.Timers[e.FullPath].Change(Timeout.Infinite, Timeout.Infinite);
				this.Timers.Remove(e.FullPath);
			}

			this.ApplyChange(e);
		}

		private void OnFileCreated(object sender, FileSystemEventArgs e)
		{
			lock (this.Timers)
			{
				if (this.Timers.ContainsKey(e.FullPath))
				{
					return;
				}

				this.Timers[e.FullPath] = new Timer(this.UpdateTimerCallback, e, UpdateDelay, Timeout.Infinite);
			}
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			lock (this.Timers)
			{
				if (this.Timers.ContainsKey(e.FullPath))
				{
					return;
				}

				this.Timers[e.FullPath] = new Timer(this.UpdateTimerCallback, e, UpdateDelay, Timeout.Infinite);
			}
		}

		private void OnFileDeleted(object sender, FileSystemEventArgs e)
		{
			this.ApplyChange(e);
		}

		private void OnFileRenamed(object sender, RenamedEventArgs e)
		{
			lock (this.Timers)
			{
				if (this.Timers.ContainsKey(e.FullPath))
				{
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

		private CatalogEntry CreateEntry(string path)
		{
			return FileCatalog.CreateNode(this.Watcher.Path, new FileInfo(path));
		}

		private void ApplyChange(FileSystemEventArgs e)
		{
			switch (e.ChangeType)
			{
				case WatcherChangeTypes.Created:
				{
					CatalogEntry entry = this.CreateEntry(e.FullPath);
					this.catalog.ApplyChanges(entry, DeltaAction.Add);
					break;
				}
				case WatcherChangeTypes.Deleted:
				{
					this.catalog.DeleteEntryByPath(e.FullPath);
					break;
				}
				case WatcherChangeTypes.Renamed:
				{
					CatalogEntry entry = this.CreateEntry(e.FullPath);
					this.catalog.ApplyChanges(entry, DeltaAction.Clone);

					entry.Path = ((RenamedEventArgs)e).OldFullPath;
					this.catalog.ApplyChanges(entry, DeltaAction.Delete);
					break;
				}
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
		/// <param name="watchFolder"></param>
		/// <param name="watchFilter"></param>
		/// <param name="catalog"></param>
		public void Start(string watchFolder, string watchFilter, Catalog catalog)
		{
			this.catalog = catalog;
			this.Watcher.Path = watchFolder;
			this.Watcher.Filter = watchFilter;

			this.Watcher.EnableRaisingEvents = true;
		}

		public void Stop()
		{
			this.Watcher.EnableRaisingEvents = true;
		}

		#endregion Methods
	}
}
