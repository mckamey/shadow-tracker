using System;
using System.Collections.Generic;

namespace Shadow.Tasks
{
	public interface ITaskStrategy<T> : IComparer<T>
	{
		/// <summary>
		/// Gets the delay between task cycles
		/// </summary>
		TimeSpan Delay
		{
			get;
		}

		/// <summary>
		/// Performs a single cycle of work
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="task"></param>
		void Execute(TaskEngine<T> engine, T task);

		/// <summary>
		/// Gives a chance to intercept task additions
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="task"></param>
		/// <returns>true if task should be added</returns>
		bool OnAddTask(TaskEngine<T> engine, T task);

		/// <summary>
		/// Callback when a work item generates an exception
		/// </summary>
		/// <param name="engine"></param>
		/// <param name="task"></param>
		/// <param name="error"></param>
		/// <remarks>
		/// Provides an opportunity to reprioritize and/or retry
		/// </remarks>
		void OnError(TaskEngine<T> engine, T task, Exception error);

		/// <summary>
		/// Callback when no work is ready to be performed</summary>
		/// </summary>
		/// <param name="engine"></param>
		/// <remarks>
		/// Provides an opportunity to perform background cleanup
		/// </remarks>
		void OnIdle(TaskEngine<T> engine);
	}
}
