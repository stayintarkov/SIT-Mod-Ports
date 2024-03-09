using DrakiaXYZ.VersionChecker;
using System.Reflection;
using System.Runtime.InteropServices;
using static ThatsLit.AssemblyInfo;
using ThatsLit;

[assembly: AssemblyTitle(Title)]
[assembly: AssemblyDescription(Description)]
[assembly: AssemblyConfiguration(Configuration)]
[assembly: AssemblyCompany(Company)]
[assembly: AssemblyProduct(Product)]
[assembly: AssemblyCopyright(Copyright)]
[assembly: AssemblyTrademark(Trademark)]
[assembly: AssemblyCulture(Culture)]
[assembly: ComVisible(false)]
[assembly: Guid("d08f8f91-95cf-4aa5-b7d8-f5d58f2feabb")]
[assembly: AssemblyVersion(ModVersion)]
[assembly: AssemblyFileVersion(ModVersion)]
[assembly: VersionChecker(AssemblyInfo.TarkovVersion)]
