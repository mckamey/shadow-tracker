using System;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.ServiceProcess;

namespace Shadow.Service
{
	static class Program
	{
		#region Constants

		private static readonly string ServiceLocation = Assembly.GetExecutingAssembly().Location;

		#endregion Constants

		#region Main

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			ShadowTrackerService service = new ShadowTrackerService();

			if (args.Length < 1)
			{
				string logName = Path.Combine(ShadowTrackerService.ServiceDirectory, DateTime.Now.ToString("yyyy-MM-dd-HHmm_"));

				try
				{
					service.Error = File.AppendText(logName+"Error.txt");
					service.Error.WriteLine("Log created: " +logName+"Error.txt");
					service.Error.WriteLine();
					Console.SetError(service.Error);
				}
				catch (Exception ex)
				{
					service.Error.WriteLine("Error creating Error log");
					service.Error.WriteLine(ex);
				}

				try
				{
					service.Out = File.AppendText(logName+"Log.txt");
					service.Out.WriteLine("Log created: " +logName+"Log.txt");
					service.Out.WriteLine();
					Console.SetOut(service.Out);
				}
				catch (Exception ex)
				{
					service.Error.WriteLine("Error creating Out log");
					service.Error.WriteLine(ex);
				}

				ServiceBase.Run(service);
			}
			else
			{
				string serviceName = Path.GetFileName(ServiceLocation);

				service.In = Console.In;
				service.Out = Console.Out;
				service.Error = Console.Error;

				switch (args[0].Replace("/", "").Replace("-", "").Trim().ToLowerInvariant())
				{
					case "c":
					case "console":
					{
						Console.WriteLine("Running {0} as Console", serviceName);

						service.Begin(args);

						Console.WriteLine("Press any key to exit.");
						Console.ReadKey(true);

						service.End();
						break;
					}
					case "i":
					case "install":
					{
						Console.WriteLine("Installing {0} as Windows Service", serviceName);

						try
						{
							service.InstallDatabase();

							ManagedInstallerClass.InstallHelper(new string[] { ServiceLocation });

							Console.WriteLine("Installation successful");
						}
						catch (Exception ex)
						{
							Console.Error.WriteLine("Installation failed: "+ex);
						}
						break;
					}
					case "u":
					case "uninstall":
					{
						Console.WriteLine("Uninstalling {0} as Windows Service", serviceName);

						try
						{
							ManagedInstallerClass.InstallHelper(new string[] { "/u", ServiceLocation });

							Console.WriteLine("Uninstallation successful.");
						}
						catch (Exception ex)
						{
							Console.Error.WriteLine("Uninstallation failed: "+ex);
						}
						break;
					}
					default:
					case "?":
					{
						Console.WriteLine("USAGE:");
						Console.WriteLine("\t"+serviceName+" [/? | /console | /install | /uninstall]");
						Console.WriteLine();
						Console.WriteLine("Options:");
						Console.WriteLine("\t/?\t\tDisplay this help message");
						Console.WriteLine("\t/console\tRun the tracker as a console application");
						Console.WriteLine("\t/install\tInstall the tracker as a Windows Service");
						Console.WriteLine("\t/uninstall\tUninstall the tracker as a Windows Service");
						Console.WriteLine();
						Console.WriteLine("Examples:");
						Console.WriteLine("\t> "+serviceName+" /console\t... Run as console");
						Console.WriteLine("\t> "+serviceName+" /install\t... Install service");
						Console.WriteLine("\t> "+serviceName+" /uninstall\t... Uninstall service");
						Console.WriteLine();
						break;
					}
				}
			}
		}

		#endregion Main
	}
}
