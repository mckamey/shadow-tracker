using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		/// Gets and sets the source path of the bits
		/// </summary>
		[DefaultValue(null)]
		public string SourcePath { get; set; }

		/// <summary>
		/// Gets and sets target attributes
		/// </summary>
		[DefaultValue(null)]
		public CatalogEntry Entry { get; set; }

		#endregion Properties

		#region Object Overrides

		[DebuggerHidden]
		public override bool Equals(object obj)
		{
			var that = obj as NodeDelta;

			return (that != null) &&
				EqualityComparer<DeltaAction>.Default.Equals(this.Action, that.Action) &&
				StringComparer.OrdinalIgnoreCase.Equals(this.SourcePath, that.SourcePath) &&
				EqualityComparer<CatalogEntry>.Default.Equals(this.Entry, that.Entry);
		}

		[DebuggerHidden]
		public override int GetHashCode()
		{
			int hashcode = -834397989;
			hashcode = (-1521134295 * hashcode) + EqualityComparer<DeltaAction>.Default.GetHashCode(this.Action);
			hashcode = (-1521134295 * hashcode) + StringComparer.OrdinalIgnoreCase.GetHashCode(this.SourcePath);
			return ((-1521134295 * hashcode) + EqualityComparer<CatalogEntry>.Default.GetHashCode(this.Entry));
		}

		[DebuggerHidden]
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();

			builder.Append("{ Action = ");
			builder.Append(this.Action);

			builder.Append(", SourcePath = ");
			builder.Append(this.SourcePath);

			builder.Append(", Node = ");
			builder.Append(this.Entry);

			builder.Append(" }");

			return builder.ToString();
		}

		#endregion Object Overrides
	}
}
