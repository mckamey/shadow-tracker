using System;
using System.Configuration;
using System.ServiceProcess;

using Shadow.Watcher;

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
			string watchFilter = ConfigurationManager.AppSettings["WatchFilter"];

			this.Tracker.Start(watchFolder, watchFilter);
		}

		protected override void OnStop()
		{
			this.Tracker.Stop();
		}

		#endregion Methods
	}
}
