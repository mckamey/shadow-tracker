using System;
using System.IO;
using System.ServiceProcess;

namespace Shadow.Service
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			ShadowTrackerService service = new ShadowTrackerService();

			if (args.Length < 1)
			{
				string logName = DateTime.Now.ToString("yyyy-MM-dd-HHmm")+"_ShadowTrackerService.txt";

				try
				{
					service.Error = File.AppendText(logName);
				}
				catch (Exception ex)
				{
					service.Error.WriteLine(ex);
				}

				try
				{
					service.Out = File.AppendText(logName);
				}
				catch (Exception ex)
				{
					service.Error.WriteLine(ex);
				}

				ServiceBase.Run(service);
			}
			else
			{
				service.In = Console.In;
				service.Out = Console.Out;
				service.Error = Console.Error;

				switch (args[0].ToLowerInvariant())
				{
					case "/?":
					{
						Console.WriteLine("USAGE:");
						Console.WriteLine("\tShadowTracker.Service.exe [/? | /console | /install | /uninstall]");
						Console.WriteLine();
						Console.WriteLine("Options:");
						Console.WriteLine("\t/?\t\tDisplay this help message");
						Console.WriteLine("\t/console\tRun the tracker as a console application");
						Console.WriteLine("\t/install\tInstall the tracker as a Windows Service");
						Console.WriteLine("\t/uninstall\tUninstall the tracker as a Windows Service");
						Console.WriteLine();
						Console.WriteLine("Examples:");
						Console.WriteLine("\t> ShadowTracker.Service.exe /console\t... Run as console");
						Console.WriteLine("\t> ShadowTracker.Service.exe /install\t... Install service");
						Console.WriteLine("\t> ShadowTracker.Service.exe /uninstall\t... Uninstall service");
						Console.WriteLine();
						break;
					}
					case "/console":
					{
						Console.WriteLine("Running ShadowTracker as Console");

						try
						{
							service.Begin(args);
						}
						catch (Exception ex)
						{
							Console.Error.WriteLine(ex);
						}

						Console.WriteLine("Press any key to exit.");
						Console.ReadKey(true);

						try
						{
							service.End();
						}
						catch (Exception ex)
						{
							Console.Error.WriteLine(ex);
						}
						break;
					}
					case "/install":
					{
						Console.WriteLine("Installing ShadowTracker as Windows Service");
						Console.WriteLine("Press any key to continue.");
						Console.ReadKey(true);

						try
						{
							service.InstallDatabase();

							// TODO: install service

							Console.WriteLine("Installation successful");
						}
						catch (Exception ex)
						{
							Console.Error.WriteLine("Installation failed: "+ex);
						}
						Console.WriteLine("Press any key to exit.");
						Console.ReadKey(true);
						break;
					}
					case "/uninstall":
					{
						Console.WriteLine("Uninstalling ShadowTracker as Windows Service");
						Console.WriteLine("Press any key to continue.");
						Console.ReadKey(true);

						try
						{
							// TODO: uninstall service

							Console.WriteLine("Uninstallation successful.");
						}
						catch (Exception ex)
						{
							Console.Error.WriteLine("Uninstallation failed: "+ex);
						}
						Console.WriteLine("Press any key to exit.");
						Console.ReadKey(true);
						break;
					}
				}
			}
		}
	}
}
