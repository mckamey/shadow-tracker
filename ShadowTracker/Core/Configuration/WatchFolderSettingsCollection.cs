using System;
using System.Configuration;

namespace Shadow.Configuration
{
	[ConfigurationCollection(typeof(WatchFolderSettings))]
	public sealed class WatchFolderSettingsCollection : ConfigurationElementCollection
	{
		#region Fields

		private static readonly ConfigurationPropertyCollection properties = new ConfigurationPropertyCollection();

		#endregion Fields

		#region Init

		/// <summary>
		/// Ctor
		/// </summary>
		public WatchFolderSettingsCollection()
			: base(StringComparer.OrdinalIgnoreCase)
		{
		}

		#endregion Init

		#region Method

		public void Add(WatchFolderSettings settings)
		{
			this.BaseAdd(settings);
		}

		protected override void BaseAdd(int index, ConfigurationElement element)
		{
			if (index < 0)
			{
				base.BaseAdd(element, false);
			}
			else
			{
				base.BaseAdd(index, element);
			}
		}

		public void Clear()
		{
			base.BaseClear();
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new WatchFolderSettings();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((WatchFolderSettings)element).Name;
		}

		public int IndexOf(WatchFolderSettings settings)
		{
			return base.BaseIndexOf(settings);
		}

		public void Remove(WatchFolderSettings settings)
		{
			if (base.BaseIndexOf(settings) < 0)
			{
				return;
			}

			base.BaseRemove(settings.Name);
		}

		public void Remove(string name)
		{
			base.BaseRemove(name);
		}

		public void RemoveAt(int index)
		{
			base.BaseRemoveAt(index);
		}

		#endregion Method

		#region Properties

		public new WatchFolderSettings this[string name]
		{
			get
			{
				return (WatchFolderSettings)base.BaseGet(name);
			}
		}

		public WatchFolderSettings this[int index]
		{
			get
			{
				return (WatchFolderSettings)base.BaseGet(index);
			}
			set
			{
				if (base.BaseGet(index) != null)
				{
					base.BaseRemoveAt(index);
				}
				this.BaseAdd(index, value);
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get { return WatchFolderSettingsCollection.properties; }
		}

		#endregion Properties
	}
}
