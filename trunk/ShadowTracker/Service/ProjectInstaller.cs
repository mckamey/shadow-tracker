﻿using System;
using System.ComponentModel;
using System.Configuration.Install;

namespace Shadow.Service
{
	[RunInstaller(true)]
	public partial class ProjectInstaller : Installer
	{
		public ProjectInstaller()
		{
			InitializeComponent();
		}
	}
}
