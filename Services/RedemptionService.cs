using CrowdKeys.Models;

namespace CrowdKeys.Services;

public class LogEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string Message { get; init; } = "";
    public bool IsMatch { get; init; }
    public string? CustomColor { get; init; }
    public string ForegroundColor => CustomColor ?? (IsMatch ? "#00c853" : "#6b6b7a");
}

public class RedemptionService
{
    private readonly IKeySimulator _keySimulator;
    private List<RedemptionBinding> _bindings = [];

    public event EventHandler<LogEntry>? LogAdded;

    public RedemptionService(IKeySimulator keySimulator) => _keySimulator = keySimulator;

    public void UpdateBindings(IEnumerable<RedemptionBinding> bindings) =>
        _bindings = bindings.ToList();

    public async Task OnRewardReceivedAsync(string rewardName)
    {
        var match = _bindings.FirstOrDefault(b =>
            b.IsEnabled &&
            b.RewardName.Equals(rewardName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            Log($"\"{rewardName}\" - pas de binding", isMatch: false);
            return;
        }

        if (match.Steps.Count == 0)
        {
            Log($"\"{rewardName}\" - binding sans étapes", isMatch: false);
            return;
        }

        Log($"\"{rewardName}\" → {match.Steps.Count} étape(s)", isMatch: true);

        foreach (var step in match.Steps)
        {
            switch (step.Type)
            {
                case Models.StepType.Pause:
                    if (step.DurationMs > 0)
                        await Task.Delay((int)step.DurationMs);

                    break;

                case Models.StepType.Key:
                    var repeat = Math.Max(1, (int)step.RepeatCount);
                    for (var i = 0; i < repeat; i++)
                    {
                        _keySimulator.PressCombo(step.Keys);
                        if (i < repeat - 1 && step.DelayBetweenMs > 0)
                            await Task.Delay((int)step.DelayBetweenMs);
                    }

                    break;

                case Models.StepType.MouseClick:
                    var clickRepeat = Math.Max(1, (int)step.RepeatCount);
                    for (var i = 0; i < clickRepeat; i++)
                    {
                        _keySimulator.ClickMouse(step.MouseButton);
                        if (i < clickRepeat - 1 && step.DelayBetweenMs > 0)
                            await Task.Delay((int)step.DelayBetweenMs);
                    }

                    break;

                case Models.StepType.MouseScroll:
                    _keySimulator.ScrollMouse(step.ScrollDirection, Math.Max(1, (int)step.ScrollAmount));
                    break;

                case Models.StepType.MouseMove:
                    if (step.MoveSpeedMs <= 0)
                    {
                        _keySimulator.MoveMouse((int)step.MoveX, (int)step.MoveY);
                    }
                    else
                    {
                        const int frameMs = 16;
                        var frames = Math.Max(1, (int)step.MoveSpeedMs / frameMs);
                        var dx = step.MoveX / frames;
                        var dy = step.MoveY / frames;
                        for (var i = 0; i < frames; i++)
                        {
                            _keySimulator.MoveMouse((int)Math.Round(dx), (int)Math.Round(dy));
                            if (i < frames - 1)
                                await Task.Delay(frameMs);
                        }
                    }
                    
                    break;
            }
        }
    }

    private void Log(string message, bool isMatch) =>
        LogAdded?.Invoke(this, new LogEntry { Message = message, IsMatch = isMatch });
}
