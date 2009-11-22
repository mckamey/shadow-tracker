using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;

using Shadow.Model.L2S;
using System.Linq.Expressions;

namespace Shadow.Model
{
	/// <summary>
	/// Represents a single catalog entry
	/// </summary>
	public class CatalogEntry :
		INotifyPropertyChanging,
		INotifyPropertyChanged,
		ISoftDeleteEntity
	{
		#region EqualityComparer

		/// <summary>
		/// Tests equality of two CatalogEntry objects by paths
		/// </summary>
		public static readonly IEqualityComparer<CatalogEntry> PathComparer = new PathEqualityComparer();

		/// <summary>
		/// Tests equality of two CatalogEntry objects by signatures
		/// </summary>
		public static readonly IEqualityComparer<CatalogEntry> SignatureComparer = new SignatureEqualityComparer();

		/// <summary>
		/// Tests equality of two CatalogEntry objects by non-identity/non-signature fields
		/// </summary>
		public static readonly IEqualityComparer<CatalogEntry> LiteValueComparer = new LiteValueEqualityComparer();

		/// <summary>
		/// Tests equality of two CatalogEntry objects by non-identity fields
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

		private class LiteValueEqualityComparer : IEqualityComparer<CatalogEntry>
		{
			#region Constants

			protected const int ShiftValue = -1521134295;

			#endregion Constants

			#region IEqualityComparer<T> Members

			public virtual bool Equals(CatalogEntry x, CatalogEntry y)
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
					(x.CatalogID == y.CatalogID);
			}

			public virtual int GetHashCode(CatalogEntry obj)
			{
				if (obj == null)
				{
					return EqualityComparer<CatalogEntry>.Default.GetHashCode(null);
				}

				int hashcode = 0x23f797e3;
				hashcode = (ShiftValue * hashcode) + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
				hashcode = (ShiftValue * hashcode) + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Parent);
				hashcode = (ShiftValue * hashcode) + EqualityComparer<FileAttributes>.Default.GetHashCode(obj.Attributes);
				hashcode = (ShiftValue * hashcode) + EqualityComparer<Int64>.Default.GetHashCode(obj.Length);
				hashcode = (ShiftValue * hashcode) + EqualityComparer<DateTime>.Default.GetHashCode(obj.CreatedDate);
				hashcode = (ShiftValue * hashcode) + EqualityComparer<DateTime>.Default.GetHashCode(obj.ModifiedDate);
				return hashcode;
			}

			#endregion IEqualityComparer<T> Members
		}

		private class ValueEqualityComparer : LiteValueEqualityComparer
		{
			#region IEqualityComparer<T> Members

			public override bool Equals(CatalogEntry x, CatalogEntry y)
			{
				if (x == null && y == null)
				{
					// return true if both null
					return true;
				}

				return
					base.Equals(x, y) &&
					StringComparer.OrdinalIgnoreCase.Equals(x.Signature, y.Signature);
			}

			public override int GetHashCode(CatalogEntry obj)
			{
				int hashcode = base.GetHashCode(obj);
				hashcode = ((ShiftValue * hashcode) + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Signature));
				return hashcode;
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
		[DefaultValue(0L)]
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
		[DefaultValue("")]
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
		[DefaultValue("")]
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

		[DefaultValue("")]
		public string FullPath
		{
			get { return this.Parent+this.Name; }
		}

		/// <summary>
		/// Gets and sets file size in bytes
		/// </summary>
		[DefaultValue(0L)]
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
		[DefaultValue(0)]
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
				value = VersionHistory.ScrubDate(value);
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
				value = VersionHistory.ScrubDate(value);
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
		[DefaultValue("")]
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
		/// Gets if the Signature field has been loaded.
		/// </summary>
		[DefaultValue(false)]
		public bool HasSignature
		{
			get { return this.Signature != null; }
		}

		/// <summary>
		/// Gets and sets the ID of the owning catalog
		/// </summary>
		[DefaultValue(0L)]
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
		[DefaultValue(null)]
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
		[DefaultValue(false)]
		public bool IsDirectory
		{
			get { return ((this.Attributes&FileAttributes.Directory) != 0); }
		}

		#endregion Properties

		#region IL2SSoftDeleteEntity Members

		[DefaultValue(null)]
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
		public void CopyValuesFrom(object entity)
		{
			CatalogEntry that = entity as CatalogEntry;
			if (that == null)
			{
				throw new ArgumentException("Can only copy from CatalogEntry");
			}

			//this.ID = that.ID;

			this.Attributes = that.Attributes;
			this.CreatedDate = that.CreatedDate;
			this.DeletedDate = that.DeletedDate;
			this.Length = that.Length;
			this.ModifiedDate = that.ModifiedDate;
			this.Parent = that.Parent;
			this.Name = that.Name;
			if (that.HasSignature)
			{
				this.Signature = that.Signature;
			}

			if (that.CatalogID > 0)
			{
				this.CatalogID = that.CatalogID;
			}
		}

		#endregion IL2SSoftDeleteEntity Members

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
			if (this.CreatedDate >= VersionHistory.SqlDateTimeMinValue)
			{
				builder.Append(", CreatedDate = ");
				builder.Append(this.CreatedDate);
			}
			if (this.ModifiedDate >= VersionHistory.SqlDateTimeMinValue)
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
