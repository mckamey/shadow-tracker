using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.Practices.ServiceLocation;
using Shadow.Model;
using Shadow.Tasks;

namespace Shadow.Agent
{
	public class TrackerWorkQueue : ITaskStrategy<TrackerTask>
	{
		#region Constants

		private const decimal MaxRetryCount = 3m;

		#endregion Constants

		#region Fields

		private readonly IServiceLocator IoC;
		private readonly TimeSpan TrickleRate;
		private readonly Func<FileSystemInfo, bool> fileFilter;
		private readonly long CatalogID;
		private readonly string CatalogPath;

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
		public TrackerWorkQueue(IServiceLocator ioc, long catalogID, string catalogPath, Func<FileSystemInfo, bool> fileFilter, TimeSpan trickleRate)
		{
			this.CatalogID = catalogID;
			this.CatalogPath = catalogPath;
			this.IoC = ioc;
			this.fileFilter = fileFilter;
			this.TrickleRate = trickleRate;
		}

		#endregion Init

		#region ITaskStrategy<TrackerTask> Members

		TimeSpan ITaskStrategy<TrackerTask>.Delay
		{
			get { return this.TrickleRate; }
		}

		void ITaskStrategy<TrackerTask>.Execute(TaskEngine<TrackerTask> engine, TrackerTask task)
		{
			if (engine == null)
			{
				throw new ArgumentNullException("engine");
			}

			CatalogRepository repos = this.IoC.GetInstance<CatalogRepository>();

			switch (task.ChangeType)
			{
				case WatcherChangeTypes.Deleted:
				{
					repos.DeleteEntryByPath(this.CatalogID, this.NormalizePath(task.FullPath));
					break;
				}
				case WatcherChangeTypes.Renamed:
				{
					if (String.IsNullOrEmpty(task.OldFullPath))
					{
						throw new ArgumentException("OldFullPath", "Rename operation was missing OldFullPath");
					}

					string oldPath = this.NormalizePath(task.OldFullPath);
					string newPath = this.NormalizePath(task.FullPath);
					if (this.IsFiltered(oldPath))
					{
						task.ChangeType = WatcherChangeTypes.Created;
						task.OldFullPath = null;
						engine.Add(task);
						return;
					}

					if (this.IsFiltered(newPath))
					{
						task.ChangeType = WatcherChangeTypes.Deleted;
						task.FullPath = task.OldFullPath;
						task.OldFullPath = null;
						engine.Add(task);
						return;
					}

					bool found = repos.MoveEntry(this.CatalogID, oldPath, newPath);
					if (!found)
					{
						task.ChangeType = WatcherChangeTypes.Created;
						task.OldFullPath = null;
						engine.Add(task);
					}
					break;
				}
				case WatcherChangeTypes.Created:
				case WatcherChangeTypes.Changed:
				default:
				{
					FileSystemInfo info = FileUtility.CreateFileSystemInfo(task.FullPath);
					CatalogEntry entry = FileUtility.CreateEntry(this.CatalogID, this.CatalogPath, info);
					if (repos.AddOrUpdate(entry))
					{
						repos.Save();
					}

					// add any children
					if (info is DirectoryInfo &&
						task.ChangeType == WatcherChangeTypes.Created)
					{
						Trace.TraceInformation("Add children \"{0}/*\"", entry.FullPath);

						// TODO: queue these up instead of immediately executing
						foreach (FileSystemInfo child in ((DirectoryInfo)info).GetFileSystemInfos())
						{
							engine.Add(new TrackerTask
							{
								ChangeType = task.ChangeType,
								FullPath = child.FullName,
								TaskSource = task.TaskSource
							});
						}
					}
					break;
				}
			}

			repos.Save();
		}

		bool ITaskStrategy<TrackerTask>.OnAddTask(TaskEngine<TrackerTask> engine, TrackerTask task)
		{
			if (engine == null)
			{
				throw new ArgumentNullException("engine");
			}
			if (task == null)
			{
				return false;
			}

			if (task.ChangeType == WatcherChangeTypes.Renamed)
			{
				if (this.IsFiltered(task.FullPath))
				{
					if (!this.IsFiltered(task.OldFullPath))
					{
						task.ChangeType = WatcherChangeTypes.Deleted;
						task.FullPath = task.OldFullPath;
						task.OldFullPath = null;
					}
					else
					{
						return false;
					}
				}
				else if (this.IsFiltered(task.OldFullPath))
				{
					task.ChangeType = WatcherChangeTypes.Created;
					task.OldFullPath = null;
				}
				else
				{
					return false;
				}
			}
			else if (this.IsFiltered(task.FullPath))
			{
				return false;
			}

			IEnumerable<TrackerTask> tasks = engine.Find(t => StringComparer.OrdinalIgnoreCase.Equals(t.FullPath, task.FullPath));
			if (tasks.Any())
			{
				// TODO: determine if should remove, filter, merge
				return false;
			}

			task.Priority = this.CalculatePriority(task);

			return true;
		}

		void ITaskStrategy<TrackerTask>.OnError(TaskEngine<TrackerTask> engine, TrackerTask task, Exception ex)
		{
			if (ex == null)
			{
				throw new ArgumentNullException("ex");
			}

			if (task == null)
			{
				Trace.TraceError("Engine Error: "+ex.Message);
				return;
			}

			Trace.TraceError("Engine Error: "+ex.Message+" "+task.ToString());

			if (engine == null)
			{
				throw new ArgumentNullException("engine");
			}

			if (ex is IOException)
			{
				//"The process cannot access the file 'XYZ' because it is being used by another process."
				// http://stackoverflow.com/questions/1314958

				// TODO: decide how to better detect this issue
				// works best when wait for 1000ms or more
				task.RetryCount++;
				engine.Add(task);
			}

			if (task.RetryCount < TrackerWorkQueue.MaxRetryCount)
			{
				task.RetryCount++;
				engine.Add(task);
			}
		}

		void ITaskStrategy<TrackerTask>.OnIdle(TaskEngine<TrackerTask> engine)
		{
			if (engine == null)
			{
				throw new ArgumentNullException("engine");
			}

#if DEBUG
			Trace.TraceInformation("Engine: idle");
#endif
		}

		#endregion ITaskStrategy<TrackerTask> Members

		#region Utility Methods

		[DebuggerStepThrough]
		private string NormalizePath(string path)
		{
			return FileUtility.NormalizePath(this.CatalogPath, path);
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

		#endregion Utility Methods

		#region Priority Calculations

		/// <summary>
		/// Calculates a fuzzy weighting of tasks which may then be ordered.
		/// </summary>
		/// <param name="task"></param>
		/// <returns></returns>
		private decimal CalculatePriority(TrackerTask task)
		{
			const decimal TaskSourceWeight = 0.35m;
			const decimal ChangeTypeWeight = 0.25m;
			const decimal RetryCountWeight = 0.40m;

			decimal priority = 0.0m;

			priority += this.PrioritizeTaskSource(task.TaskSource) * TaskSourceWeight;
			priority += this.PrioritizeChangeType(task.ChangeType) * ChangeTypeWeight;
			priority += this.PrioritizeRetryCount(task.RetryCount) * RetryCountWeight;

			return priority;
		}

		/// <summary>
		/// Calculates prioritization for the task-source component
		/// </summary>
		/// <param name="source"></param>
		/// <returns>a normalized [0.0-1.0] range</returns>
		private decimal PrioritizeTaskSource(TaskSource source)
		{
			// arbitrary values but allows custom ordering
			switch (source)
			{
				case TaskSource.FileTracker:
				{
					return 1.00m;
				}
				case TaskSource.ExtrasScan:
				{
					return 0.67m;
				}
				case TaskSource.ChangesScan:
				{
					return 0.33m;
				}
				default:
				{
					return 0.00m;
				}
			}
		}

		/// <summary>
		/// Calculates prioritization for the retry-count component
		/// </summary>
		/// <param name="retryCount"></param>
		/// <returns>a normalized [0.0-1.0] range</returns>
		private decimal PrioritizeRetryCount(decimal retryCount)
		{
			// bound count between zero and max
			if (retryCount < Decimal.Zero)
			{
				retryCount = Decimal.Zero;
			}
			else if (retryCount > TrackerWorkQueue.MaxRetryCount)
			{
				retryCount = TrackerWorkQueue.MaxRetryCount;
			}

			// normalize count inversely between zero and max
			return (TrackerWorkQueue.MaxRetryCount-retryCount) / TrackerWorkQueue.MaxRetryCount;
		}

		/// <summary>
		/// Calculates prioritization for the change-type component
		/// </summary>
		/// <param name="changeType"></param>
		/// <returns>a normalized [0.0-1.0] range</returns>
		private decimal PrioritizeChangeType(WatcherChangeTypes changeType)
		{
			// arbitrary values but allows custom ordering
			switch (changeType)
			{
				case WatcherChangeTypes.Deleted:
				{
					return 1.00m;
				}
				case WatcherChangeTypes.Renamed:
				{
					return 0.75m;
				}
				case WatcherChangeTypes.Created:
				{
					return 0.50m;
				}
				case WatcherChangeTypes.Changed:
				{
					return 0.25m;
				}
				default:
				{
					return 0.00m;
				}
			}
		}

		#endregion Priority Calculations

		#region IComparer<TrackerTask> Members

		/// <summary>
		/// Returns the relative priority of the two tasks
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		int IComparer<TrackerTask>.Compare(TrackerTask x, TrackerTask y)
		{
			const int XLowerPriorityThanY = -1;
			const int XSamePriorityAsY = 0;
			const int XHigherPriorityThanY = 1;

			// null values are less
			if (x == null)
			{
				if (y == null)
				{
					return XSamePriorityAsY;
				}
				return XLowerPriorityThanY;
			}
			if (y == null)
			{
				return XHigherPriorityThanY;
			}

			// compare Priority levels
			if (x.Priority < y.Priority)
			{
				return XLowerPriorityThanY;
			}
			if (x.Priority > y.Priority)
			{
				return XHigherPriorityThanY;
			}
			return XSamePriorityAsY;
		}

		#endregion IComparer<TrackerTask> Members
	}
}
