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
		/// Gets and sets the local path of the data
		/// </summary>
		[DefaultValue(null)]
		public string LocalPath { get; set; }

		/// <summary>
		/// Gets and sets target attributes
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
				StringComparer.OrdinalIgnoreCase.Equals(this.LocalPath, that.LocalPath) &&
				EqualityComparer<DataNode>.Default.Equals(this.Target, that.Target);
		}

		[DebuggerHidden]
		public override int GetHashCode()
		{
			int hashcode = -834397989;
			hashcode = (-1521134295 * hashcode) + EqualityComparer<DeltaAction>.Default.GetHashCode(this.Action);
			hashcode = (-1521134295 * hashcode) + StringComparer.OrdinalIgnoreCase.GetHashCode(this.LocalPath);
			return ((-1521134295 * hashcode) + EqualityComparer<DataNode>.Default.GetHashCode(this.Target));
		}

		[DebuggerHidden]
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();

			builder.Append("{ Action = ");
			builder.Append(this.Action);

			builder.Append(", Local = ");
			builder.Append(this.LocalPath);

			builder.Append(", Target = ");
			builder.Append(this.Target);

			builder.Append(" }");

			return builder.ToString();
		}

		#endregion Object Overrides
	}
}
