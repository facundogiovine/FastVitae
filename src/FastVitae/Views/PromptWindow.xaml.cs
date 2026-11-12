using System.Windows;

namespace FastVitae.Views;

// One-line text prompt
public partial class PromptWindow : Window
{
    public PromptWindow()
    {
        InitializeComponent();
    }

    // Null when cancelled or empty
    public static string? Ask(Window owner, string title, string label, string initial = "")
    {
        var window = new PromptWindow { Owner = owner, Title = title };
        window.LabelText.Text = label;
        window.AnswerBox.Text = initial;
        window.Loaded += (s, e) =>
        {
            window.AnswerBox.Focus();
            window.AnswerBox.SelectAll();
        };

        if (window.ShowDialog() != true)
            return null;
        string answer = window.AnswerBox.Text.Trim();
        return answer == "" ? null : answer;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
