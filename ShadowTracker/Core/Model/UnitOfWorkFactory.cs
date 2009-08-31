using System;

namespace Shadow.Model
{
	public static class UnitOfWorkFactory
	{
		#region Fields

		private static Func<IUnitOfWork> FactoryMethod;

		#endregion Fields

		#region Factory Method

		public static IUnitOfWork Create()
		{
			if (UnitOfWorkFactory.FactoryMethod == null)
			{
				throw new InvalidOperationException("Must use UnitOfWorkFactory.SetFactoryMethod to configure persistence.");
			}

			return UnitOfWorkFactory.FactoryMethod();
		}

		#endregion Factory Method

		#region Configuration

		public static void SetFactoryMethod(Func<IUnitOfWork> factory)
		{
			UnitOfWorkFactory.FactoryMethod = factory;
		}

		#endregion Configuration
	}
}
