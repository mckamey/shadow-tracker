using System;

using Ninject;
using Shadow.Browser.IoC;

namespace Shadow.Browser
{
	public class Global : System.Web.HttpApplication
	{
		public IKernel IoC
		{
			get;
			private set;
		}

		protected void Application_Start(object sender, EventArgs e)
		{
			this.IoC = new StandardKernel(new IocModule());
		}
	}
}