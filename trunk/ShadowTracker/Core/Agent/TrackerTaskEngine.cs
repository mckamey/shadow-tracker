using System;
using System.Diagnostics;

using Shadow.Tasks;

namespace Shadow.Agent
{
	public class TrackerTaskEngine : ITaskEngineDefn<TrackerTask>
	{
		#region ITaskEngineDefn<TrackerTask> Members

		public long Delay
		{
			get { return 150; }
		}

		public bool IsHigherPriority(TrackerTask a, TrackerTask b)
		{
			return a.Priority > a.Priority;
		}

		public void Perform(TrackerTask task)
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
		}

		#endregion ITaskEngineDefn<TrackerTask> Members
	}
}
