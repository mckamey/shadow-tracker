using System;
using System.Configuration;
using System.Data.Linq.Mapping;
using System.IO;
using System.Web;

using Microsoft.Practices.ServiceLocation;
using Shadow.Configuration;
using Shadow.Model;
using Shadow.Model.L2S;

namespace Shadow.Browser
{
	public class Global : System.Web.HttpApplication
	{
		protected void Application_Start(object sender, EventArgs e)
		{
			TrackerSettingsSection settings = TrackerSettingsSection.GetSettings();
			ServiceLocator.SetLocatorProvider(new SimpleServiceLocator(
				this.GetUnitOfWorkFactory(settings.SqlConnectionString, settings.SqlMapping)).ServiceLocatorProvider);
		}

		private Func<string, IUnitOfWork> GetUnitOfWorkFactory(string connection, string mappings)
		{
			MappingSource map = this.EnsureDatabase(ref connection, mappings);

			return delegate(string key)
			{
				return new L2SUnitOfWork(connection, map);
			};
		}

		private MappingSource EnsureDatabase(ref string connection, string mappings)
		{
			if (connection != null && connection.IndexOf("|DataDirectory|") >= 0)
			{
				connection = connection.Replace("|DataDirectory|", HttpRuntime.AppDomainAppPath+"App_data\\");
			}
			mappings = Path.Combine(HttpRuntime.AppDomainAppPath, mappings);
			MappingSource map = XmlMappingSource.FromUrl(mappings);

			return map;
		}
	}
}