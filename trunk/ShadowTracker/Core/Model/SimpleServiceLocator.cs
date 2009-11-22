using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Practices.ServiceLocation;

namespace Shadow.Model
{
	public class SimpleServiceLocator : ServiceLocatorImplBase
	{
		#region Fields

		private readonly Dictionary<Type, Delegate> FactoryMethods = new Dictionary<Type, Delegate>();

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="factories">must be of type Func&lt;string, T&gt;</param>
		public SimpleServiceLocator(params Delegate[] factoryMethods)
		{
			if (factoryMethods == null)
			{
				throw new ArgumentNullException("factories");
			}

			foreach (Delegate method in factoryMethods)
			{
				if (factoryMethods == null)
				{
					throw new ArgumentNullException("factories");
				}

				ParameterInfo[] parameters = method.Method.GetParameters();
				if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
				{
					throw new ArgumentException("factoryMethods", "Factory methods must be Func<string, T>");
				}

				this.FactoryMethods[method.Method.ReturnType] = method;
			}
		}

		#endregion Init

		#region Methods

		/// <summary>
		/// Satisfies the ServiceLocatorProvider delegate.
		/// </summary>
		/// <returns></returns>
		public IServiceLocator ServiceLocatorProvider()
		{
			return this;
		}

		#endregion Methods

		#region ServiceLocatorImplBase Members

		protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
		{
			yield return this.GetInstance(serviceType);
		}

		protected override object DoGetInstance(Type serviceType, string key)
		{
			if (!this.FactoryMethods.ContainsKey(serviceType))
			{
				throw new ActivationException("Must set Func<string, T> factory methods in SimpleServiceLocator constructor.");
			}

			return this.FactoryMethods[serviceType].DynamicInvoke(key);
		}

		#endregion ServiceLocatorImplBase Members
	}
}
