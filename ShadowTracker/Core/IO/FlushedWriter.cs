using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Security.Permissions;

namespace Shadow.IO
{
	/// <summary>
	/// Creates a thread-safe wrapper around the specified TextWriter which flushes after any writes.
	/// </summary>
	[Serializable]
	public class FlushedWriter : TextWriter
	{
		#region Fields

		private readonly TextWriter writer;

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		private FlushedWriter(TextWriter writer)
			: base(CultureInfo.InvariantCulture)
		{
			this.writer = writer;
		}

		[HostProtection(SecurityAction.LinkDemand, Synchronization=true)]
		public static TextWriter Create(TextWriter writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			if (writer is FlushedWriter)
			{
				return writer;
			}
			return new FlushedWriter(writer);
		}

		#endregion Init

		#region Properties

		public override Encoding Encoding
		{
			get { return this.writer.Encoding; }
		}

		public override IFormatProvider FormatProvider
		{
			get { return this.writer.FormatProvider; }
		}

		public override string NewLine
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			get { return this.writer.NewLine; }

			[MethodImpl(MethodImplOptions.Synchronized)]
			set { this.writer.NewLine = value; }
		}

		#endregion Properties

		#region Methods

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Close()
		{
			this.writer.Flush();
			this.writer.Close();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this.writer.Dispose();
			}
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Flush()
		{
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(bool value)
		{
			this.writer.Write(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(char[] buffer)
		{
			this.writer.Write(buffer);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(char value)
		{
			this.writer.Write(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(decimal value)
		{
			this.writer.Write(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(double value)
		{
			this.writer.Write(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(int value)
		{
			this.writer.Write(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(long value)
		{
			this.writer.Write(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(object value)
		{
			this.writer.Write(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(float value)
		{
			this.writer.Write(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(string value)
		{
			this.writer.Write(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(uint value)
		{
			this.writer.Write(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(ulong value)
		{
			this.writer.Write(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(string format, object[] arg)
		{
			this.writer.Write(format, arg);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(string format, object arg0)
		{
			this.writer.Write(format, arg0);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(string format, object arg0, object arg1)
		{
			this.writer.Write(format, arg0, arg1);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(char[] buffer, int index, int count)
		{
			this.writer.Write(buffer, index, count);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Write(string format, object arg0, object arg1, object arg2)
		{
			this.writer.Write(format, arg0, arg1, arg2);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine()
		{
			this.writer.WriteLine();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(char value)
		{
			this.writer.WriteLine(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(decimal value)
		{
			this.writer.WriteLine(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(char[] buffer)
		{
			this.writer.WriteLine(buffer);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(bool value)
		{
			this.writer.WriteLine(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(double value)
		{
			this.writer.WriteLine(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(int value)
		{
			this.writer.WriteLine(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(long value)
		{
			this.writer.WriteLine(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(object value)
		{
			this.writer.WriteLine(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(float value)
		{
			this.writer.WriteLine(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(string value)
		{
			this.writer.WriteLine(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(uint value)
		{
			this.writer.WriteLine(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(ulong value)
		{
			this.writer.WriteLine(value);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(string format, object arg0)
		{
			this.writer.WriteLine(format, arg0);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(string format, object[] arg)
		{
			this.writer.WriteLine(format, arg);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(string format, object arg0, object arg1)
		{
			this.writer.WriteLine(format, arg0, arg1);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(char[] buffer, int index, int count)
		{
			this.writer.WriteLine(buffer, index, count);
			this.writer.Flush();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void WriteLine(string format, object arg0, object arg1, object arg2)
		{
			this.writer.WriteLine(format, arg0, arg1, arg2);
			this.writer.Flush();
		}

		#endregion Methods
	}
}