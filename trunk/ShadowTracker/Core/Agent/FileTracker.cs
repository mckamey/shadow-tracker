using System;
using System.Diagnostics;
using System.IO;

using Microsoft.Practices.ServiceLocation;
using Shadow.Model;
using Shadow.Tasks;

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

		private const NotifyFilters AllNotifyFilters = NotifyFilters.Attributes|NotifyFilters.CreationTime|NotifyFilters.DirectoryName|NotifyFilters.FileName|NotifyFilters.LastAccess|NotifyFilters.LastWrite|NotifyFilters.Security|NotifyFilters.Size;

		#endregion Constants

		#region Fields

		private readonly IServiceLocator IoC;
		private readonly TaskEngine<TrackerTask> WorkQueue;
		private readonly FileSystemWatcher Watcher = new FileSystemWatcher();
		private readonly long CatalogID;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="ioc"></param>
		/// <param name="catalogPath"></param>
		/// <param name="workQueue"></param>
		public FileTracker(IServiceLocator ioc, long catalogID, string catalogPath, TaskEngine<TrackerTask> workQueue)
		{
			this.IoC = ioc;
			this.CatalogID = catalogID;

			this.Watcher.Path = catalogPath;
			this.Watcher.IncludeSubdirectories = true;
			this.Watcher.NotifyFilter = FileTracker.AllNotifyFilters;
			this.Watcher.Error += this.OnError;

			this.Watcher.Changed += this.OnFileChanged;
			this.Watcher.Created += this.OnFileChanged;
			this.Watcher.Deleted += this.OnFileChanged;
			this.Watcher.Renamed += this.OnFileChanged;

			this.WorkQueue = workQueue;
		}

		#endregion Init

		#region Control Methods

		public void Start()
		{
			this.WorkQueue.Start();
			this.Watcher.EnableRaisingEvents = true;

			// TODO: move this into idle processing area
			this.RemoveExtras();
		}

		public void Stop()
		{
			this.WorkQueue.Stop();
			this.Watcher.EnableRaisingEvents = false;
		}

		private void RemoveExtras()
		{
			CatalogRepository repos = this.IoC.GetInstance<CatalogRepository>();

			foreach (string path in repos.GetExistingPaths(this.CatalogID))
			{
				string fullPath = FileUtility.DenormalizePath(this.Watcher.Path, path);

				if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
				{
					this.WorkQueue.Add(new TrackerTask
					{
						ChangeType= WatcherChangeTypes.Deleted,
						FullPath = fullPath,
						TaskSource = TaskSource.DataSync
					});
				}
			}
		}

		#endregion Control Methods

		#region Event Handlers

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			this.WorkQueue.Add(new TrackerTask(e));
		}

		private void OnError(object sender, ErrorEventArgs e)
		{
			string message = e.GetException().Message;
			Trace.TraceError("FileTracker Error ("+this.Watcher.Path+"):\r\n"+message);
		}

		#endregion Event Handlers
	}
}
