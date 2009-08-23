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
			string pathFilter = ConfigurationManager.AppSettings["PathFilter"];
			string fileFilter = ConfigurationManager.AppSettings["FileFilter"] ?? "";
			var callback = FileUtility.CreateFileFilter(fileFilter.Split(',', '|'));

			Console.WriteLine("Initializing " + watchFolder);
			ConsoleCatalog catalog = new ConsoleCatalog(new MemoryTable<CatalogEntry>(CatalogEntry.PathComparer));
			FileUtility.SyncCatalog(catalog, watchFolder, callback);

			Console.WriteLine("Begin tracking " + watchFolder);
			tracker.Start(watchFolder, pathFilter, catalog);

			Console.WriteLine("Press ENTER to exit.");
			Console.ReadLine();

			tracker.Stop();
		}
	}
}
