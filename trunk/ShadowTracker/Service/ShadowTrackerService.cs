using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;

using IgnorantPersistence.L2S;
using Microsoft.Practices.ServiceLocation;
using Ninject;
using Shadow.Agent;
using Shadow.Configuration;
using Shadow.IO;
using Shadow.Model;
using Shadow.Service.IoC;
using Shadow.Tasks;

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
			// setup Ninject
			IKernel kernel = new StandardKernel(new IocModule(this));
			this.IoC = ServiceLocator.Current;

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
				Trace.Listeners.Clear();
				Trace.Listeners.Add(new TextWriterTraceListener(this.Out));

				TrackerSettingsSection settings = TrackerSettingsSection.GetSettings();

				var filterCallback = FileUtility.CreateFileFilter(settings.FileFilters);

				CatalogRepository repository = this.IoC.GetInstance<CatalogRepository>();
				var version = repository.GetVersionInfo();

				this.Out.WriteLine(this.GetType().Name);
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

				WatchFolderSettingsCollection folders = settings.WatchFolders;
				this.Trackers = new FileTracker[folders.Count];
				for (int i=0; i<folders.Count; i++)
				{
					this.Out.WriteLine();
					this.Out.WriteLine("Begin sync: "+folders[i].Name+" ("+folders[i].Path+")");

					Catalog catalog = repository.FindOrCreateCatalog(folders[i].Name, folders[i].Path);

					TaskEngine<TrackerTask> workQueue = new TaskEngine<TrackerTask>(new TrackerWorkQueue(
						this.IoC,
						catalog.ID,
						catalog.Path,
						filterCallback,
						TimeSpan.FromMilliseconds(settings.TrickleRate),
						settings.ThreadCount));

					this.Trackers[i] = new FileTracker(this.IoC, catalog.ID, catalog.Path, workQueue);
					this.Trackers[i].Start();
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

		public void InstallDatabase()
		{
			TrackerSettingsSection settings = TrackerSettingsSection.GetSettings();

			string connection = settings.SqlConnectionString;
			if (connection != null && connection.IndexOf("|DataDirectory|") >= 0)
			{
				connection = connection.Replace("|DataDirectory|", ShadowTrackerService.ServiceDirectory);
			}
			string mappings = Path.Combine(ShadowTrackerService.ServiceDirectory, settings.SqlMapping);
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
					new CatalogRepository(unitOfWork).StoreVersionInfo();
					unitOfWork.Save();
				}
				catch (Exception ex)
				{
					this.Error.WriteLine(ex);
					throw;
				}
			}
		}

		#endregion Service Events
	}
}
