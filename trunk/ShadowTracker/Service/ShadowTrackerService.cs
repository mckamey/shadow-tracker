using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

using Microsoft.Practices.ServiceLocation;
using Shadow.Agent;
using Shadow.Configuration;
using Shadow.IO;
using Shadow.Model;
using Shadow.Model.L2S;

namespace Shadow.Service
{
	public partial class ShadowTrackerService : ServiceBase
	{
		#region Constants

		internal static readonly string ServiceDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;

		#endregion Constants

		#region Fields

		private FileTracker[] Trackers;
		private TextWriter outWriter;
		private TextWriter errorWriter;
		private IServiceLocator IoC;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public ShadowTrackerService()
		{
			this.Out = this.Error = TextWriter.Null;

			this.InitializeComponent();

			this.ServiceName = TrackerSettingsSection.GetSettings().ServiceName;
		}

		#endregion Init

		#region Properties

		public TextReader In
		{
			get;
			set;
		}

		public TextWriter Out
		{
			get { return this.outWriter; }
			set
			{
#if DEBUG
				this.outWriter = FlushedWriter.Create(value);
#else
				this.outWriter = TextWriter.Synchronized(value);
#endif
			}
		}

		public TextWriter Error
		{
			get { return this.errorWriter; }
			set
			{
				this.errorWriter = FlushedWriter.Create(value);
			}
		}

		#endregion Properties

		#region Service Events

		internal void Begin(string[] args)
		{
			this.OnStart(args);
		}

		protected override void OnStart(string[] args)
		{
			try
			{
				TrackerSettingsSection settings = TrackerSettingsSection.GetSettings();
				this.IoC = new SimpleServiceLocator(this.GetUnitOfWorkFactory(settings.SqlConnectionString, settings.SqlMapping));

				var filterCallback = FileUtility.CreateFileFilter(settings.FileFilters);

				IUnitOfWork unitOfWork = this.IoC.GetInstance<IUnitOfWork>();
				CatalogRepository repository = new CatalogRepository(unitOfWork);
				var version = repository.GetLatestVersion();

				this.Out.WriteLine("ShadowTracker");
				if (version != null)
				{
					this.Out.WriteLine("v"+version.Label+" ("+version.UpdatedDate.ToString("yyyy-MM-dd HH:mm")+")");
				}
				this.Out.WriteLine("__________________________");

				this.Out.WriteLine();
				this.Out.WriteLine("Connecting to database...");
				this.Out.WriteLine("__________________________");

				this.Out.WriteLine();
				this.Out.WriteLine("Beginning trickle update...");
				this.Out.WriteLine(settings.FileFilter);
				this.Out.WriteLine("__________________________");

				var watch = Stopwatch.StartNew();

				WatchFolderSettingsCollection folders = settings.WatchFolders;
				this.Trackers = new FileTracker[folders.Count];
				for (int i=0; i<folders.Count; i++)
				{
					this.Out.WriteLine();
					this.Out.WriteLine("Begin sync: "+folders[i].Name+" ("+folders[i].Path+")");

					new FileUtility(this.IoC).SyncCatalog(
						folders[i].Name,
						folders[i].Path,
						filterCallback,
						settings.TrickleRate,
						delegate(Catalog syncCatalog)
						{
							this.Out.WriteLine();
							this.Out.WriteLine("End sync: "+syncCatalog.Name+" ("+syncCatalog.Path+")"+Environment.NewLine+"Elapsed trickle update: "+watch.Elapsed);
							this.Out.WriteLine("__________________________");
						},
						delegate(Catalog syncCatalog, Exception ex)
						{
							this.Error.WriteLine("Error in sync: "+syncCatalog.Name+" ("+syncCatalog.Path+")");
							this.Error.WriteLine(ex);
						});

					this.Trackers[i] = new FileTracker(this.IoC);
					this.Trackers[i].TrackerError += this.OnError;

					Catalog catalog = repository.FindOrCreateCatalog(folders[i].Name, folders[i].Path);
					this.Trackers[i].Start(catalog, filterCallback);
				}

				this.Out.WriteLine();
				this.Out.WriteLine("Tracking started...");
				this.Out.WriteLine("__________________________");
			}
			catch (Exception ex)
			{
				this.Error.WriteLine(ex);

				// rethrow so SCM will stop and record in event log
				this.ExitCode = 10;		// ERROR_BAD_ENVIRONMENT, "The environment is incorrect"
				throw;
			}
		}

		internal void End()
		{
			this.OnStop();
		}

		protected override void OnStop()
		{
			foreach (FileTracker tracker in this.Trackers)
			{
				if (tracker == null)
				{
					continue;
				}

				tracker.Stop();
			}

			this.Out.WriteLine();
			this.Out.WriteLine("Tracking stopped.");
		}

		private void OnError(object sender, ErrorEventArgs e)
		{
			FileTracker tracker = sender as FileTracker;
			if (tracker == null || tracker.Catalog == null)
			{
				this.Error.WriteLine("Error in FileTracker:");
			}
			else
			{
				this.Error.WriteLine("Error in FileTracker: "+tracker.Catalog.Name+" ("+tracker.Catalog.Path+")");
			}
			this.Error.WriteLine(e.GetException());
		}

		public void InstallDatabase()
		{
			TrackerSettingsSection settings = TrackerSettingsSection.GetSettings();
			string connection = settings.SqlConnectionString;
			this.EnsureDatabase(ref connection, settings.SqlMapping);
		}

		#endregion Service Events

		#region Utility Methods

		private Func<string, IUnitOfWork> GetUnitOfWorkFactory(string connection, string mappings)
		{
			MappingSource map = this.EnsureDatabase(ref connection, mappings);

			return delegate(string key)
			{
				L2SUnitOfWork unitOfWork = new L2SUnitOfWork(new DataContext(connection, map));
				unitOfWork.OnCommit += this.OnCommit;
				return unitOfWork;
			};
		}

		private void OnCommit(L2SUnitOfWork unitOfWork, ChangeSet changes)
		{
			foreach (var inserted in changes.Inserts)
			{
				CatalogEntry entry = inserted as CatalogEntry;
				if (entry != null)
				{
					this.Out.WriteLine("ADD \"{0}\" at \"{1}\"", entry.Signature, entry.FullPath);
				}
				else if (inserted is Catalog)
				{
					this.Out.WriteLine("ADD Catalog at \"{0}\"", ((Catalog)inserted).Path);
				}
				else
				{
					this.Out.WriteLine("ADD "+inserted);
				}
			}
			foreach (var updated in changes.Updates)
			{
				CatalogEntry entry = updated as CatalogEntry;
				if (entry != null)
				{
					this.Out.WriteLine("UPDATE \"{0}\"", entry.FullPath);
				}
				else if (updated is Catalog)
				{
					this.Out.WriteLine("UPDATE Catalog \"{0}\"", ((Catalog)updated).Path);
				}
				else
				{
					this.Out.WriteLine("UPDATE "+updated);
				}
			}
			foreach (var deleted in changes.Deletes)
			{
				CatalogEntry entry = deleted as CatalogEntry;
				if (entry != null)
				{
					this.Out.WriteLine("REMOVE \"{0}\"", entry.FullPath);
				}
				else if (deleted is Catalog)
				{
					this.Out.WriteLine("REMOVE Catalog \"{0}\"", ((Catalog)deleted).Path);
				}
				else
				{
					this.Out.WriteLine("REMOVE "+deleted);
				}
			}
		}

		private MappingSource EnsureDatabase(ref string connection, string mappings)
		{
			if (connection != null && connection.IndexOf("|DataDirectory|") >= 0)
			{
				connection = connection.Replace("|DataDirectory|", ShadowTrackerService.ServiceDirectory);
			}
			mappings = Path.Combine(ShadowTrackerService.ServiceDirectory, mappings);
			MappingSource map = XmlMappingSource.FromUrl(mappings);

			// create one to test out
			L2SUnitOfWork unitOfWork = new L2SUnitOfWork(new DataContext(connection, map));
			if (!unitOfWork.CanConnect())
			{
				string answer = "n";

				if (this.In != null && this.In != TextReader.Null)
				{
					this.Out.Write("Specified database does not exist. Want to create it? (y/n): ");
					answer = this.In.ReadLine();
				}

				if (!StringComparer.OrdinalIgnoreCase.Equals(answer, "y"))
				{
					throw new Exception("Database specified in connection string does not exist.");
				}

				try
				{
					unitOfWork.InitializeDatabase();
					new CatalogRepository(unitOfWork).StoreVersion();
					unitOfWork.Save();
				}
				catch (Exception ex)
				{
					this.Error.WriteLine(ex);
					throw;
				}
			}
			return map;
		}

		#endregion Utility Methods
	}
}
