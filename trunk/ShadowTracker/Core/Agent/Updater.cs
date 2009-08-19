using System;
using System.IO;

using Shadow.Model;

namespace Shadow.Agent
{
	public class Updater
	{
		#region Fields

		private readonly string RootPath;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="rootPath"></param>
		public Updater(string rootPath)
		{
			if (String.IsNullOrEmpty(rootPath))
			{
				throw new ArgumentNullException("Root is invalid.");
			}

			if (rootPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				rootPath.TrimEnd(Path.DirectorySeparatorChar);
			}

			this.RootPath = rootPath;
		}

		#endregion Init

		#region Methods

		public void PerformUpdate(Catalog local, Catalog target)
		{
			CatalogDelta delta = Catalog.GetDelta(local, target);

			using (TextWriter writer = File.CreateText(@"X:\ExampleActions.txt"))
			{
				foreach (NodeDelta action in delta.Actions)
				{
					switch(action.Action)
					{
						case DeltaAction.Meta:
						{
							this.SyncMetaData(writer, action);
							break;
						}
						case DeltaAction.Copy:
						{
							this.CopyData(writer, action, action.Local.Path);
							break;
						}
						case DeltaAction.Data:
						{
							this.DownloadData(writer, action);
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
					writer.WriteLine("REMOVE: \"{0}\"", path);
					writer.WriteLine();
				}
			}
		}

		private void DownloadData(TextWriter writer, NodeDelta action)
		{
			writer.WriteLine("DOWNLOAD \"{0}\"", action.Target.Signature);
			string sourcePath = action.Target.Signature;
			this.CopyData(writer, action, sourcePath);
		}

		private void CopyData(TextWriter writer, NodeDelta action, string sourcePath)
		{
			writer.WriteLine("COPY: \"{0}\" to \"{1}\"", sourcePath, action.Target.Path);
			this.SyncMetaData(writer, action);
		}

		private void SyncMetaData(TextWriter writer, NodeDelta action)
		{
			writer.WriteLine("ATTRIB: \"{0}\"", action.Target.Path);
		}

		#endregion Methods
	}
}
