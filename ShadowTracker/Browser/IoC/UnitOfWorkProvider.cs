using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.IO;
using System.Web;

using IgnorantPersistence;
using IgnorantPersistence.L2S;
using Ninject.Activation;
using Shadow.Configuration;

namespace Shadow.Browser.IoC
{
	public class UnitOfWorkProvider : Provider<IUnitOfWork>
	{
		#region Fields

		private readonly string ConnectionString;
		private readonly MappingSource MappingSource;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public UnitOfWorkProvider()
		{
			TrackerSettingsSection settings = TrackerSettingsSection.GetSettings();

			this.ConnectionString = settings.SqlConnectionString;
			if (this.ConnectionString != null && this.ConnectionString.IndexOf("|DataDirectory|") >= 0)
			{
				this.ConnectionString = this.ConnectionString.Replace(
					"|DataDirectory|",
					HttpRuntime.AppDomainAppPath+"App_data\\");
			}

			string mappings = Path.Combine(HttpRuntime.AppDomainAppPath, settings.SqlMapping);
			this.MappingSource = XmlMappingSource.FromUrl(mappings);
		}

		#endregion Init

		#region Provider<IUnitOfWork> Members

		protected override IUnitOfWork CreateInstance(IContext context)
		{
			return new L2SUnitOfWork(new DataContext(this.ConnectionString, this.MappingSource));
		}

		#endregion Provider<IUnitOfWork> Members
	}
}
