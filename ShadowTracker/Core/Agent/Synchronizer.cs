using System;
using System.Collections.Generic;
using System.Linq;

using Shadow.Model;

namespace Shadow.Agent
{
	public class Synchronizer
	{
		#region Delta Methods

		public IEnumerable<NodeDelta> GetDelta(Catalog local, Catalog target)
		{
			// sequence of actions to take place
			return (
					from node in target.Entries
					let action = this.CalcNodeDelta(local, node)
					where action != null
					orderby action.Action
					select action
				).Union(
					// extras are any local entries not contained in target
					from node in local.Entries
					where !target.ContainsPath(node.Path)
					select new NodeDelta
						{
							Action = DeltaAction.Delete,
							Target = node
						}
				);
		}

		private NodeDelta CalcNodeDelta(Catalog catalog, DataNode target)
		{
			// look for existing node
			if (!catalog.ContainsPath(target.Path))
			{
				// check if is an empty directory
				if (target.IsDirectory)
				{
					// can build directory from metadata alone
					return new NodeDelta
					{
						Action = DeltaAction.Meta,
						Target = target
					};
				}

				// file is missing, see if have a copy elsewhere (e.g. moved/copied/renamed)
				if (catalog.ContainsSignature(target.Signature))
				{
					// equivalent file found
					return new NodeDelta
					{
						Action = DeltaAction.Clone,
						ClonePath = catalog.GetPathOfNodeWithSignature(target.Signature),
						Target = target
					};
				}

				// completely missing file, need to add
				return new NodeDelta
				{
					Action = DeltaAction.Add,
					Target = target
				};
			}

			DataNode local = catalog.GetNodeAtPath(target.Path);
			if (target.Equals(local))
			{
				// no changes, identical
				return null;
			}

			if (StringComparer.OrdinalIgnoreCase.Equals(local.Signature, target.Signature))
			{
				// correct bits exist at correct path but metadata is different
				return new NodeDelta
				{
					Action = DeltaAction.Meta,
					Target = target
				};
			}

			// bits are different, see if have a equivalent copy elsewhere
			if (catalog.ContainsSignature(target.Signature))
			{
				// equivalent file found
				return new NodeDelta
				{
					Action = DeltaAction.Clone,
					ClonePath = catalog.GetPathOfNodeWithSignature(target.Signature),
					Target = target
				};
			}

			// file exists but bits are different
			return new NodeDelta
			{
				Action = DeltaAction.Update,
				Target = target
			};
		}

		#endregion Delta Methods

		#region Event Placeholders

		public void SyncCatalogs(Catalog local, Catalog target)
		{
			IEnumerable<NodeDelta> delta = this.GetDelta(local, target);

			foreach (NodeDelta action in delta)
			{
				switch (action.Action)
				{
					case DeltaAction.Add:
					{
						Console.WriteLine("ADD \"{0}\" at \"{1}\"", action.Target.Signature, action.Target.Path);
						break;
					}
					case DeltaAction.Clone:
					{
						Console.WriteLine("COPY: \"{0}\" to \"{1}\"", action.ClonePath, action.Target.Path);
						break;
					}
					case DeltaAction.Update:
					{
						Console.WriteLine("REPLACE: \"{0}\" to \"{1}\"", action.Target.Signature, action.Target.Path);
						break;
					}
					case DeltaAction.Meta:
					{
						Console.WriteLine("ATTRIB: \"{0}\"", action.Target.Path);
						break;
					}
					case DeltaAction.Delete:
					{
						Console.WriteLine("REMOVE: \"{0}\"", action.Target.Path);
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
