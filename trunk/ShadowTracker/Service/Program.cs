using System;
using System.ServiceProcess;

namespace ShadowTrackerService
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			ServiceBase[] ServicesToRun;
			ServicesToRun = new ServiceBase[] 
			{ 
				new ShadowTrackerService() 
			};
			ServiceBase.Run(ServicesToRun);
		}
	}
}
