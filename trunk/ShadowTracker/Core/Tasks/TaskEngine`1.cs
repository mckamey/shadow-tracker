using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
		private const int TrimThreshold = 1000;

		#endregion Constants

		#region Fields

		private readonly Stopwatch Watch = Stopwatch.StartNew();
		private readonly PriorityQueue<T> Queue;
		private readonly ITaskStrategy<T> Strategy;
		private readonly IEnumerable<Timer> Timers;
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
			if (strategy.ThreadCount <= 0)
			{
				throw new ArgumentException("ThreadCount");
			}

			this.Strategy = strategy;
			this.Queue = new PriorityQueue<T>(strategy);

			Timer[] timers = new Timer[strategy.ThreadCount];
			for (int i=0; i< timers.Length; i++)
			{
				timers[i] = new Timer(this.Next, i, TaskEngine<T>.Infinite, TaskEngine<T>.Infinite);
			}
			this.Timers = timers;
		}

		#endregion Init

		#region Properties

		public int Count
		{
			get { return this.Queue.Count; }
		}

		/// <summary>
		/// Gets the count of total work cycles
		/// </summary>
		public long CycleCount
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the count of total errors
		/// </summary>
		public long ErrorCount
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the elapsed life of the engine
		/// </summary>
		public TimeSpan Elapsed
		{
			get { return this.Watch.Elapsed; }
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
					foreach (Timer timer in this.Timers)
					{
						timer.Change(this.Strategy.Delay, TaskEngine<T>.Infinite);
					}
				}
				else if (this.state != EngineState.Ready)
				{
					// set state to start when tasks are added
					this.state = EngineState.Ready;

					// stop any queued iterations
					foreach (Timer timer in this.Timers)
					{
						timer.Change(Timeout.Infinite, Timeout.Infinite);
					}

					try
					{
						if (this.Queue.Capacity > TaskEngine<T>.TrimThreshold)
						{
							// reduce overhead
							this.Queue.TrimExcess();
						}

						// signal idle
						this.Strategy.OnIdle(this);
					}
					catch (Exception ex)
					{
						try
						{
							this.Strategy.OnError(this, -1, default(T), ex);
						}
						catch { }
					}
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
			foreach (Timer timer in this.Timers)
			{
				timer.Change(Timeout.Infinite, Timeout.Infinite);
			}
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
		/// Finds any tasks in the queue which match the criteria
		/// </summary>
		/// <param name="predicate">matching criteria</param>
		/// <returns>the sequence of matched elements</returns>
		public IEnumerable<T> Find(Func<T, bool> predicate)
		{
			lock (this.Queue.SyncRoot)
			{
				// convert to list so queue isn't locked during use of enumeration
				return this.Queue.Find(predicate).ToList();
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

			int timerID;
			try
			{
				timerID = (int)state;
			}
			catch
			{
				timerID = -2;
			}

			try
			{
				bool hasTasks;
				lock (this.Queue.SyncRoot)
				{
					// check if any pending work
					hasTasks = this.Queue.Count > 0;
					if (hasTasks)
					{
						// get highest priority task
						task = this.Queue.Dequeue();
					}
				}

				if (hasTasks)
				{
					// increment and perform work
					this.CycleCount++;
					this.Strategy.Execute(this, timerID, task);
				}
			}
			catch (Exception ex)
			{
				try
				{
					// increment and signal error
					this.ErrorCount++;
					this.Strategy.OnError(this, timerID, task, ex);
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

		#region Object Overrides

		/// <summary>
		/// Represents the TaskItem as a string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder("{ ");

			builder.Append("Count = ");
			builder.Append(this.Count);

			builder.Append(", Cycles = ");
			builder.Append(this.CycleCount);

			builder.Append(", Errors = ");
			builder.Append(this.ErrorCount);

			builder.Append(", Elapsed = ");
			builder.Append(this.Elapsed);

			builder.Append(" }");

			return builder.ToString();
		}

		#endregion Object Overrides
	}
}
