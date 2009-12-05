using System;

namespace Shadow.Tasks
{
	public interface ITaskEngineDefn<T>
	{
		/// <summary>
		/// Gets the delay between task cycles in milliseconds
		/// </summary>
		long Delay
		{
			get;
		}

		bool IsHigherPriority(T a, T b);

		/// <summary>
		/// Performs a single cycle of work.
		/// </summary>
		/// <param name="task"></param>
		void Perform(T task);

		/// <summary>
		/// Callback when a work item generates an exception
		/// </summary>
		/// <remarks>
		/// Provides an opportunity to reprioritize and/or retry
		/// </remarks>
		void OnError(TaskEngine<T> engine, T task, Exception ex);

		/// <summary>
		/// Callback when no work is ready to be performed</summary>
		/// </summary>
		/// <remarks>
		/// Provides an oppoortunity to queue more work.
		/// </remarks>
		void OnIdle(TaskEngine<T> engine);
	}
}
