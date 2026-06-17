namespace CrowdKeys.Localization;

public sealed class LocAccessor
{
    public string this[string key] => Loc.Instance[key];
}
