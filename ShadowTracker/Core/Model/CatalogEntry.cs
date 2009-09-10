using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;

using Shadow.Model.L2S;

namespace Shadow.Model
{
	/// <summary>
	/// Represents a single catalog entry
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

				return StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name) &&
					StringComparer.OrdinalIgnoreCase.Equals(x.Parent, y.Parent);
			}

			int IEqualityComparer<CatalogEntry>.GetHashCode(CatalogEntry obj)
			{
				return (obj == null) ?
					StringComparer.OrdinalIgnoreCase.GetHashCode(null) :
					StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Parent+obj.Name);
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
					EqualityComparer<FileAttributes>.Default.Equals(x.Attributes, y.Attributes) &&
					(x.CreatedDate.Ticks == y.CreatedDate.Ticks) &&
					EqualityComparer<Int64>.Default.Equals(x.Length, y.Length) &&
					(x.ModifiedDate.Ticks == y.ModifiedDate.Ticks) &&
					StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name) &&
					StringComparer.OrdinalIgnoreCase.Equals(x.Parent, y.Parent) &&
					StringComparer.OrdinalIgnoreCase.Equals(x.Signature, y.Signature) &&
					(x.CatalogID == y.CatalogID);
			}

			int IEqualityComparer<CatalogEntry>.GetHashCode(CatalogEntry obj)
			{
				if (obj == null)
				{
					return StringComparer.OrdinalIgnoreCase.GetHashCode(null);
				}

				const int ShiftValue = -1521134295;
				int hashcode = 0x23f797e3;
				hashcode = (ShiftValue * hashcode) + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
				hashcode = (ShiftValue * hashcode) + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Parent);
				hashcode = (ShiftValue * hashcode) + EqualityComparer<FileAttributes>.Default.GetHashCode(obj.Attributes);
				hashcode = (ShiftValue * hashcode) + EqualityComparer<Int64>.Default.GetHashCode(obj.Length);
				hashcode = (ShiftValue * hashcode) + EqualityComparer<DateTime>.Default.GetHashCode(obj.CreatedDate);
				hashcode = (ShiftValue * hashcode) + EqualityComparer<DateTime>.Default.GetHashCode(obj.ModifiedDate);
				return ((ShiftValue * hashcode) + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Signature));
			}

			#endregion IEqualityComparer<T> Members
		}

		#endregion EqualityComparer

		#region Fields

		private DateTime? deletedDate;
		private long id;
		private string name;
		private string parent;
		private long length;
		private FileAttributes attributes;
		private DateTime createdDate;
		private DateTime modifiedDate;
		private string signature;

		private long catalogID;
		private EntityRef<Catalog> catalog = default(EntityRef<Catalog>);

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
		/// Gets and sets the file or directory name
		/// </summary>
		/// <remarks>
		/// Name and resulting path are case-insensitive.
		/// </remarks>
		public string Name
		{
			get { return this.name; }
			set
			{
				if (StringComparer.OrdinalIgnoreCase.Equals(this.name, value))
				{
					return;
				}

				this.OnPropertyChanging("Name");
				this.name = value;
				this.OnPropertyChanged("Name");
			}
		}

		/// <summary>
		/// Gets and sets the relative path of the parent directory
		/// </summary>
		/// <remarks>
		/// Parent and the resulting path are case-insensitive.
		/// </remarks>
		public string Parent
		{
			get { return this.parent; }
			set
			{
				if (String.IsNullOrEmpty(value))
				{
					value = "/";
				}
				else if (!value.EndsWith("/"))
				{
					value += '/';
				}

				if (StringComparer.OrdinalIgnoreCase.Equals(this.parent, value))
				{
					return;
				}

				this.OnPropertyChanging("Parent");
				this.parent = value;
				this.OnPropertyChanged("Parent");
			}
		}

		public string FullPath
		{
			get { return this.Parent+this.Name; }
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
		/// Gets and sets the ID of the owning catalog
		/// </summary>
		public long CatalogID
		{
			get { return this.catalogID; }
			set
			{
				if (this.catalogID == value)
				{
					return;
				}

				if (this.catalog.HasLoadedOrAssignedValue)
				{
					throw new ForeignKeyReferenceAlreadyHasValueException("Catalog already assigned to CatalogEntry.");
				}
				this.OnPropertyChanging("CatalogID");
				this.catalogID = value;
				this.OnPropertyChanged("CatalogID");
			}
		}

		/// <summary>
		/// Gets and sets the owning catalog
		/// </summary>
		internal Catalog Catalog
		{
			get { return this.catalog.Entity; }
			set
			{
				if (this.catalog.Entity == value)
				{
					return;
				}

				this.OnPropertyChanging("Catalog");
				this.catalog.Entity = value;
				this.OnPropertyChanged("Catalog");
			}
		}

		/// <summary>
		/// Gets if this node represents a directory
		/// </summary>
		public bool IsDirectory
		{
			get { return ((this.Attributes&FileAttributes.Directory) != 0); }
		}

		#endregion Properties

		#region IL2SSoftDeleteEntity Members

		public DateTime? DeletedDate
		{
			get { return this.deletedDate; }
			set
			{
				if (this.deletedDate == value)
				{
					return;
				}

				this.OnPropertyChanging("DeletedDate");
				this.deletedDate = value;
				this.OnPropertyChanged("DeletedDate");
			}
		}

		/// <summary>
		/// Updates the values of one entry with those of another
		/// </summary>
		/// <param name="entry"></param>
		public void CopyValuesFrom(IL2SSoftDeleteEntity entity)
		{
			if (entity == null)
			{
				return;
			}

			CatalogEntry that = entity as CatalogEntry;
			if (that == null)
			{
				this.Signature = entity.Signature;
				return;
			}

			//this.ID = that.ID;

			this.Attributes = that.Attributes;
			this.CreatedDate = that.CreatedDate;
			this.DeletedDate = that.DeletedDate;
			this.Length = that.Length;
			this.ModifiedDate = that.ModifiedDate;
			this.Parent = that.Parent;
			this.Name = that.Name;
			this.Signature = that.Signature;

			// TODO: evaluate whether this is needed
			if (that.CatalogID > 0)
			{
				this.CatalogID = that.CatalogID;
			}
		}

		#endregion IL2SSoftDeleteEntity Members

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

			builder.Append("{ Name = ");
			builder.Append(this.Name);
			builder.Append(", Parent = ");
			builder.Append(this.Parent);
			builder.Append(", Attributes = ");
			builder.Append(this.Attributes);
			if (this.CreatedDate >= CatalogEntry.SqlDateTimeMinValue)
			{
				builder.Append(", CreatedDate = ");
				builder.Append(this.CreatedDate);
			}
			if (this.ModifiedDate >= CatalogEntry.SqlDateTimeMinValue)
			{
				builder.Append(", ModifiedDate = ");
				builder.Append(this.ModifiedDate);
			}
			if (!String.IsNullOrEmpty(this.Signature))
			{
				builder.Append(", Signature = ");
				builder.Append(this.Signature);
			}
			if (!String.IsNullOrEmpty(this.Parent))
			{
				builder.Append(", Parent = ");
				builder.Append(this.Parent);
			}
			builder.Append(" }");

			return builder.ToString();
		}

		#endregion Object Overrides
	}
}
