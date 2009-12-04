using System;
using System.Threading;

using Shadow.Model;
using System.Collections.Generic;

namespace Shadow.Agent.Tasks
{
	/// <summary>
	/// Trickle update task engine
	/// </summary>
	public class TaskEngine
	{
		#region Fields

		private readonly PriorityQueue<TaskItem> Queue = new PriorityQueue<TaskItem>(
			delegate(TaskItem x, TaskItem y) { return x.Priority > y.Priority; });

		private readonly Func<TaskItem, Exception, bool> OnFailure;
		private readonly Action OnIdle;
		private readonly Timer Timer;
		private readonly long Delay;
		private bool isRunning;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="delay">milliseconds</param>
		/// <param name="onFailure">callback when a work item throws an exception</param>
		/// <param name="onIdle">callback when no work is ready to be performed</param>
		public TaskEngine(
			long delay,
			Action onIdle,
			Func<TaskItem, Exception, bool> onFailure)
		{
			if (delay <= 0)
			{
				throw new ArgumentOutOfRangeException("trickleRate");
			}

			this.Delay = delay;
			this.OnIdle = onIdle;
			this.OnFailure = onFailure;
			this.Timer = new Timer(this.Next);
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets if tasks with duplicate keys should be allowed
		/// </summary>
		public bool AllowDuplicateKeys
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the count of errors
		/// </summary>
		public long ErrorCount
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the count of work cycles
		/// </summary>
		public long CyclesCount
		{
			get;
			private set;
		}

		#endregion Properties

		#region Control Methods

		/// <summary>
		/// Start the trickle of work
		/// </summary>
		public void Start()
		{
			// flag as not running to allow more iterations
			this.isRunning = true;

			// start
			this.Timer.Change(this.Delay, Timeout.Infinite);
		}

		/// <summary>
		/// Stop the trickle of work
		/// </summary>
		public void Stop()
		{
			// flag as not running to prevent next iteration
			this.isRunning = false;

			// stop any queued iterations
			this.Timer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		public void Add(TaskItem task)
		{
			lock (this.Queue.SyncRoot)
			{
				if (this.AllowDuplicateKeys ||
					!this.Queue.Contains(task, TaskItem.EqualityComparer))
				{
					this.Queue.Enqueue(task);
				}
			}
		}

		#endregion Control Methods

		#region Work Methods

		/// <summary>
		/// Perform one iteration of work
		/// </summary>
		/// <param name="state"></param>
		private void Next(object state)
		{
			TaskItem task = null;

			try
			{
				lock (this.Queue.SyncRoot)
				{
					// check if any pending work
					if (this.Queue.Count > 0)
					{
						// perform work
						task = this.Queue.Dequeue();
					}
				}

				if (task != null &&
					task.Perform != null)
				{
					task.Perform();
				}
				else if (this.OnIdle != null)
				{
					// singnal idle
					this.OnIdle();
				}
			}
			catch (Exception ex)
			{
				this.ErrorCount++;
				if (this.OnFailure != null)
				{
					try
					{
						// signal that an error occurred
						// provide a chance to reprioritize
						// and a chance to requeue
						if (this.OnFailure(task, ex))
						{
							if (task != null)
							{
								// re-queue item
								lock (this.Queue.SyncRoot)
								{
									this.Queue.Enqueue(task);
								}
							}
						}
					}
					catch { }
				}
			}

			if (this.isRunning)
			{
				// queue up next iteration
				this.Start();
			}
		}

		#endregion Work Methods
	}
}
