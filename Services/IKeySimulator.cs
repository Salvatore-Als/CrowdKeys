using CrowdKeys.Models;

namespace CrowdKeys.Services;

public interface IKeySimulator
{
    void PressKey(string key);
    void PressCombo(IReadOnlyList<string> keys);
    void KeyDown(IReadOnlyList<string> keys);
    void KeyUp(IReadOnlyList<string> keys);
    void ClickMouse(MouseButton button, int repeatCount = 1);
    void MouseDown(MouseButton button);
    void MouseUp(MouseButton button);
    void ScrollMouse(ScrollDirection direction, int amount);
    void MoveMouse(int deltaX, int deltaY);
}
