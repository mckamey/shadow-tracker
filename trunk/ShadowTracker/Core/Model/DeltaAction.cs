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
		None = 0,

		/// <summary>
		/// Bits are missing, need to import
		/// </summary>
		Add = 1,

		/// <summary>
		/// Bits are different at path, replace
		/// </summary>
		Update = 2,

		/// <summary>
		/// Bits exist but at different path
		/// </summary>
		Clone = 3,

		/// <summary>
		/// Bits exist but metadata needs to be updated
		/// </summary>
		Meta = 4,

		/// <summary>
		/// Bits are extra, need to remove
		/// </summary>
		Delete = 5
	}
}
