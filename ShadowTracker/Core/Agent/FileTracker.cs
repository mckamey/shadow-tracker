using System;
using System.IO;

using Shadow.Model;

namespace Shadow.Agent
{
	public class FileTracker
	{
		#region Constants

		private const NotifyFilters AllNotifyFilters = NotifyFilters.Attributes|NotifyFilters.CreationTime|NotifyFilters.DirectoryName|NotifyFilters.FileName|NotifyFilters.LastAccess|NotifyFilters.LastWrite|NotifyFilters.Security|NotifyFilters.Size;

		#endregion Constants

		#region Fields

		private readonly FileSystemWatcher watcher = new FileSystemWatcher();
		private Catalog catalog;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public FileTracker()
		{
			this.watcher.IncludeSubdirectories = true;
			this.watcher.NotifyFilter = FileTracker.AllNotifyFilters;

			this.watcher.Created += new FileSystemEventHandler(this.OnFileCreated);
			this.watcher.Changed += new FileSystemEventHandler(this.OnFileChanged);
			this.watcher.Renamed += new RenamedEventHandler(this.OnFileRenamed);
			this.watcher.Error += new ErrorEventHandler(this.OnError);
		}

		#endregion Init

		#region Events

		private void OnFileCreated(object sender, FileSystemEventArgs e)
		{
			Console.WriteLine(e.ChangeType+": "+e.FullPath);
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			Console.WriteLine(e.ChangeType+": "+e.FullPath);
		}

		private void OnFileRenamed(object sender, RenamedEventArgs e)
		{
			Console.WriteLine(e.ChangeType+": "+e.FullPath+" to "+e.OldFullPath);
		}

		private void OnError(object sender, ErrorEventArgs e)
		{
			// TODO: log error
			throw e.GetException();
		}

		#endregion Events

		#region Methods

		public void Start(string watchFolder, string watchFilter, Catalog catalog)
		{
			this.catalog = catalog;
			this.watcher.Path = watchFolder;
			this.watcher.Filter = watchFilter;

			this.watcher.EnableRaisingEvents = true;
		}

		public void Stop()
		{
			this.watcher.EnableRaisingEvents = true;
		}

		#endregion Methods
	}
}
