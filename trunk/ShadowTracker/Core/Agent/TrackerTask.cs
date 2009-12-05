using System;
using System.IO;
using System.Text;

namespace Shadow.Agent
{
	public class TrackerTask
	{
		#region Properties

		public WatcherChangeTypes ChangeType
		{
			get;
			set;
		}

		public string FullPath
		{
			get;
			set;
		}

		public string OldFullPath
		{
			get;
			set;
		}

		public TaskPriority Priority
		{
			get;
			set;
		}

		public int RetryCount
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
			builder.Append(this.ChangeType.ToString());
			if (!String.IsNullOrEmpty(this.FullPath))
			{
				builder.Append("; Path=");
				builder.Append(this.FullPath);
			}
			if (!String.IsNullOrEmpty(this.OldFullPath))
			{
				builder.Append("; Old=");
				builder.Append(this.OldFullPath);
			}
			builder.Append(" }");
			return builder.ToString();
		}

		#endregion Object Overrides
	}
}
