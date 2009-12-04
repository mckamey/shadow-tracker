using System;
using System.Collections.Generic;

namespace Shadow.Agent.Tasks
{
	/// <summary>
	/// Represents one cycle of work
	/// </summary>
	public class TaskItem
	{
		#region TaskItemEqualityComparer

		public static readonly IEqualityComparer<TaskItem> EqualityComparer = new TaskItemEqualityComparer();

		private class TaskItemEqualityComparer : IEqualityComparer<TaskItem>
		{
			#region IEqualityComparer<TaskItem> Members

			public bool Equals(TaskItem x, TaskItem y)
			{
				return StringComparer.OrdinalIgnoreCase.Equals(x.Key, y.Key);
			}

			public int GetHashCode(TaskItem obj)
			{
				return StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
			}

			#endregion IEqualityComparer<T> Members
		}

		#endregion TaskItemEqualityComparer

		#region Properties

		/// <summary>
		/// Gets a key that allows for deduping tasks
		/// </summary>
		public string Key
		{
			get;
			set;
		}

		/// <summary>
		/// Gets and sets the priority level of the work item
		/// </summary>
		public TaskPriority Priority
		{
			get;
			set;
		}

		/// <summary>
		/// Gets and sets the actual work method
		/// </summary>
		public Action Perform
		{
			get;
			set;
		}

		#endregion Properties
	}
}
