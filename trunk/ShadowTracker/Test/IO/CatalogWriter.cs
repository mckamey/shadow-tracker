using System;
using System.IO;

using JsonFx.Json;
using Shadow.Model;

namespace Shadow.IO
{
	public class CatalogWriter
	{
		#region Methods

		public static void Save(Catalog catalog, Stream stream)
		{
			using (CustomJsonWriter writer = new CustomJsonWriter(stream))
			{
#if DEBUG
				writer.PrettyPrint = true;
#endif
				writer.Write(catalog);
			}
		}

		public static void Save(CatalogDelta delta, Stream stream)
		{
			using (CustomJsonWriter writer = new CustomJsonWriter(stream))
			{
#if DEBUG
				writer.PrettyPrint = true;
#endif
				writer.Write(delta);
			}
		}

		#endregion Methods

		#region CustomJsonWriter

		private class CustomJsonWriter : JsonWriter
		{
			#region Init

			public CustomJsonWriter(Stream output)
				: base(output)
			{
			}

			#endregion Init

			#region Write Methods

			public override void Write(DateTime value)
			{
				base.Write(value.ToString("O"));
			}

			public override void Write(Enum value)
			{
				base.Write(Convert.ToInt64(value));
			}

			public override void Write(Guid value)
			{
				base.Write(value.ToString("N"));
			}

			public override void WriteBase64(byte[] value)
			{
				base.WriteBase64(value);
			}

			#endregion Write Methods
		}

		#endregion CustomJsonWriter
	}
}
