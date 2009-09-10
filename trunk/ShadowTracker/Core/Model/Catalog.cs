using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Shadow.Model
{
	/// <summary>
	/// Represents a single tree of catalog entries
	/// </summary>
	public class Catalog :
		INotifyPropertyChanging,
		INotifyPropertyChanged
	{
		#region EqualityComparer

		/// <summary>
		/// Tests equality of two Catalog objects on their paths
		/// </summary>
		public static readonly IEqualityComparer<Catalog> PathComparer = new PathEqualityComparer();

		private class PathEqualityComparer : IEqualityComparer<Catalog>
		{
			#region IEqualityComparer<T> Members

			bool IEqualityComparer<Catalog>.Equals(Catalog x, Catalog y)
			{
				if (x == null || y == null)
				{
					// return true if both null
					return EqualityComparer<Catalog>.Default.Equals(x, y);
				}

				return StringComparer.OrdinalIgnoreCase.Equals(x.Path, y.Path);
			}

			int IEqualityComparer<Catalog>.GetHashCode(Catalog obj)
			{
				return (obj == null) ?
					StringComparer.OrdinalIgnoreCase.GetHashCode(null) :
					StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Path);
			}

			#endregion IEqualityComparer<T> Members
		}

		#endregion EqualityComparer

		#region Fields

		private long id;
		private string name;
		private string path;
		private bool isIndexed;

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
		/// Gets and sets the name of the catalog
		/// </summary>
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
		/// Gets and sets the absolute path of the catalog root
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
		/// Gets and sets if catalog has been fully calculated
		/// </summary>
		public bool IsIndexed
		{
			get { return this.isIndexed; }
			set
			{
				if (StringComparer.OrdinalIgnoreCase.Equals(this.isIndexed, value))
				{
					return;
				}

				this.OnPropertyChanging("IsIndexed");
				this.isIndexed = value;
				this.OnPropertyChanged("IsIndexed");
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

		#region Object Overrides

		[DebuggerHidden]
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();

			builder.Append("{ Name = ");
			builder.Append(this.Name);
			builder.Append(", Path = ");
			builder.Append(this.Path);
			if (this.IsIndexed)
			{
				builder.Append(", IsIndexed");
			}
			builder.Append(" }");

			return builder.ToString();
		}

		#endregion Object Overrides
	}
}
