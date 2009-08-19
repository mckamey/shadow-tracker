using System;
using System.Collections.Generic;

namespace Shadow.Model
{
	/// <summary>
	/// Represents a single catalog data node
	/// </summary>
	public class CatalogDelta
	{
		#region Properties

		/// <summary>
		/// Gets and sets the nodes which need updating
		/// </summary>
		public IEnumerable<NodeDelta> Actions { get; set; }

		/// <summary>
		/// Gets and sets the nodes which need to be removed
		/// </summary>
		public IEnumerable<string> Extras { get; set; }

		#endregion Properties
	}
}
