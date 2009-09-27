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

			this.ServiceInstaller.ServiceName = settings.ServiceName;
			this.ServiceInstaller.DisplayName = settings.DisplayName;
			this.ServiceInstaller.Description = settings.ServiceDescription;

			//this.ServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
			//this.ServiceProcessInstaller.Password = null;
			//this.ServiceProcessInstaller.Username = null;
		}
	}
}
