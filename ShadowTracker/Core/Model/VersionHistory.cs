using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Shadow.Model
{
	public class VersionHistory :
		INotifyPropertyChanging,
		INotifyPropertyChanged
	{
		#region Constants

		public static readonly Version AssemblyVersion;
		internal static readonly DateTime SqlDateTimeMinValue = new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		#endregion Constants

		#region Fields

		private long id;
		private string label;
		private DateTime updatedDate;

		#endregion Fields

		#region Init

		static VersionHistory()
		{
			AssemblyName assemblyName = typeof(VersionHistory).Assembly.GetName();
			VersionHistory.AssemblyVersion = assemblyName.Version;
		}

		public static VersionHistory Create()
		{
			VersionHistory version = new VersionHistory();
			version.label = VersionHistory.AssemblyVersion.ToString();
			version.UpdatedDate = DateTime.UtcNow;

			return version;
		}

		#endregion Init

		#region Properties

		/// <summary>
		/// Gets and sets the primary key
		/// </summary>
		public long ID
		{
			get { return this.id; }
			set
			{
				if (this.id == value)
				{
					return;
				}

				this.OnPropertyChanging("ID");
				this.id = value;
				this.OnPropertyChanged("ID");
			}
		}

		/// <summary>
		/// Gets and sets the Version label
		/// </summary>
		public string Label
		{
			get { return this.label; }
			set
			{
				if (this.label == value)
				{
					return;
				}

				this.OnPropertyChanging("Label");
				this.label = value;
				this.OnPropertyChanged("Label");
			}
		}

		/// <summary>
		/// Gets and sets modified date
		/// </summary>
		public DateTime UpdatedDate
		{
			get { return this.updatedDate; }
			set
			{
				value = VersionHistory.ScrubDate(value);
				if (this.updatedDate.Ticks == value.Ticks)
				{
					return;
				}

				this.OnPropertyChanging("UpdatedDate");
				this.updatedDate = value;
				this.OnPropertyChanged("UpdatedDate");
			}
		}

		#endregion Properties

		#region INotifyPropertyChanging Members

		public event PropertyChangingEventHandler PropertyChanging;

		protected virtual void OnPropertyChanging(string propertyName)
		{
			if ((this.PropertyChanging != null))
			{
				this.PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
			}
		}

		#endregion INotifyPropertyChanging Members

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if ((this.PropertyChanged != null))
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion INotifyPropertyChanged Members

		#region Utility Methods

		/// <summary>
		/// Cleanses dates for round-trip storage equality.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <remarks>
		/// Converts to UTC and only stores accurately to the second.
		/// Disregards dates before 1753-01-01T00:00:00z which is DateTime.MinValue for SQL DateTime
		/// </remarks>
		internal static DateTime ScrubDate(DateTime value)
		{
			if (value.Kind == DateTimeKind.Local)
			{
				value = value.ToUniversalTime();
			}

			if (value < VersionHistory.SqlDateTimeMinValue)
			{
				return VersionHistory.SqlDateTimeMinValue;
			}

			return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, DateTimeKind.Utc);
		}

		#endregion Utility Methods

		#region Object Overrides

		[DebuggerHidden]
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();

			builder.Append("{ ID = ");
			builder.Append(this.ID);
			builder.Append(", Label = ");
			builder.Append(this.Label);
			if (this.UpdatedDate >= VersionHistory.SqlDateTimeMinValue)
			{
				builder.Append(", UpdatedDate = ");
				builder.Append(this.UpdatedDate);
			}
			builder.Append(" }");

			return builder.ToString();
		}

		#endregion Object Overrides
	}
}
