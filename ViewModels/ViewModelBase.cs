using CommunityToolkit.Mvvm.ComponentModel;
using CrowdKeys.Localization;

namespace CrowdKeys.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    private LocAccessor _loc = new();
    public LocAccessor Loc => _loc;

    protected ViewModelBase()
    {
        Localization.Loc.Instance.PropertyChanged += (_, _) =>
        {
            _loc = new LocAccessor();
            OnPropertyChanged(nameof(Loc));
        };
    }
}
