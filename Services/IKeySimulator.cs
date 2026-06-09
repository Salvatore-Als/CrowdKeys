using TwitchKeyboard.Models;

namespace TwitchKeyboard.Services;

public interface IKeySimulator
{
    void PressKey(string key);
    void PressCombo(IReadOnlyList<string> keys);
    void ClickMouse(MouseButton button, int repeatCount = 1);
    void ScrollMouse(ScrollDirection direction, int amount);
    void MoveMouse(int deltaX, int deltaY);
}
