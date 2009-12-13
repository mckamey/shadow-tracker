using System;

using CommonServiceLocator.NinjectAdapter;
using IgnorantPersistence;
using Microsoft.Practices.ServiceLocation;
using Ninject.Modules;

namespace Shadow.Service.IoC
{
	/// <summary>
	/// Establishes IoC bindings between DI interfaces and their implementations.
	/// </summary>
	public class IocModule : NinjectModule
	{
		#region Fields

		private readonly ShadowTrackerService TrackerService;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="service"></param>
		public IocModule(ShadowTrackerService service)
		{
			this.TrackerService = service;
		}

		#endregion Init

		#region NinjectModule Members

		public override void Load()
		{
			// UnitOfWork, provider will check data connection
			this.Bind<IUnitOfWork>().ToProvider(new UnitOfWorkProvider(this.TrackerService)).InTransientScope();

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
