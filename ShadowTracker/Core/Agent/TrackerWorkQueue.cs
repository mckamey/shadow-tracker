using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Shadow.Tasks;

namespace Shadow.Agent
{
	public class TrackerWorkQueue :
		ITaskStrategy<TrackerTask>,
		IComparer<TrackerTask>
	{
		#region Constants

		private static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(150);

		#endregion Constants

		#region ITaskEngineDefn<TrackerTask> Members

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

			if (task.RetryCount < 3)
			{
				task.RetryCount++;
				task.Priority--;
				engine.Add(task);
			}
		}

		public void OnIdle(TaskEngine<TrackerTask> engine)
		{
#if DEBUG
			Trace.TraceInformation("Tracker TaskEngine Idle");
#endif
		}

		#endregion ITaskEngineDefn<TrackerTask> Members

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

			// Priority directly affects
			if (x.Priority < y.Priority)
			{
				return XLowerPriorityThanY;
			}
			if (x.Priority > y.Priority)
			{
				return XHigherPriorityThanY;
			}

			// the higher RetryCount, the lower priority
			if (x.RetryCount < y.RetryCount)
			{
				return XHigherPriorityThanY;
			}
			if (x.RetryCount > y.RetryCount)
			{
				return XLowerPriorityThanY;
			}

			// sort by change type
			int xPri = GetChangeTypePriority(x.ChangeType);
			int yPri = GetChangeTypePriority(y.ChangeType);
			if (xPri < yPri)
			{
				return XLowerPriorityThanY;
			}
			if (xPri > yPri)
			{
				return XHigherPriorityThanY;
			}

			// same
			return XSamePriorityAsY;
		}

		private static int GetChangeTypePriority(WatcherChangeTypes changeType)
		{
			switch (changeType)
			{
				case WatcherChangeTypes.Deleted:
				{
					return 4;
				}
				case WatcherChangeTypes.Renamed:
				{
					return 3;
				}
				case WatcherChangeTypes.Created:
				{
					return 2;
				}
				case WatcherChangeTypes.Changed:
				{
					return 1;
				}
				default:
				{
					return 0;
				}
			}
		}

		#endregion IComparer<TrackerTask> Members
	}
}
