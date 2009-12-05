using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Shadow.Tasks;

namespace Shadow.Agent
{
	public class TrackerWorkQueue : ITaskStrategy<TrackerTask>
	{
		#region Constants

		private static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(150);
		private const decimal MaxRetryCount = 3m;

		#endregion Constants

		#region ITaskStrategy<TrackerTask> Members

		public TimeSpan Delay
		{
			get { return TrackerWorkQueue.DefaultDelay; }
		}

		public void Execute(TaskEngine<TrackerTask> engine, TrackerTask task)
		{
		}

		public void OnError(TaskEngine<TrackerTask> engine, TrackerTask task, Exception ex)
		{
			Trace.TraceError("Tracker TaskEngine Error: "+ex.Message);

			if (task == null)
			{
				return;
			}

			if (task.RetryCount < TrackerWorkQueue.MaxRetryCount)
			{
				task.RetryCount++;
				task.Priority = this.CalculatePriority(task);
				engine.Add(task);
			}
		}

		public void OnIdle(TaskEngine<TrackerTask> engine)
		{
#if DEBUG
			Trace.TraceInformation("Tracker TaskEngine Idle");
#endif
		}

		#endregion ITaskStrategy<TrackerTask> Members

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

			// same
			return XSamePriorityAsY;
		}

		#endregion IComparer<TrackerTask> Members
	}
}
