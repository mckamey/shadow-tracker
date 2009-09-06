using System;
using System.Configuration;

namespace Shadow.Configuration
{
	public sealed class WatchFolderSettings : ConfigurationSection
	{
		#region Constants

		private const string Key_Name = "name";
		private const string Key_Path = "path";

		#endregion Constants

		#region Properties

		[ConfigurationProperty(Key_Name, DefaultValue="", IsRequired=true, Options=ConfigurationPropertyOptions.IsKey|ConfigurationPropertyOptions.IsRequired)]
		public string Name
		{
			get
			{
				try
				{
					return (string)this[Key_Name];
				}
				catch
				{
					return "";
				}
			}
			set { this[Key_Name] = value; }
		}

		[ConfigurationProperty(Key_Path, DefaultValue="", IsRequired=true)]
		public string Path
		{
			get
			{
				try
				{
					return (string)this[Key_Path];
				}
				catch
				{
					return "";
				}
			}
			set { this[Key_Path] = value; }
		}

		#endregion Properties
	}
}
