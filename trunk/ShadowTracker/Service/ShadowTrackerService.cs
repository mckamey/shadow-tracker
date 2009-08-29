using System;
using System.Configuration;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.IO;
using System.ServiceProcess;

using Shadow.Agent;
using Shadow.Model;

namespace Shadow.Service
{
	public partial class ShadowTrackerService : ServiceBase
	{
		#region Fields

		private readonly FileTracker Tracker = new FileTracker();

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public ShadowTrackerService()
		{
			this.In = TextReader.Null;
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
				string watchFolder = ConfigurationManager.AppSettings["WatchFolder"];
				string pathFilter = ConfigurationManager.AppSettings["PathFilter"];
				string fileFilter = ConfigurationManager.AppSettings["FileFilter"] ?? "";
				var callback = FileUtility.CreateFileFilter(fileFilter.Split(',', '|'));

				string connection = ConfigurationManager.ConnectionStrings["ShadowDB"].ConnectionString;
				string mappings = ConfigurationManager.AppSettings["SqlMapping"];

				this.Out.WriteLine("ShadowTracker");
				this.Out.WriteLine(watchFolder);
				this.Out.WriteLine("__________________________");

				this.Out.WriteLine();
				this.Out.WriteLine("Connecting to database...");
				this.Out.WriteLine("__________________________");

				DataContext db = this.GetDataContext(connection, mappings);
				AnnotatedCatalog catalog = new AnnotatedCatalog(db);

				catalog.Log = this.Out;
				catalog.Error = this.Error;

				this.Out.WriteLine();
				this.Out.WriteLine("Beginning trickle update...");
				this.Out.WriteLine("__________________________");
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
						this.Out.WriteLine();
						this.Out.WriteLine("Elapsed trickle update: "+watch.Elapsed);
						this.Out.WriteLine("__________________________");
#endif
					});

				this.Out.WriteLine();
				this.Out.WriteLine("Tracking started...");
				this.Out.WriteLine("__________________________");
				this.Tracker.Start(catalog, watchFolder, pathFilter, callback);
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
			this.Tracker.Stop();

			this.Out.WriteLine();
			this.Out.WriteLine("Tracking stopped.");
		}

		#endregion Service Events

		#region Utility Methods

		private DataContext GetDataContext(string connection, string mappings)
		{
			if (connection != null && connection.IndexOf("|DataDirectory|") >= 0)
			{
				connection = connection.Replace("|DataDirectory|", Environment.CurrentDirectory);
			}

			MappingSource map = XmlMappingSource.FromUrl(mappings);
			DataContext db = new DataContext(connection, map);

			if (!db.DatabaseExists())
			{
				string answer = "n";

				if (this.In != TextReader.Null)
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
					db.CreateDatabase();
				}
				catch (Exception ex)
				{
					// TODO: handle this situation
					this.Error.WriteLine(ex.Message);
				}
			}

			return db;
		}

		#endregion Utility Methods
	}
}
