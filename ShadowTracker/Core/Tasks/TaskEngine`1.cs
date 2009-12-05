using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Shadow.Tasks
{
	/// <summary>
	/// Trickle update task engine
	/// </summary>
	public class TaskEngine<T>
	{
		#region EngineState

		private enum EngineState
		{
			Stopped,
			Ready,
			Running
		}

		#endregion EngineState

		#region Constants

		/// <summary>
		/// A constant used to specify an infinite waiting period
		/// </summary>
		public static readonly TimeSpan Infinite = TimeSpan.FromMilliseconds(Timeout.Infinite);

		#endregion Constants

		#region Fields

		private readonly PriorityQueue<T> Queue;
		private readonly ITaskStrategy<T> Strategy;
		private readonly Timer Timer;
		private EngineState state = EngineState.Stopped;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public TaskEngine(ITaskStrategy<T> strategy)
		{
			if (strategy == null)
			{
				throw new ArgumentNullException("strategy");
			}

			this.Strategy = strategy;
			this.Queue = new PriorityQueue<T>(this.Strategy);
			this.Timer = new Timer(this.Next);
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets the count of total errors
		/// </summary>
		public long ErrorCount
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the count of total work cycles
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
			lock (this.Queue.SyncRoot)
			{
				if (this.Queue.Count > 0)
				{
					// set state to allow more iterations
					this.state = EngineState.Running;

					// start
					this.Timer.Change(this.Strategy.Delay, TaskEngine<T>.Infinite);
				}
				else
				{
					// set state to start when tasks are added
					this.state = EngineState.Ready;
				}
			}
		}

		/// <summary>
		/// Stop the trickle of work
		/// </summary>
		public void Stop()
		{
			// set state to prevent next iteration
			this.state = EngineState.Stopped;

			// stop any queued iterations
			this.Timer.Change(Timeout.Infinite, Timeout.Infinite);
		}

		/// <summary>
		/// Adds a task to the queue
		/// </summary>
		/// <param name="task"></param>
		public void Add(T task)
		{
			lock (this.Queue.SyncRoot)
			{
				// give chanced to modify task, filter duplicates, etc.
				if (this.Strategy.OnAddTask(this, task))
				{
					this.Queue.Enqueue(task);

					// is ready to begin so start iterations
					if (this.state == EngineState.Ready)
					{
						this.Start();
					}
				}
			}
		}

		/// <summary>
		/// Adds a task to the queue
		/// </summary>
		/// <param name="task"></param>
		public bool Contains(Func<T, bool> predicate)
		{
			lock (this.Queue.SyncRoot)
			{
				return this.Queue.Find(predicate).Any();
			}
		}

		/// <summary>
		/// Removes and returns any tasks in the queue which match the criteria
		/// </summary>
		/// <param name="predicate">removal criteria</param>
		/// <returns>the sequence of removed elements</returns>
		public IEnumerable<T> Remove(Func<T, bool> predicate)
		{
			lock (this.Queue.SyncRoot)
			{
				// convert to list so queue isn't locked during use of enumeration
				return this.Queue.Remove(predicate).ToList();
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
			T task = default(T);

			try
			{
				bool hasTasks;
				lock (this.Queue.SyncRoot)
				{
					// check if any pending work
					hasTasks = this.Queue.Count > 0;
					if (hasTasks)
					{
						// perform work
						task = this.Queue.Dequeue();
					}
				}

				if (hasTasks)
				{
					this.Strategy.Execute(this, task);
				}
				else
				{
					// signal idle
					this.Strategy.OnIdle(this);
				}
			}
			catch (Exception ex)
			{
				try
				{
					// increment and signal error
					this.ErrorCount++;
					this.Strategy.OnError(this, task, ex);
				}
				catch { }
			}

			if (this.state != EngineState.Stopped)
			{
				// queue up next iteration
				this.Start();
			}
		}

		#endregion Work Methods
	}
}
