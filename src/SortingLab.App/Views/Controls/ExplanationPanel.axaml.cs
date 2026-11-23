using Avalonia;
using Avalonia.Controls;

namespace SortingLab.App.Views.Controls;

public partial class ExplanationPanel : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ExplanationPanel, string>(nameof(Title), "Демонстрация не запущена");

    public static readonly StyledProperty<string> ExplanationProperty =
        AvaloniaProperty.Register<ExplanationPanel, string>(
            nameof(Explanation),
            "Нажмите «Запустить», чтобы начать пошаговую демонстрацию.");

    public static readonly StyledProperty<string> KindProperty =
        AvaloniaProperty.Register<ExplanationPanel, string>(nameof(Kind), "—");

    public ExplanationPanel()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Explanation
    {
        get => GetValue(ExplanationProperty);
        set => SetValue(ExplanationProperty, value);
    }

    public string Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }
}
