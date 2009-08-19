using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Shadow.Model
{
	/// <summary>
	/// Represents the changes to a single catalog data node
	/// </summary>
	public class NodeDelta
	{
		#region Properties

		/// <summary>
		/// Gets and sets the action to be performed
		/// </summary>
		[DefaultValue(DeltaAction.None)]
		public DeltaAction Action { get; set; }

		/// <summary>
		/// Gets and sets the relative path to the data
		/// </summary>
		[DefaultValue(null)]
		public DataNode Local { get; set; }

		/// <summary>
		/// Gets and sets file attributes
		/// </summary>
		[DefaultValue(null)]
		public DataNode Target { get; set; }

		#endregion Properties

		#region Object Overrides

		[DebuggerHidden]
		public override bool Equals(object obj)
		{
			var that = obj as NodeDelta;

			return (that != null) &&
				EqualityComparer<DeltaAction>.Default.Equals(this.Action, that.Action) &&
				EqualityComparer<DataNode>.Default.Equals(this.Local, that.Local) &&
				EqualityComparer<DataNode>.Default.Equals(this.Target, that.Target);
		}

		[DebuggerHidden]
		public override int GetHashCode()
		{
			int hashcode = -834397989;
			hashcode = (-1521134295 * hashcode) + EqualityComparer<DeltaAction>.Default.GetHashCode(this.Action);
			hashcode = (-1521134295 * hashcode) + EqualityComparer<DataNode>.Default.GetHashCode(this.Local);
			return ((-1521134295 * hashcode) + EqualityComparer<DataNode>.Default.GetHashCode(this.Target));
		}

		[DebuggerHidden]
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();

			builder.Append("{ Action = ");
			builder.Append(this.Action);

			builder.Append(", Local = ");
			builder.Append(this.Local);

			builder.Append(", Target = ");
			builder.Append(this.Target);

			builder.Append(" }");

			return builder.ToString();
		}

		#endregion Object Overrides
	}
}
