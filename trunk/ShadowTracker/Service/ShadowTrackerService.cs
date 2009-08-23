using System;
using System.Configuration;
using System.ServiceProcess;

using Shadow.Agent;
using Shadow.Model;

namespace ShadowTrackerService
{
	public partial class ShadowTrackerService : ServiceBase
	{
		#region Fields

		private readonly FileTracker Tracker = new FileTracker();

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public ShadowTrackerService()
		{
			this.InitializeComponent();
		}

		#endregion Init

		#region Methods

		protected override void OnStart(string[] args)
		{
			string watchFolder = ConfigurationManager.AppSettings["WatchFolder"];
			string pathFilter = ConfigurationManager.AppSettings["PathFilter"];
			string fileFilter = ConfigurationManager.AppSettings["FileFilter"] ?? "";
			var callback = FileUtility.CreateFileFilter(fileFilter.Split(',', '|'));

			CatalogRepository catalog = new CatalogRepository(new MemoryTable<CatalogEntry>(CatalogEntry.PathComparer));
			FileUtility.SyncCatalog(catalog, watchFolder, callback);

			this.Tracker.Start(catalog, watchFolder, pathFilter);
		}

		protected override void OnStop()
		{
			this.Tracker.Stop();
		}

		#endregion Methods
	}
}
