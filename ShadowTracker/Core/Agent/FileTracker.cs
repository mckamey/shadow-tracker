using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

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

			FileSystemInfo info = FileUtility.CreateFileSystemInfo(fullPath);

			return !this.fileFilter(info);
		}

		private string NormalizePath(string path)
		{
			return FileUtility.NormalizePath(this.Watcher.Path, path);
		}

		private void ApplyChange(FileSystemEventArgs e)
		{
			//Console.WriteLine(e.ChangeType + ": " + e.FullPath);
			switch (e.ChangeType)
			{
				case WatcherChangeTypes.Deleted:
				{
					IUnitOfWork unitOfWork = UnitOfWorkFactory.Create();
					CatalogRepository catalog = new CatalogRepository(unitOfWork);
					catalog.DeleteEntryByPath(this.NormalizePath(e.FullPath));
					unitOfWork.Save();
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
						bool hasChildren = false;
						foreach (FileSystemInfo info in FileIterator.GetFiles(e2.FullPath, true))
						{
							try
							{
								string infoOldName = FileUtility.ReplaceRoot(e2.FullPath, info.FullName, e2.OldFullPath);
								if (this.IsFiltered(infoOldName))
								{
									this.OnFileCreated(this, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(info.FullName), Path.GetFileName(info.FullName)));
									continue;
								}

								if (this.IsFiltered(info.FullName))
								{
									this.OnFileDeleted(this, new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(infoOldName), Path.GetFileName(infoOldName)));
									continue;
								}

								IUnitOfWork unitOfWork = UnitOfWorkFactory.Create();
								CatalogRepository catalog = new CatalogRepository(unitOfWork);
								catalog.RenameEntry(this.NormalizePath(infoOldName), this.NormalizePath(info.FullName));
								unitOfWork.Save();
								hasChildren = true;
							}
							catch (Exception ex)
							{
								// TODO: log as error
								Console.Error.WriteLine(ex.Message);
							}
						}

						if (!hasChildren)
						{
							IUnitOfWork unitOfWork = UnitOfWorkFactory.Create();
							CatalogRepository catalog = new CatalogRepository(unitOfWork);
							catalog.RenameEntry(this.NormalizePath(e2.OldFullPath), this.NormalizePath(e2.FullPath));
							unitOfWork.Save();
						}
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
					FileSystemInfo info = FileUtility.CreateFileSystemInfo(e.FullPath);
					if (info is DirectoryInfo &&
						FileIterator.GetFiles(e.FullPath, true).Where(this.fileFilter).Any())
					{
						break;
					}

					CatalogEntry entry = FileUtility.CreateEntry(this.Watcher.Path, info);
					IUnitOfWork unitOfWork = UnitOfWorkFactory.Create();
					CatalogRepository catalog = new CatalogRepository(unitOfWork);
					catalog.ApplyChanges(entry);
					unitOfWork.Save();
					break;
				}
			}
		}

		#endregion Events

		#region Methods

		/// <summary>
		/// Sends updates to a catalog
		/// </summary>
		/// 
		/// <param name="watchFolder"></param>
		public void Start(string watchFolder)
		{
			this.Start(watchFolder, null);
		}

		/// <summary>
		/// Sends updates to a catalog
		/// </summary>
		/// <param name="watchFolder"></param>
		/// <param name="fileFilter"></param>
		public void Start(string watchFolder, Func<FileSystemInfo, bool> fileFilter)
		{
			this.fileFilter = fileFilter;

			this.Watcher.Path = watchFolder;

			this.Watcher.EnableRaisingEvents = true;
		}

		public void Stop()
		{
			this.Watcher.EnableRaisingEvents = true;
		}

		#endregion Methods
	}
}
