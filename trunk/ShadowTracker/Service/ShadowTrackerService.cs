using System;
using System.Configuration;
using System.Data.Linq.Mapping;
using System.IO;
using System.ServiceProcess;

using Shadow.Agent;
using Shadow.Model;
using Shadow.Model.L2S;

namespace Shadow.Service
{
	public partial class ShadowTrackerService : ServiceBase
	{
		#region Fields

		private static readonly char[] ConfigDelims = { ';', '|' };
		private FileTracker[] Trackers;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public ShadowTrackerService()
		{
			this.Out = this.Error = TextWriter.Null;

			this.InitializeComponent();
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
			get;
			set;
		}

		public TextWriter Error
		{
			get;
			set;
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
				string watchFolder = ConfigurationManager.AppSettings["WatchFolder"] ?? String.Empty;
				string[] folders = watchFolder.Split(ConfigDelims, StringSplitOptions.RemoveEmptyEntries);

				string fileFilter = ConfigurationManager.AppSettings["FileFilter"] ?? String.Empty;
				var filterCallback = FileUtility.CreateFileFilter(fileFilter.Split(ConfigDelims, StringSplitOptions.RemoveEmptyEntries));

				string connection = ConfigurationManager.ConnectionStrings["ShadowDB"].ConnectionString;
				string mappings = ConfigurationManager.AppSettings["SqlMapping"];

				this.Out.WriteLine("ShadowTracker");
				this.Out.WriteLine(watchFolder);
				this.Out.WriteLine(fileFilter);
				this.Out.WriteLine("__________________________");

				this.Out.WriteLine();
				this.Out.WriteLine("Connecting to database...");
				this.Out.WriteLine("__________________________");

				UnitOfWorkFactory.SetFactoryMethod(this.GetUnitOfWorkFactory(connection, mappings));

				this.Out.WriteLine();
				this.Out.WriteLine("Beginning trickle update...");
				this.Out.WriteLine("__________________________");

				var watch = System.Diagnostics.Stopwatch.StartNew();

				this.Trackers = new FileTracker[folders.Length];
				for (int i=0; i<folders.Length; i++)
				{
					FileUtility.SyncCatalog(
						folders[i],
						filterCallback,
						FileUtility.DefaultTrickleRate,
						delegate(string syncFolder)
						{
							this.Out.WriteLine();
							this.Out.WriteLine(syncFolder+Environment.NewLine+"Elapsed trickle update: "+watch.Elapsed);
							this.Out.WriteLine("__________________________");
						});

					this.Trackers[i] = new FileTracker();
					this.Trackers[i].Start(CatalogRepository.EnsureCatalog(UnitOfWorkFactory.Create(), folders[i]), filterCallback);
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

		#endregion Service Events

		#region Utility Methods

		private Func<IUnitOfWork> GetUnitOfWorkFactory(string connection, string mappings)
		{
			if (connection != null && connection.IndexOf("|DataDirectory|") >= 0)
			{
				connection = connection.Replace("|DataDirectory|", Environment.CurrentDirectory);
			}
			MappingSource map = XmlMappingSource.FromUrl(mappings);

			// create one to test out
			L2SUnitOfWork db = new L2SUnitOfWork(connection, map);
			if (!db.CanConnect())
			{
				string answer = "n";

				if (this.In != null)
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
				}
				catch (Exception ex)
				{
					// TODO: handle this situation
					this.Error.WriteLine(ex.Message);
				}
			}

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

		#endregion Utility Methods
	}
}
