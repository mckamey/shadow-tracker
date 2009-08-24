using System;
using System.Configuration;

using Shadow.Agent;
using Shadow.Model;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace Shadow.ConsoleTest
{
	class Program
	{
		static void Main(string[] args)
		{
			string watchFolder = ConfigurationManager.AppSettings["WatchFolder"];
			string pathFilter = ConfigurationManager.AppSettings["PathFilter"];
			string fileFilter = ConfigurationManager.AppSettings["FileFilter"] ?? "";
			var callback = FileUtility.CreateFileFilter(fileFilter.Split(',', '|'));

			string connection = ConfigurationManager.ConnectionStrings["CatalogDB"].ConnectionString;
			string mappings = ConfigurationManager.AppSettings["SqlMapping"];
			DataContext db = GetDataContext(connection, mappings);

			Console.WriteLine("Initializing " + watchFolder);
			ConsoleCatalog catalog = new ConsoleCatalog(db);
			FileUtility.SyncCatalog(catalog, watchFolder, callback, -1);

			Console.WriteLine("Begin tracking " + watchFolder);
			FileTracker tracker = new FileTracker();
			tracker.Start(catalog, watchFolder, pathFilter, callback);

			Console.WriteLine("Press ENTER to exit.");
			Console.ReadLine();

			tracker.Stop();
			db.SubmitChanges();
		}

		private static DataContext GetDataContext(string connection, string mappings)
		{
			MappingSource map = XmlMappingSource.FromUrl(mappings);
			DataContext db = new DataContext(connection, map);

			if (!db.DatabaseExists())
			{
				Console.Write("Database in connection string does not exist. Want to create it? (y/n):");

				string answer = Console.ReadLine();
				if (!StringComparer.OrdinalIgnoreCase.Equals(answer, "y"))
				{
					throw new Exception("Database in connection string does not exist.");
				}

				try
				{
					db.CreateDatabase();
				}
				catch (Exception ex)
				{
					// TODO: handle this situation
					Console.Error.WriteLine(ex.Message);
				}
			}

			return db;
		}
	}
}
