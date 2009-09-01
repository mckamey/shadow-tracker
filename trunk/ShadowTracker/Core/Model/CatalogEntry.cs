using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

using Shadow.Model.L2S;

namespace Shadow.Model
{
	/// <summary>
	/// Represents a single catalog data node
	/// </summary>
	public class CatalogEntry :
		INotifyPropertyChanging,
		INotifyPropertyChanged,
		IL2SSoftDeleteEntity
	{
		#region Constants

		private static readonly DateTime SqlDateTimeMinValue = new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		#endregion Constants

		#region EqualityComparer

		/// <summary>
		/// Tests equality of two CatalogEntry objects on their paths
		/// </summary>
		public static readonly IEqualityComparer<CatalogEntry> PathComparer = new PathEqualityComparer();

		/// <summary>
		/// Tests equality of two CatalogEntry objects on their signatures
		/// </summary>
		public static readonly IEqualityComparer<CatalogEntry> SignatureComparer = new SignatureEqualityComparer();

		/// <summary>
		/// Tests equality of two CatalogEntry objects on their non-identity fields
		/// </summary>
		public static readonly IEqualityComparer<CatalogEntry> ValueComparer = new ValueEqualityComparer();

		private class PathEqualityComparer : IEqualityComparer<CatalogEntry>
		{
			#region IEqualityComparer<T> Members

			bool IEqualityComparer<CatalogEntry>.Equals(CatalogEntry x, CatalogEntry y)
			{
				if (x == null || y == null)
				{
					// return true if both null
					return EqualityComparer<CatalogEntry>.Default.Equals(x, y);
				}

				return StringComparer.OrdinalIgnoreCase.Equals(x.Path, y.Path);
			}

			int IEqualityComparer<CatalogEntry>.GetHashCode(CatalogEntry obj)
			{
				return (obj == null) ?
					StringComparer.OrdinalIgnoreCase.GetHashCode(null) :
					StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Path);
			}

			#endregion IEqualityComparer<T> Members
		}

		private class SignatureEqualityComparer : IEqualityComparer<CatalogEntry>
		{
			#region IEqualityComparer<T> Members

			bool IEqualityComparer<CatalogEntry>.Equals(CatalogEntry x, CatalogEntry y)
			{
				if (x == null || y == null)
				{
					// return true if both null
					return EqualityComparer<CatalogEntry>.Default.Equals(x, y);
				}

				return StringComparer.OrdinalIgnoreCase.Equals(x, y);
			}

			int IEqualityComparer<CatalogEntry>.GetHashCode(CatalogEntry obj)
			{
				return (obj == null) ?
					StringComparer.OrdinalIgnoreCase.GetHashCode(null) :
					StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Signature);
			}

			#endregion IEqualityComparer<T> Members
		}

		private class ValueEqualityComparer : IEqualityComparer<CatalogEntry>
		{
			#region IEqualityComparer<T> Members

			bool IEqualityComparer<CatalogEntry>.Equals(CatalogEntry x, CatalogEntry y)
			{
				if (x == null || y == null)
				{
					// return true if both null
					return EqualityComparer<CatalogEntry>.Default.Equals(x, y);
				}

				return
					StringComparer.OrdinalIgnoreCase.Equals(x.Path, y.Path) &&
					EqualityComparer<FileAttributes>.Default.Equals(x.Attributes, y.Attributes) &&
					EqualityComparer<Int64>.Default.Equals(x.Length, y.Length) &&
					(x.CreatedDate.Ticks == y.CreatedDate.Ticks) &&
					(x.ModifiedDate.Ticks == y.ModifiedDate.Ticks) &&
					StringComparer.OrdinalIgnoreCase.Equals(x.Signature, y.Signature);
			}

			int IEqualityComparer<CatalogEntry>.GetHashCode(CatalogEntry obj)
			{
				if (obj == null)
				{
					return StringComparer.OrdinalIgnoreCase.GetHashCode(null);
				}

				int hashcode = 0x23f797e3;
				hashcode = (-1521134295 * hashcode) + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Path);
				hashcode = (-1521134295 * hashcode) + EqualityComparer<FileAttributes>.Default.GetHashCode(obj.Attributes);
				hashcode = (-1521134295 * hashcode) + EqualityComparer<Int64>.Default.GetHashCode(obj.Length);
				hashcode = (-1521134295 * hashcode) + EqualityComparer<DateTime>.Default.GetHashCode(obj.CreatedDate);
				hashcode = (-1521134295 * hashcode) + EqualityComparer<DateTime>.Default.GetHashCode(obj.ModifiedDate);
				return ((-1521134295 * hashcode) + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Signature));
			}

			#endregion IEqualityComparer<T> Members
		}

		#endregion EqualityComparer

		#region Fields

		private bool isDeleted;
		private long id;
		private string path;
		private long length;
		private FileAttributes attributes;
		private DateTime createdDate;
		private DateTime modifiedDate;
		private string signature;

		#endregion Fields

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
		/// Gets and sets the relative path to the data
		/// </summary>
		public string Path
		{
			get { return this.path; }
			set
			{
				if (StringComparer.OrdinalIgnoreCase.Equals(this.path, value))
				{
					return;
				}

				this.OnPropertyChanging("Path");
				this.path = value;
				this.OnPropertyChanged("Path");
			}
		}

		/// <summary>
		/// Gets and sets file size in bytes
		/// </summary>
		public long Length
		{
			get
			{
				if (this.IsDirectory)
				{
					return 0L;
				}
				return this.length;
			}
			set
			{
				if (this.length == value)
				{
					return;
				}

				this.OnPropertyChanging("Length");
				this.length = value;
				this.OnPropertyChanged("Length");
			}
		}

		/// <summary>
		/// Gets and sets file attributes
		/// </summary>
		public FileAttributes Attributes
		{
			get { return this.attributes; }
			set
			{
				if (this.attributes == value)
				{
					return;
				}

				this.OnPropertyChanging("Attributes");
				this.attributes = value;
				this.OnPropertyChanged("Attributes");
			}
		}

		/// <summary>
		/// Gets and sets created date
		/// </summary>
		public DateTime CreatedDate
		{
			get { return this.createdDate; }
			set
			{
				value = CatalogEntry.ScrubDate(value);
				if (this.createdDate.Ticks == value.Ticks)
				{
					return;
				}

				this.OnPropertyChanging("CreatedDate");
				this.createdDate = value;
				this.OnPropertyChanged("CreatedDate");
			}
		}

		/// <summary>
		/// Gets if this node has an associated CreatedDate
		/// </summary>
		public bool HasCreatedDate
		{
			get { return this.CreatedDate >= CatalogEntry.SqlDateTimeMinValue; }
		}

		/// <summary>
		/// Gets and sets modified date
		/// </summary>
		public DateTime ModifiedDate
		{
			get { return this.modifiedDate; }
			set
			{
				value = CatalogEntry.ScrubDate(value);
				if (this.modifiedDate.Ticks == value.Ticks)
				{
					return;
				}

				this.OnPropertyChanging("ModifiedDate");
				this.modifiedDate = value;
				this.OnPropertyChanged("ModifiedDate");
			}
		}

		/// <summary>
		/// Gets if this node has an associated ModifiedDate
		/// </summary>
		public bool HasModifiedDate
		{
			get { return this.ModifiedDate >= CatalogEntry.SqlDateTimeMinValue; }
		}

		/// <summary>
		/// Gets and sets the hash signature of the file
		/// </summary>
		public string Signature
		{
			get
			{
				if (this.IsDirectory)
				{
					return String.Empty;
				}
				return this.signature;
			}
			set
			{
				if (StringComparer.OrdinalIgnoreCase.Equals(this.signature, value))
				{
					return;
				}

				this.OnPropertyChanging("Signature");
				this.signature = value;
				this.OnPropertyChanged("Signature");
			}
		}

		/// <summary>
		/// Gets if this node has an associated hash signature
		/// </summary>
		public bool HasSignature
		{
			get { return !String.IsNullOrEmpty(this.Signature); }
		}

		/// <summary>
		/// Gets if this node represents a directory
		/// </summary>
		public bool IsDirectory
		{
			get { return ((this.Attributes&FileAttributes.Directory) != 0); }
		}

		#endregion Properties

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
		private static DateTime ScrubDate(DateTime value)
		{
			if (value.Kind == DateTimeKind.Local)
			{
				value = value.ToUniversalTime();
			}

			if (value < CatalogEntry.SqlDateTimeMinValue)
			{
				return CatalogEntry.SqlDateTimeMinValue;
			}

			return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, DateTimeKind.Utc);
		}

		#endregion Utility Methods

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

		#region Object Overrides

		[DebuggerHidden]
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();

			builder.Append("{ Path = ");
			builder.Append(this.Path);
			builder.Append(", Attributes = ");
			builder.Append(this.Attributes);
			if (this.HasCreatedDate)
			{
				builder.Append(", CreatedDate = ");
				builder.Append(this.CreatedDate);
			}
			if (this.HasModifiedDate)
			{
				builder.Append(", ModifiedDate = ");
				builder.Append(this.ModifiedDate);
			}
			if (this.HasSignature)
			{
				builder.Append(", Signature = ");
				builder.Append(this.Signature);
			}
			builder.Append(" }");

			return builder.ToString();
		}

		#endregion Object Overrides

		#region IL2SSoftDeleteEntity Members

		public bool IsDeleted
		{
			get { return this.isDeleted; }
			set
			{
				if (this.isDeleted == value)
				{
					return;
				}

				this.OnPropertyChanging("IsDeleted");
				this.isDeleted = value;
				this.OnPropertyChanged("IsDeleted");
			}
		}

		#endregion IL2SSoftDeleteEntity Members
	}
}
