using System;
using System.IO;
using System.Linq;

using Shadow.Model;

namespace Shadow.Agent
{
	public class Synchronizer
	{
		#region Delta Methods

		public void SyncCatalogs(Catalog local, Catalog target)
		{
			CatalogDelta delta = this.GetDelta(local, target);

			using (TextWriter writer = File.CreateText(@"X:\ExampleActions.txt"))
			{
				foreach (NodeDelta action in delta.Actions)
				{
					switch(action.Action)
					{
						case DeltaAction.Meta:
						{
							this.UpdateNode(writer, action);
							break;
						}
						case DeltaAction.Copy:
						{
							this.CopyNode(writer, action, action.Local.Path);
							break;
						}
						case DeltaAction.Data:
						{
							this.AddNode(writer, action);
							break;
						}
						default:
						case DeltaAction.None:
						{
							writer.WriteLine("ERROR! Unknown action");
							break;
						}
					}

					writer.WriteLine();
				}

				foreach (string path in delta.Extras)
				{
					this.RemoveNode(writer, path);
				}
			}
		}

		public CatalogDelta GetDelta(Catalog local, Catalog target)
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
				where !target.ContainsPath(node.Path)
				select node.Path;

			return delta;
		}

		private static NodeDelta CalcNodeDelta(Catalog catalog, DataNode target)
		{
			// look for existing node
			if (!catalog.ContainsPath(target.Path))
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
				if (catalog.ContainsSignature(target.Signature))
				{
					// equivalent file found
					return new NodeDelta
					{
						Action=DeltaAction.Copy,
						Local=catalog.GetNodeWithSignature(target.Signature),
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

			DataNode local = catalog.GetNodeAtPath(target.Path);
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
			if (catalog.ContainsSignature(target.Signature))
			{
				// equivalent file found
				return new NodeDelta
				{
					Action=DeltaAction.Copy,
					Local=catalog.GetNodeWithSignature(target.Signature),
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

		#region Events

		private void AddNode(TextWriter writer, NodeDelta action)
		{
			writer.WriteLine("DOWNLOAD \"{0}\"", action.Target.Signature);
			string sourcePath = action.Target.Signature;
			this.CopyNode(writer, action, sourcePath);
		}

		private void CopyNode(TextWriter writer, NodeDelta action, string sourcePath)
		{
			writer.WriteLine("COPY: \"{0}\" to \"{1}\"", sourcePath, action.Target.Path);
			this.UpdateNode(writer, action);
		}

		private void UpdateNode(TextWriter writer, NodeDelta action)
		{
			writer.WriteLine("ATTRIB: \"{0}\"", action.Target.Path);
		}

		private void RemoveNode(TextWriter writer, string path)
		{
			writer.WriteLine("REMOVE: \"{0}\"", path);
			writer.WriteLine();
		}

		#endregion Events
	}
}
