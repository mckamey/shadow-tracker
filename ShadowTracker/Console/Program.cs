using System;
using System.Configuration;
using System.Data.Linq;
using System.Data.Linq.Mapping;

using Shadow.Agent;
using Shadow.Model;

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
#if DEBUG
			var watch = System.Diagnostics.Stopwatch.StartNew();
#endif
			FileUtility.SyncCatalog(
				catalog,
				watchFolder,
				callback,
				FileUtility.DefaultTrickleRate,
				delegate(CatalogRepository c)
				{
#if DEBUG
					watch.Stop();
					Console.WriteLine("__________________________");
					Console.WriteLine();
					Console.WriteLine("Sync Time: "+watch.Elapsed);
					Console.WriteLine("__________________________");
#endif
				});

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
			if (connection != null && connection.IndexOf("|DataDirectory|") >= 0)
			{
				connection = connection.Replace("|DataDirectory|", Environment.CurrentDirectory);
			}

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
