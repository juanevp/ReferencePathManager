using System.Reflection;
using System.Runtime.InteropServices;
using ReferencePathManager.Properties;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Reference Path Manager")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Reference Path Manager")]
[assembly: AssemblyCopyright("Juan Vidal Pich")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyInformationalVersion(AssemblyInformation.Version)]
[assembly: AssemblyVersion(AssemblyInformation.Version)]
[assembly: AssemblyFileVersion(AssemblyInformation.Version)]

namespace ReferencePathManager.Properties
{
    internal static class AssemblyInformation
    {
        internal const string Version = "1.1.0";
    }
}
