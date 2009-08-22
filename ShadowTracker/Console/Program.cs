using System;
using System.Configuration;

using Shadow.Agent;
using Shadow.Model;

namespace Shadow.ConsoleTest
{
	class Program
	{
		static void Main(string[] args)
		{
			FileTracker tracker = new FileTracker();
			string watchFolder = ConfigurationManager.AppSettings["WatchFolder"];
			string watchFilter = ConfigurationManager.AppSettings["WatchFilter"];

			Console.WriteLine("Initializing " + watchFolder);
			ConsoleCatalog catalog = new ConsoleCatalog(new MemoryTable<CatalogEntry>(CatalogEntry.PathComparer));
			FileUtility.LoadCatalog(catalog, watchFolder);

			Console.WriteLine("Begin tracking " + watchFolder);
			tracker.Start(watchFolder, watchFilter, catalog);

			Console.WriteLine("Press ENTER to exit.");
			Console.ReadLine();

			tracker.Stop();
		}
	}
}
