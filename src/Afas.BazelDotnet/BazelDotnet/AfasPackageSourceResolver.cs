using System;
using System.Linq;
using Afas.BazelDotnet.Nuget;

namespace Afas.BazelDotnet
{
  /// <summary>
  ///   LocalPackages do not indicate their source. Therefore we have to resolve them here.
  /// </summary>
  internal class AfasPackageSourceResolver : IPackageSourceResolver
  {
    public string Resolve(string packageId)
    {
      //if(packageId.Split(".").Contains("afas", StringComparer.OrdinalIgnoreCase))
      //todo implement generic lookup behaviour

      if(packageId.StartsWith("afas.online.", StringComparison.OrdinalIgnoreCase))
      {
        return "https://tfsai.afasgroep.nl/tfs/Next/_packaging/aol/nuget/v3/flat2";
      }

      if(packageId.EndsWith(".by.afas", StringComparison.OrdinalIgnoreCase))
      {
        return "https://nuget.afasgroep.nl/api/v2/package";
      }

      if(packageId.StartsWith("afas.", StringComparison.OrdinalIgnoreCase))
      {
        return "https://tfsai.afasgroep.nl/tfs/Next/_packaging/next/nuget/v3/flat2";
      }

      return null;
    }
  }
}