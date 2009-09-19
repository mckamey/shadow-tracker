using System;
using System.Configuration;

using Shadow.Agent;

namespace Shadow.Configuration
{
	public sealed class TrackerSettingsSection : ConfigurationSection
	{
		#region Constants

		private static readonly char[] ConfigDelims = { ';', '|' };

		private const string DefaultSectionPath = "trackerSettings";

		private const string Key_SqlMapping = "sqlMapping";
		private const string Key_FileFilter = "fileFilter";
		private const string Key_TrickleRate = "trickleRate";
		private const string Key_WatchFolders = ""; // default so no key name

		#endregion Constants

		#region Fields

		private string[] fileFilters;

		#endregion Fields

		#region Properties

		[ConfigurationProperty(Key_SqlMapping, DefaultValue="", IsRequired=true)]
		public string SqlMapping
		{
			get
			{
				try
				{
					return (string)this[Key_SqlMapping];
				}
				catch
				{
					return "";
				}
			}
			set { this[Key_SqlMapping] = value; }
		}

		[ConfigurationProperty(Key_FileFilter, DefaultValue="", IsRequired=false)]
		public string FileFilter
		{
			get
			{
				try
				{
					return (string)this[Key_FileFilter];
				}
				catch
				{
					return "";
				}
			}
			set { this[Key_FileFilter] = value; }
		}

		[ConfigurationProperty(Key_TrickleRate, DefaultValue=FileUtility.DefaultTrickleRate, IsRequired=false)]
		public int TrickleRate
		{
			get
			{
				try
				{
					return (int)this[Key_TrickleRate];
				}
				catch
				{
					return FileUtility.DefaultTrickleRate;
				}
			}
			set { this[Key_TrickleRate] = value; }
		}

		[ConfigurationProperty(Key_WatchFolders, IsDefaultCollection=true)]
		public WatchFolderSettingsCollection WatchFolders
		{
			get
			{
				return (WatchFolderSettingsCollection)this[Key_WatchFolders];
			}
		}

		public string[] FileFilters
		{
			get
			{
				if (this.fileFilters == null)
				{
					this.fileFilters = this.FileFilter.Split(ConfigDelims, StringSplitOptions.RemoveEmptyEntries);
				}
				return this.fileFilters;
			}
		}

		#endregion Properties

		#region Methods

		public static TrackerSettingsSection GetSettings()
		{
			return TrackerSettingsSection.GetSettings(DefaultSectionPath);
		}

		public static TrackerSettingsSection GetSettings(string sectionPath)
		{
			TrackerSettingsSection config = null;
			try
			{
				config = (TrackerSettingsSection)ConfigurationManager.GetSection(sectionPath);
			}
			catch {}

			return config??new TrackerSettingsSection();
		}

		#endregion Methods
	}
}
