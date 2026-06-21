using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CrowdKeys.Models;
using CrowdKeys.ViewModels;

namespace CrowdKeys.Views;

public partial class StepEditorPanel : UserControl
{
    private KeyStep? _draggedStep;

    public StepEditorPanel()
    {
        InitializeComponent();
    }

    private void KeyModeNormal_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton tb && tb.DataContext is KeyStep step)
            step.IsHeld = false;
    }

    private async void DragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control ctrl || ctrl.DataContext is not KeyStep step)
            return;

        e.Handled = true;
        _draggedStep = step;

        var item = DataTransferItem.Create(DataFormat.Text, "drag");
        var transfer = new DataTransfer();
        transfer.Add(item);
        await DragDrop.DoDragDropAsync(e, transfer, DragDropEffects.Move);

        _draggedStep = null;
    }

    private void StepBorder_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = _draggedStep != null ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void StepBorder_Drop(object? sender, DragEventArgs e)
    {
        if (sender is not Control ctrl || ctrl.DataContext is not KeyStep target)
            return;

        if (_draggedStep == null || _draggedStep == target)
            return;

        (DataContext as MainWindowViewModel)?.MoveStep(_draggedStep, target);
        e.Handled = true;
    }
}
