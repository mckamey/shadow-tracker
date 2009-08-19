using System;
using System.Diagnostics;
using System.IO;

using Shadow.Agent;
using Shadow.IO;
using Shadow.Model;

namespace Shadow.Test
{
	class Program
	{
		static void Main(string[] args)
		{
#if DEBUG
			string rootPath = @"X:\ExampleStore\";
			string masterPath = @"X:\ExampleMaster.txt";
			string mirrorPath = @"X:\ExampleMirror.txt";

			Catalog target = new Cataloger(rootPath).CreateCatalog();
			CatalogWriter.Save(target, File.Create(masterPath));

			Catalog local = CatalogReader.Read(File.OpenRead(mirrorPath));

			Synchronizer updater = new Synchronizer();
			updater.SyncCatalogs(local, target);
#else
			Console.Write("Enter the root of the repository: ");
			string rootPath = Console.ReadLine();
			Console.WriteLine();

			Console.Write("Enter the path to save the catalog: ");
			string catalogPath = Console.ReadLine();
			Console.WriteLine();

			Stopwatch timer = Stopwatch.StartNew();
			Console.Write("Building catalog");
			Catalog catalog = new Cataloger(rootPath).CreateCatalog();
			Console.WriteLine();
			Console.WriteLine("Done. Elapsed: "+timer.Elapsed);
			Console.WriteLine();

			timer = Stopwatch.StartNew();
			Console.WriteLine("Writing catalog...");
			CatalogWriter.Save(catalog, File.Create(catalogPath));
			Console.WriteLine("Done. Elapsed: "+timer.Elapsed);

			Console.WriteLine();
			Console.Write("Press ENTER to exit.");
			Console.ReadLine();
#endif
		}
	}
}
