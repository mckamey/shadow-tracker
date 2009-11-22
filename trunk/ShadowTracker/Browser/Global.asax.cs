using System;
using System.Configuration;
using System.Data.Linq;
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
			SimpleServiceLocator locator = new SimpleServiceLocator(this.GetFactoryMethod());

			ServiceLocator.SetLocatorProvider(locator.ServiceLocatorProvider);
		}

		private Func<string, IUnitOfWork> GetFactoryMethod()
		{
			TrackerSettingsSection settings = TrackerSettingsSection.GetSettings();

			string connection = settings.SqlConnectionString;
			if (connection != null && connection.IndexOf("|DataDirectory|") >= 0)
			{
				connection = connection.Replace("|DataDirectory|", HttpRuntime.AppDomainAppPath+"App_data\\");
			}

			string mappings = Path.Combine(HttpRuntime.AppDomainAppPath, settings.SqlMapping);
			MappingSource map = XmlMappingSource.FromUrl(mappings);

			return delegate(string key)
			{
				return new L2SUnitOfWork(new DataContext(connection, map));
			};
		}
	}
}