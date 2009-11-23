using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.IO;

using IgnorantPersistence;
using IgnorantPersistence.L2S;
using Ninject.Activation;
using Shadow.Configuration;

namespace Shadow.Service.IoC
{
	public class UnitOfWorkProvider : Provider<IUnitOfWork>
	{
		#region Fields

		private readonly ShadowTrackerService TrackerService;
		private readonly string ConnectionString;
		private readonly MappingSource MappingSource;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public UnitOfWorkProvider(ShadowTrackerService service)
		{
			this.TrackerService = service;

			TrackerSettingsSection settings = TrackerSettingsSection.GetSettings();

			this.ConnectionString = settings.SqlConnectionString;
			if (this.ConnectionString != null && this.ConnectionString.IndexOf("|DataDirectory|") >= 0)
			{
				this.ConnectionString = this.ConnectionString.Replace("|DataDirectory|", ShadowTrackerService.ServiceDirectory);
			}
			string mappings = Path.Combine(ShadowTrackerService.ServiceDirectory, settings.SqlMapping);
			MappingSource map = XmlMappingSource.FromUrl(mappings);

			this.MappingSource = XmlMappingSource.FromUrl(mappings);
		}

		#endregion Init

		#region Provider<IUnitOfWork> Members

		protected override IUnitOfWork CreateInstance(IContext context)
		{
			L2SUnitOfWork unitOfWork = new L2SUnitOfWork(new DataContext(this.ConnectionString, this.MappingSource));
			unitOfWork.OnCommit += this.TrackerService.OnCommit;
			return unitOfWork;
		}

		#endregion Provider<IUnitOfWork> Members
	}
}
