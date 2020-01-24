namespace Afas.BazelDotnet.Nuget
{
  public interface IPackageSourceResolver
  {
    string Resolve(string packageId);
  }
}