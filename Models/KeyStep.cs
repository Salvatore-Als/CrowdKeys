using CommunityToolkit.Mvvm.ComponentModel;

namespace CrowdKeys.Models;

public enum StepType { Key, Pause, MouseClick, MouseScroll, MouseMove, ScreenEffect }
public enum MouseButton { Left, Right, Middle }
public enum ScrollDirection { Up, Down }

public partial class KeyStep : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsKeyStep))]
    [NotifyPropertyChangedFor(nameof(IsPauseStep))]
    [NotifyPropertyChangedFor(nameof(IsMouseClickStep))]
    [NotifyPropertyChangedFor(nameof(IsMouseScrollStep))]
    [NotifyPropertyChangedFor(nameof(IsMouseMoveStep))]
    [NotifyPropertyChangedFor(nameof(IsScreenEffectStep))]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private StepType _type = StepType.Key;

    // Key step fields
    [ObservableProperty][NotifyPropertyChangedFor(nameof(Keys))][NotifyPropertyChangedFor(nameof(DisplayText))] private bool _useCtrl;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(Keys))][NotifyPropertyChangedFor(nameof(DisplayText))] private bool _useShift;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(Keys))][NotifyPropertyChangedFor(nameof(DisplayText))] private bool _useAlt;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(Keys))][NotifyPropertyChangedFor(nameof(DisplayText))] private bool _useWin;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(Keys))][NotifyPropertyChangedFor(nameof(DisplayText))] private string _mainKey = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    [NotifyPropertyChangedFor(nameof(CanConfigureDelay))]
    private decimal _repeatCount = 1;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(DisplayText))] private decimal _delayBetweenMs = 0;

    public bool CanConfigureDelay => RepeatCount > 1;

    // Pause step fields
    [ObservableProperty][NotifyPropertyChangedFor(nameof(DisplayText))] private decimal _durationMs = 500;

    // Mouse click fields
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLeftClick))]
    [NotifyPropertyChangedFor(nameof(IsRightClick))]
    [NotifyPropertyChangedFor(nameof(IsMiddleClick))]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private MouseButton _mouseButton = MouseButton.Left;

    // Mouse scroll fields
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScrollUp))]
    [NotifyPropertyChangedFor(nameof(IsScrollDown))]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private ScrollDirection _scrollDirection = ScrollDirection.Up;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(DisplayText))] private decimal _scrollAmount = 3;

    // Screen effect fields
    [ObservableProperty][NotifyPropertyChangedFor(nameof(DisplayText))] private ScreenEffectType _effectType = ScreenEffectType.Mirror;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(DisplayText))] private decimal _effectDurationMs = 5000;

    // Mouse move fields
    [ObservableProperty][NotifyPropertyChangedFor(nameof(DisplayText))] private decimal _moveX = 0;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(DisplayText))] private decimal _moveY = 0;
    [ObservableProperty] private decimal _moveSpeedMs = 0;

    public bool IsKeyStep          => Type == StepType.Key;
    public bool IsPauseStep        => Type == StepType.Pause;
    public bool IsMouseClickStep   => Type == StepType.MouseClick;
    public bool IsMouseScrollStep  => Type == StepType.MouseScroll;
    public bool IsMouseMoveStep    => Type == StepType.MouseMove;
    public bool IsScreenEffectStep => Type == StepType.ScreenEffect;

    public bool IsLeftClick
    {
        get => MouseButton == MouseButton.Left;
        set { 
            if (value) 
                MouseButton = MouseButton.Left; 
            else 
            { 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(IsRightClick)); 
                OnPropertyChanged(nameof(IsMiddleClick)); 
            } 
        }
    }
    public bool IsRightClick
    {
        get => MouseButton == MouseButton.Right;
        set { 
            if (value) 
                MouseButton = MouseButton.Right; 
            else 
            { 
                OnPropertyChanged(nameof(IsLeftClick)); 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(IsMiddleClick)); 
            } 
        }
    }
    public bool IsMiddleClick
    {
        get => MouseButton == MouseButton.Middle;
        set { 
            if (value) 
                MouseButton = MouseButton.Middle; 
            else 
            { 
                OnPropertyChanged(nameof(IsLeftClick)); 
                OnPropertyChanged(nameof(IsRightClick)); 
                OnPropertyChanged(); 
            } 
        }
    }

    public bool IsScrollUp
    {
        get => ScrollDirection == ScrollDirection.Up;
        set { 
            if (value) 
                ScrollDirection = ScrollDirection.Up;
            else 
            { 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(IsScrollDown)); 
            } 
        }
    }
    public bool IsScrollDown
    {
        get => ScrollDirection == ScrollDirection.Down;
        set { 
            if (value) 
                ScrollDirection = ScrollDirection.Down; 
            else 
            { 
                OnPropertyChanged(nameof(IsScrollUp)); 
                OnPropertyChanged(); 
            } 
        }
    }

    public IReadOnlyList<string> Keys
    {
        get
        {
            var keys = new List<string>();
            if (UseCtrl)  
                keys.Add("CTRL");
            
            if (UseShift) 
                keys.Add("SHIFT");
            
            if (UseAlt)  
                keys.Add("ALT");
            
            if (UseWin)   
                keys.Add("WIN");
            
            if (!string.IsNullOrWhiteSpace(MainKey))
                keys.Add(MainKey);
            
            return keys;
        }
    }

    public string DisplayText => Type switch
    {
        StepType.Pause        => $"Pause {(int)DurationMs}ms",
        StepType.MouseClick   => $"Clic {MouseButton} ×{(int)RepeatCount}",
        StepType.MouseScroll  => $"Scroll {ScrollDirection} ×{(int)ScrollAmount}",
        StepType.MouseMove    => $"Move ({(int)MoveX},{(int)MoveY}px)",
        StepType.ScreenEffect => $"Effet {EffectType switch {
            ScreenEffectType.Mirror             => "Miroir",
            ScreenEffectType.ShuffleQuadrants   => "Quad x2",
            ScreenEffectType.ShuffleQuadrants4  => "Quad x4",
            ScreenEffectType.Blur               => "Blur",
            ScreenEffectType.Drunk              => "Shaking",
            _                                   => EffectType.ToString()
        }} • {(int)EffectDurationMs}ms",
        _ => BuildKeyText()
    };

    private string BuildKeyText()
    {
        var parts = new List<string>();
        if (UseCtrl)  
            parts.Add("CTRL");
        
        if (UseShift) 
            parts.Add("SHIFT");
        
        if (UseAlt)   
            parts.Add("ALT");
        
        if (UseWin)   
            parts.Add("WIN");
        
        if (!string.IsNullOrWhiteSpace(MainKey)) 
            parts.Add(MainKey);
        
        var label = parts.Count > 0 ? string.Join("+", parts) : "(vide)";
        if (RepeatCount > 1) 
            label += $" ×{(int)RepeatCount}";
        
        return label;
    }
}
