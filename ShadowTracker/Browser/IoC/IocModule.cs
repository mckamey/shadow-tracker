using System;

using CommonServiceLocator.NinjectAdapter;
using IgnorantPersistence;
using Microsoft.Practices.ServiceLocation;
using Ninject.Modules;

namespace Shadow.Browser.IoC
{
	/// <summary>
	/// Establishes IoC bindings between DI interfaces and their implementations.
	/// </summary>
	public class IocModule : NinjectModule
	{
		#region NinjectModule Members

		public override void Load()
		{
			// ShadowTracker UnitOfWork, provider will check data connection
			this.Bind<IUnitOfWork>().ToProvider(new UnitOfWorkProvider()).InTransientScope();

			this.RegisterServiceLocator();
		}

		#endregion NinjectModule Members

		#region Methods

		private void RegisterServiceLocator()
		{
			NinjectServiceLocator serviceLocator = new NinjectServiceLocator(this.Kernel);

			ServiceLocator.SetLocatorProvider(() => serviceLocator);
		}

		#endregion Methods
	}
}
