using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Shadow.Model;

namespace Shadow.Tasks
{
	/// <summary>
	/// Trickle update task engine
	/// </summary>
	public class TaskEngine<T>
	{
		#region Fields

		private readonly PriorityQueue<T> Queue;
		private readonly ITaskEngineDefn<T> Defn;
		private readonly Timer Timer;
		private bool isRunning;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public TaskEngine(ITaskEngineDefn<T> defn)
		{
			if (defn == null)
			{
				throw new ArgumentNullException("defn");
			}

			this.Defn = defn;
			this.Queue = new PriorityQueue<T>(this.Defn.IsHigherPriority);
			this.Timer = new Timer(this.Next);
		}

		#endregion Init

		#region Properties

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
			// flag as running to allow more iterations
			this.isRunning = true;

			long delay = Math.Abs(this.Defn.Delay);

			// start
			this.Timer.Change(delay, Timeout.Infinite);
		}

		/// <summary>
		/// Stop the trickle of work
		/// </summary>
		public void Stop()
		{
			// flag as not-running to prevent next iteration
			this.isRunning = false;

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
				this.Queue.Enqueue(task);
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
				lock (this.Queue.SyncRoot)
				{
					// check if any pending work
					if (this.Queue.Count > 0)
					{
						// perform work
						task = this.Queue.Dequeue();
					}
				}

				if (task != null)
				{
					this.Defn.Perform(task);
				}
				else
				{
					// signal idle
					this.Defn.OnIdle(this);
				}
			}
			catch (Exception ex)
			{
				try
				{
					// increment and signal error
					this.ErrorCount++;
					this.Defn.OnError(this, task, ex);
				}
				catch { }
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
