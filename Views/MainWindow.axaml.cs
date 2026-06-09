using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CrowdKeys.Models;
using CrowdKeys.ViewModels;

namespace CrowdKeys.Views;

public partial class MainWindow : Window
{
    private KeyStep? _draggedStep;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(InputElement.TextInputEvent, FilterNumericInput, RoutingStrategies.Tunnel);
    }

    private void FilterNumericInput(object? sender, TextInputEventArgs e)
    {
        if (e.Source is not TextBox textBox) 
            return;
        
        var nud = textBox.FindAncestorOfType<NumericUpDown>();
        if (nud == null || e.Text == null) 
            return;
        
        foreach (var c in e.Text)
        {
            if (char.IsDigit(c)) 
                continue;
            
            if (c == '-' && nud.Minimum < 0) 
                continue;
            
            e.Handled = true;
            return;
        }
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
