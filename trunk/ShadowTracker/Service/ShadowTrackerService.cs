using System;
using System.Configuration;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

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
				string connection = ConfigurationManager.ConnectionStrings["ShadowDB"].ConnectionString;
				UnitOfWorkFactory.SetFactoryMethod(this.GetUnitOfWorkFactory(connection, settings.SqlMapping));
				var filterCallback = FileUtility.CreateFileFilter(settings.FileFilters);

				var version = UnitOfWorkFactory.Create().Versions.OrderByDescending(v => v.ID).FirstOrDefault();

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

					FileUtility.SyncCatalog(
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

					this.Trackers[i] = new FileTracker();
					this.Trackers[i].TrackerError += this.OnError;
					this.Trackers[i].Start(CatalogRepository.EnsureCatalog(UnitOfWorkFactory.Create(), folders[i].Name, folders[i].Path), filterCallback);
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
			string connection = ConfigurationManager.ConnectionStrings["ShadowDB"].ConnectionString;
			this.EnsureDatabase(ref connection, settings.SqlMapping);
		}

		#endregion Service Events

		#region Utility Methods

		private Func<IUnitOfWork> GetUnitOfWorkFactory(string connection, string mappings)
		{
			MappingSource map = this.EnsureDatabase(ref connection, mappings);

			return delegate()
			{
				L2SUnitOfWork unitOfWork = new L2SUnitOfWork(connection, map);
				if (this.Out != TextWriter.Null)
				{
					unitOfWork.Log = this.Out;
				}
				return unitOfWork;
			};
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
			L2SUnitOfWork db = new L2SUnitOfWork(connection, map);
			if (!db.CanConnect())
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
					db.InitializeDatabase();
					db.Versions.Add(VersionHistory.Create());
					db.Save();
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
