using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("ShadowTracker.Core")]
[assembly: AssemblyDescription("Shadow Tracker Core Library.")]
[assembly: AssemblyProduct("ShadowTracker")]
[assembly: AssemblyCopyright("Copyright ©2009. All rights reserved.")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: ComVisible(false)]
[assembly: Guid("8195af82-1a28-44a8-9ed9-cb793ed53825")]

[assembly: InternalsVisibleTo("ShadowTracker.Core.Test")]