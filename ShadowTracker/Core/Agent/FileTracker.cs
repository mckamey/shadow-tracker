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
			this.ApplyChange(e);
		}

		private void OnFileRenamed(object sender, RenamedEventArgs e)
		{
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

		private string NormalizePath(string path)
		{
			return FileCatalog.NormalizePath(this.Watcher.Path, path);
		}

		private CatalogEntry CreateEntry(string path)
		{
			return FileCatalog.CreateNode(this.Watcher.Path, new FileInfo(path));
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
						this.catalog.FastMoveByPath(this.NormalizePath(e2.OldFullPath), this.NormalizePath(e2.FullPath));
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
