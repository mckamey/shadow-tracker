using System;
using System.Collections.Generic;
using System.Linq;

using Shadow.Model;

namespace Shadow.Agent
{
	public class Synchronizer
	{
		#region Delta Methods

		public IEnumerable<NodeDelta> FullCatalogSync(ICatalogRepository source, ICatalogRepository local)
		{
			// geterate the sequence of actions which represent the delta since
			return (
					from node in source.Entries
					let action = this.CalcNodeDelta(local, node)
					where action != DeltaAction.None
					let sourcePath =
						(action != DeltaAction.Clone) ? null :
						local.GetPathOfEntryBySignature(node.Signature)
					orderby action
					select new NodeDelta
					{
						Action = action,
						SourcePath = sourcePath,
						Node = node
					}
				).Union(
				// extras are any local entries not contained in target
					from node in local.Entries
					where !source.ContainsPath(node.Path)
					select new NodeDelta
					{
						Action = DeltaAction.Delete,
						Node = node
					}
				);
		}

		private DeltaAction CalcNodeDelta(ICatalogRepository catalog, CatalogEntry entry)
		{
			// look for existing node
			CatalogEntry local = catalog.GetEntryAtPath(entry.Path);

			if (local == null)
			{
				// file is missing, see if have a copy elsewhere (e.g. moved/renamed/copied)
				if (!entry.IsDirectory && catalog.ContainsSignature(entry.Signature))
				{
					// equivalent file found
					return DeltaAction.Clone;
				}

				// completely missing file, need to add
				return DeltaAction.Add;
			}

			if (entry.Equals(local))
			{
				// no changes, identical
				return DeltaAction.None;
			}

			if (StringComparer.OrdinalIgnoreCase.Equals(local.Signature, entry.Signature))
			{
				// correct bits exist at correct path but metadata is different
				return DeltaAction.Meta;
			}

			// bits are different, see if have a equivalent copy elsewhere
			if (!entry.IsDirectory && catalog.ContainsSignature(entry.Signature))
			{
				// equivalent file found
				return DeltaAction.Clone;
			}

			// file exists but bits are different
			return DeltaAction.Update;
		}

		#endregion Delta Methods

		#region Event Placeholders

		public void SyncCatalogs(ICatalogRepository local, ICatalogRepository target)
		{
			IEnumerable<NodeDelta> delta = this.FullCatalogSync(target, local);

			foreach (NodeDelta action in delta)
			{
				switch (action.Action)
				{
					case DeltaAction.Add:
					{
						Console.WriteLine("ADD \"{0}\" at \"{1}\"", action.Node.Signature, action.Node.Path);
						break;
					}
					case DeltaAction.Clone:
					{
						Console.WriteLine("COPY: \"{0}\" to \"{1}\"", action.SourcePath, action.Node.Path);
						break;
					}
					case DeltaAction.Update:
					{
						Console.WriteLine("REPLACE: \"{0}\" to \"{1}\"", action.Node.Signature, action.Node.Path);
						break;
					}
					case DeltaAction.Meta:
					{
						Console.WriteLine("ATTRIB: \"{0}\"", action.Node.Path);
						break;
					}
					case DeltaAction.Delete:
					{
						Console.WriteLine("REMOVE: \"{0}\"", action.Node.Path);
						break;
					}
					default:
					case DeltaAction.None:
					{
						Console.WriteLine("ERROR: "+action);
						break;
					}
				}
			}
		}

		#endregion Event Placeholders
	}
}
