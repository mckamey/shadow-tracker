using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("ShadowTracker.Service")]
[assembly: AssemblyDescription("Shadow Tracker Windows Service.")]
[assembly: AssemblyProduct("ShadowTracker")]
[assembly: AssemblyCopyright("Copyright ©2009. All rights reserved.")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: Guid("58cee771-1d54-4b9f-a108-ec8b853904f4")]
