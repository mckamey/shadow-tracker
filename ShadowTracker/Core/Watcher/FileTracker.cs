using System;
using System.IO;

namespace Shadow.Watcher
{
	public class FileTracker
	{
		#region Fields

		private readonly FileSystemWatcher watcher = new FileSystemWatcher();

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public FileTracker()
		{
			this.watcher.IncludeSubdirectories = true;
			this.watcher.NotifyFilter = (NotifyFilters)383;//NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.Attributes;

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
			Console.WriteLine(e.GetException().ToString());
		}

		#endregion Events

		#region Methods

		public void Start(string watchFolder, string watchFilter)
		{
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
