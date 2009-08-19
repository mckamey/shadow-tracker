using System;

namespace Shadow.Model
{
	/// <summary>
	/// Represents the action for a single catalog data node
	/// </summary>
	public enum DeltaAction
	{
		/// <summary>
		/// No action required
		/// </summary>
		None,

		/// <summary>
		/// Bits are missing, need to import
		/// </summary>
		Add,

		/// <summary>
		/// Bits are different at path, replace
		/// </summary>
		Update,

		/// <summary>
		/// Bits exist but at different path
		/// </summary>
		Clone,

		/// <summary>
		/// Bits exist but metadata needs to be updated
		/// </summary>
		Meta,

		/// <summary>
		/// Bits are extra, need to remove
		/// </summary>
		Delete
	}
}
