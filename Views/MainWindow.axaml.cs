using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace CrowdKeys.Views;

public partial class MainWindow : Window
{
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
}
