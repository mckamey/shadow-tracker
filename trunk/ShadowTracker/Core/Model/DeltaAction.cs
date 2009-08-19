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
		/// Data exists but metadata needs to be updated
		/// </summary>
		Meta = 1,

		/// <summary>
		/// Data exists but in different location
		/// </summary>
		/// <remarks>
		/// Includes Meta action
		/// </remarks>
		Copy = 2,

		/// <summary>
		/// Data is missing
		/// </summary>
		/// <remarks>
		/// Includes Meta action
		/// </remarks>
		Data = 3
	}
}
