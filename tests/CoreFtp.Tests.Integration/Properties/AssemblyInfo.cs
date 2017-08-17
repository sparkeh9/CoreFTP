using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[ assembly: AssemblyConfiguration( "" ) ]
[ assembly: AssemblyCompany( "" ) ]
[ assembly: AssemblyProduct( "" ) ]
[ assembly: AssemblyTrademark( "" ) ]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.

[ assembly: ComVisible( false ) ]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[ assembly: Guid( "13781bec-50bc-476b-8b96-ce12f94a2c8f" ) ]
// Disable test parallelisation due to xUnit issue with async tests 
// Message: "There is no currently active test case"
[assembly: CollectionBehavior( DisableTestParallelization = true ) ]