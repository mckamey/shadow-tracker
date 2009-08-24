using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Shadow.Model
{
	/// <summary>
	/// Represents a single catalog data node
	/// </summary>
	public class CatalogEntry
	{
		#region EqualityComparer

		public static readonly IEqualityComparer<CatalogEntry> PathComparer = new PathEqualityComparer();
		public static readonly IEqualityComparer<CatalogEntry> SignatureComparer = new SignatureEqualityComparer();

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

		#endregion EqualityComparer

		#region Properties

		/// <summary>
		/// Gets and sets the primary key
		/// </summary>
		public long ID { get; set; }

		/// <summary>
		/// Gets and sets the relative path to the data
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// Gets and sets file attributes
		/// </summary>
		public FileAttributes Attributes { get; set; }

		/// <summary>
		/// Gets and sets created date
		/// </summary>
		public DateTime CreatedDate { get; set; }

		/// <summary>
		/// Gets if this node has an associated CreatedDate
		/// </summary>
		public bool HasCreatedDate
		{
			get { return (this.CreatedDate != default(DateTime)); }
		}

		/// <summary>
		/// Gets and sets modified date
		/// </summary>
		public DateTime ModifiedDate { get; set; }

		/// <summary>
		/// Gets if this node has an associated ModifiedDate
		/// </summary>
		public bool HasModifiedDate
		{
			get { return (this.ModifiedDate != default(DateTime)); }
		}

		/// <summary>
		/// Gets and sets the hash signature of the file
		/// </summary>
		public string Signature { get; set; }

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

		#region Object Overrides

		[DebuggerHidden]
		public override bool Equals(object obj)
		{
			var that = obj as CatalogEntry;

			return (that != null) &&
				EqualityComparer<String>.Default.Equals(this.Path, that.Path) &&
				EqualityComparer<FileAttributes>.Default.Equals(this.Attributes, that.Attributes) &&
				EqualityComparer<DateTime>.Default.Equals(this.CreatedDate, that.CreatedDate) &&
				EqualityComparer<DateTime>.Default.Equals(this.ModifiedDate, that.ModifiedDate) &&
				StringComparer.OrdinalIgnoreCase.Equals(this.Signature, that.Signature);
		}

		[DebuggerHidden]
		public override int GetHashCode()
		{
			int hashcode = 0x23f797e3;
			hashcode = (-1521134295 * hashcode) + EqualityComparer<String>.Default.GetHashCode(this.Path);
			hashcode = (-1521134295 * hashcode) + EqualityComparer<FileAttributes>.Default.GetHashCode(this.Attributes);
			hashcode = (-1521134295 * hashcode) + EqualityComparer<DateTime>.Default.GetHashCode(this.CreatedDate);
			hashcode = (-1521134295 * hashcode) + EqualityComparer<DateTime>.Default.GetHashCode(this.ModifiedDate);
			return ((-1521134295 * hashcode) + StringComparer.OrdinalIgnoreCase.GetHashCode(this.Signature));
		}

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
	}
}
