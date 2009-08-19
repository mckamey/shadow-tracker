using System;
using System.Collections.Generic;
using System.Linq;

namespace Shadow.Model
{
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
			set { this.RebuildEntries(value); }
		}

		#endregion Properties

		#region Methods

		private void RebuildEntries(IEnumerable<DataNode> nodes)
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

		#endregion Methods

		#region Delta Methods

		public static CatalogDelta GetDelta(Catalog local, Catalog target)
		{
			CatalogDelta delta = new CatalogDelta();

			// sequence of actions to take place
			delta.Actions =
				from node in target.Entries
				let action = CalcNodeDelta(local, node)
				where action != null
				orderby action.Action
				select action;

			// extras are any local entries not contained in target
			delta.Extras =
				from node in local.Entries
				where !target.Paths.ContainsKey(node.Path)
				select node.Path;

			return delta;
		}

		private static NodeDelta CalcNodeDelta(Catalog catalog, DataNode target)
		{
			// look for existing node
			if (!catalog.Paths.ContainsKey(target.Path))
			{
				// check if is an empty directory
				if (target.IsDirectory)
				{
					// can build directory from metadata
					return new NodeDelta
					{
						Action=DeltaAction.Meta,
						Target=target
					};
				}

				// file is missing, see if have a copy elsewhere (e.g. moved/copied/renamed)
				if (catalog.Signatures.ContainsKey(target.Signature))
				{
					// equivalent file found
					return new NodeDelta
					{
						Action=DeltaAction.Copy,
						Local=catalog.Signatures[target.Signature],
						Target=target
					};
				}

				// completely missing file, need to download
				return new NodeDelta
				{
					Action=DeltaAction.Data,
					Target=target
				};
			}

			DataNode local = catalog.Paths[target.Path];
			if (target.Equals(local))
			{
				// no changes, identical
				return null;
			}

			if (StringComparer.OrdinalIgnoreCase.Equals(local.Signature, target.Signature))
			{
				// file exists with correct bits but metadata has changed
				return new NodeDelta
				{
					Action=DeltaAction.Meta,
					Local=local,
					Target=target
				};
			}

			// bits are different, see if have a equivalent copy elsewhere
			if (catalog.Signatures.ContainsKey(target.Signature))
			{
				// equivalent file found
				return new NodeDelta
				{
					Action=DeltaAction.Copy,
					Local=catalog.Signatures[target.Signature],
					Target=target
				};
			}

			// file exists but bits are different
			return new NodeDelta
			{
				Action=DeltaAction.Data,
				Local=local,
				Target=target
			};
		}

		#endregion Delta Methods
	}
}
