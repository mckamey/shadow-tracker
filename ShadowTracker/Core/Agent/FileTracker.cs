using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Microsoft.Practices.ServiceLocation;
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

		private readonly TaskEngine<TrackerTask> WorkQueue;
		private readonly FileSystemWatcher Watcher = new FileSystemWatcher();

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="ioc"></param>
		/// <param name="catalogID"></param>
		/// <param name="catalogPath"></param>
		/// <param name="fileFilter"></param>
		/// <param name="trickleRate"></param>
		public FileTracker(string catalogPath, TaskEngine<TrackerTask> workQueue)
		{
			this.Watcher.Path = catalogPath;
			this.Watcher.IncludeSubdirectories = true;
			this.Watcher.NotifyFilter = FileTracker.AllNotifyFilters;

			this.Watcher.Created += new FileSystemEventHandler(this.OnFileCreated);
			this.Watcher.Changed += new FileSystemEventHandler(this.OnFileChanged);
			this.Watcher.Renamed += new RenamedEventHandler(this.OnFileRenamed);
			this.Watcher.Deleted += new FileSystemEventHandler(this.OnFileDeleted);
			this.Watcher.Error += new ErrorEventHandler(this.OnError);

			this.WorkQueue = workQueue;
		}

		#endregion Init

		#region Events

		private void OnFileCreated(object sender, FileSystemEventArgs e)
		{
			this.WorkQueue.Add(new TrackerTask(e));
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			this.WorkQueue.Add(new TrackerTask(e));
		}

		private void OnFileDeleted(object sender, FileSystemEventArgs e)
		{
			this.WorkQueue.Add(new TrackerTask(e));
		}

		private void OnFileRenamed(object sender, RenamedEventArgs e)
		{
			this.WorkQueue.Add(new TrackerTask(e));
		}

		private void OnError(object sender, ErrorEventArgs e)
		{
			string message = e.GetException().Message;
			Trace.TraceError("FileTracker Error ("+this.Watcher.Path+"):\r\n"+message);
		}

		#endregion Events

		#region Methods

		public void Start()
		{
			this.WorkQueue.Start();
			this.Watcher.EnableRaisingEvents = true;
		}

		public void Stop()
		{
			this.WorkQueue.Stop();
			this.Watcher.EnableRaisingEvents = true;
		}

		#endregion Methods
	}
}
