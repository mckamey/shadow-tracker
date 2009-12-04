using System;
using System.Text;

namespace Shadow.Agent.Tasks
{
	/// <summary>
	/// Represents one cycle of work
	/// </summary>
	public class TaskItem
	{
		#region Properties

		/// <summary>
		/// Gets and sets a key that helps for deduping tasks
		/// </summary>
		public virtual string Key
		{
			get;
			set;
		}

		/// <summary>
		/// Gets and sets the actual work method
		/// </summary>
		public virtual Action Perform
		{
			get;
			set;
		}

		/// <summary>
		/// Gets and sets the priority level of the work item
		/// </summary>
		public virtual TaskPriority Priority
		{
			get;
			set;
		}

		/// <summary>
		/// Gets and sets a count of number of failures
		/// </summary>
		public virtual int RetryCount
		{
			get;
			set;
		}

		#endregion Properties

		#region Object Overrides

		/// <summary>
		/// Displays the TaskItem as a string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder("{ ");
			builder.Append(this.Priority.ToString());
			if (!String.IsNullOrEmpty(this.Key))
			{
				builder.Append(": ");
				builder.Append(this.Key);
			}
			builder.Append(" }");
			return builder.ToString();
		}

		#endregion Object Overrides
	}
}
