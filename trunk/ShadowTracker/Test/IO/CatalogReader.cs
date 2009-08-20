using System;
using System.Collections.Generic;
using System.IO;

using JsonFx.Json;
using Shadow.Model;

namespace Shadow.IO
{
	public class CatalogReader
	{
		#region Methods

		public static ICatalogRepository Read(Stream stream)
		{
			return new JsonReader(stream).Deserialize<Catalog>();
		}

		#endregion Methods
	}
}
