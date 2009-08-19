using System;
using System.Collections.Generic;

namespace Shadow.Model
{
	/// <summary>
	/// Contains an in-memory listing of DataNodes with ability to lookup by file path or file signature.
	/// </summary>
	public class Catalog
	{
		#region Fields

		private readonly Dictionary<string, DataNode> Paths = new Dictionary<string, DataNode>(100, StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, DataNode> Signatures = new Dictionary<string, DataNode>(100, StringComparer.OrdinalIgnoreCase);

		#endregion Fields

		#region Properties

		/// <summary>
		/// Gets and sets the sequence of data nodes
		/// </summary>
		public IEnumerable<DataNode> Entries
		{
			get { return this.Paths.Values; }
			set { this.RebuildIndexes(value); }
		}

		#endregion Properties

		#region Methods

		private void RebuildIndexes(IEnumerable<DataNode> nodes)
		{
			this.Paths.Clear();
			this.Signatures.Clear();

			foreach (DataNode entry in nodes)
			{
				this.Paths[entry.Path] = entry;
				if (entry.HasSignature)
				{
					this.Signatures[entry.Signature] = entry;
				}
			}
		}

		internal DataNode GetNodeAtPath(string path)
		{
			return this.Paths[path];
		}

		internal bool ContainsPath(string path)
		{
			return this.Paths.ContainsKey(path);
		}

		internal bool ContainsSignature(string signature)
		{
			return this.Signatures.ContainsKey(signature);
		}

		internal DataNode GetNodeWithSignature(string signature)
		{
			return this.Signatures[signature];
		}

		#endregion Methods
	}
}
