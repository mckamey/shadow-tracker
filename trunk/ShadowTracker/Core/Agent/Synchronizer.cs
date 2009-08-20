using System;
using System.Collections.Generic;
using System.Linq;

using Shadow.Model;

namespace Shadow.Agent
{
	public class Synchronizer
	{
		#region Delta Methods

		public IEnumerable<NodeDelta> FullCatalogSync(Catalog source, Catalog local)
		{
			// geterate the sequence of actions which represent the delta since
			return (
					from node in source.Entries
					let action = local.CalcNodeDelta(node)
					where action != DeltaAction.None
					let sourcePath =
						(action != DeltaAction.Clone) ? null :
						this.GetPathOfEntryBySignature(local, node.Signature)
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
					where !this.ContainsPath(source, node.Path)
					select new NodeDelta
					{
						Action = DeltaAction.Delete,
						Node = node
					}
				);
		}

		private bool ContainsPath(Catalog catalog, string path)
		{
			IQueryable<string> query =
				from entry in catalog.Entries
				where entry.Path == path
				select entry.Path;

			return query.Any();
		}

		private string GetPathOfEntryBySignature(Catalog catalog, string hash)
		{
			IQueryable<string> query =
				from entry in catalog.Entries
				where entry.Signature == hash
				select entry.Path;

			return query.FirstOrDefault();
		}

		#endregion Delta Methods

		#region Event Placeholders

		public void SyncCatalogs(Catalog local, Catalog target)
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
