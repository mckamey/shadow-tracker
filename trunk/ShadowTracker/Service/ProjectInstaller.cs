using System;
using System.ComponentModel;
using System.Configuration.Install;
using Shadow.Configuration;

namespace Shadow.Service
{
	[RunInstaller(true)]
	public partial class ProjectInstaller : Installer
	{
		/// <summary>
		/// Ctor
		/// </summary>
		public ProjectInstaller()
		{
			this.InitializeComponent();

			TrackerSettingsSection settings = TrackerSettingsSection.GetSettings();

			this.ShadowTrackerServiceInstaller.ServiceName = settings.ServiceName;
			this.ShadowTrackerServiceInstaller.DisplayName = settings.DisplayName;
			this.ShadowTrackerServiceInstaller.Description = settings.ServiceDescription;

			//this.ShadowTrackerServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
			//this.ShadowTrackerServiceProcessInstaller.Password = null;
			//this.ShadowTrackerServiceProcessInstaller.Username = null;
		}
	}
}
