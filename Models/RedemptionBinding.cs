using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CrowdKeys.Models;

public partial class RedemptionBinding : ObservableObject
{
    [ObservableProperty] private string _rewardName = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDescription))]
    private string _description = "";

    public bool HasDescription => !string.IsNullOrEmpty(Description);
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private bool _isOrphaned;

    public ObservableCollection<KeyStep> Steps { get; set; } = [];
}
