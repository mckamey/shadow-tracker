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

				// TODO: handle command line args

				service.Begin(args);

				Console.WriteLine("Press any key to exit.");
				Console.ReadKey(true);

				service.End();
			}
		}
	}
}
