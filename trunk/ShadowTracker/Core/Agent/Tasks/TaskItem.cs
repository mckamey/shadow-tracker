using System;

namespace Shadow.Agent.Tasks
{
	/// <summary>
	/// Represents one cycle of work
	/// </summary>
	public class TaskItem
	{
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
