using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SimpleWPFGame.UI;

public sealed class TooltipManager
{
    private static readonly Lazy<TooltipManager> _instance = new(() => new TooltipManager());
    public static TooltipManager Instance => _instance.Value;

    private Border? _tooltipBorder;
    private TextBlock? _tooltipText;
    private DispatcherTimer? _hideTimer;
    private UIElement? _parent;
    private Point _offset = new(30, 30);

    private TooltipManager() { }

    public void Initialize(UIElement parent)
    {
        if (_tooltipBorder != null) return;
        _parent = parent;

        _tooltipText = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 300,
            FontFamily = new FontFamily("Segoe UI"),
            LineHeight = 18
        };

        _tooltipBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(240, 15, 15, 30)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 180, 220)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 8, 10, 8),
            Child = _tooltipText,
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 12,
                ShadowDepth = 2,
                Opacity = 0.5,
                Color = Colors.Black
            }
        };

        Panel.SetZIndex(_tooltipBorder, 9999);

        if (parent is Panel panel)
            panel.Children.Add(_tooltipBorder);
        else if (parent is Window window && window.Content is Panel windowPanel)
            windowPanel.Children.Add(_tooltipBorder);
    }

    public void Show(string text)
    {
        if (_tooltipBorder == null || _tooltipText == null) return;

        _hideTimer?.Stop();
        _tooltipText.Text = text;
        _tooltipBorder.Visibility = Visibility.Visible;

        var pos = GetTooltipPosition();
        _tooltipBorder.Margin = new Thickness(pos.X, pos.Y, 0, 0);
        _tooltipBorder.HorizontalAlignment = HorizontalAlignment.Left;
        _tooltipBorder.VerticalAlignment = VerticalAlignment.Top;
    }

    public void Hide()
    {
        if (_tooltipBorder == null) return;

        _hideTimer?.Stop();
        _tooltipBorder.Visibility = Visibility.Collapsed;
    }

    public void HideDelayed(int milliseconds = 200)
    {
        if (_tooltipBorder == null) return;

        _hideTimer?.Stop();
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        _hideTimer.Tick += (s, e) =>
        {
            _hideTimer.Stop();
            Hide();
        };
        _hideTimer.Start();
    }

    public void UpdatePosition()
    {
        if (_tooltipBorder == null || _tooltipBorder.Visibility != Visibility.Visible) return;

        var pos = GetTooltipPosition();
        _tooltipBorder.Margin = new Thickness(pos.X, pos.Y, 0, 0);
    }

    private Point GetTooltipPosition()
    {
        var mousePos = Mouse.GetPosition(Application.Current.MainWindow);
        double x = mousePos.X + _offset.X;
        double y = mousePos.Y + _offset.Y;

        // Keep tooltip within window bounds
        if (_tooltipBorder != null)
        {
            double tipWidth = _tooltipBorder.ActualWidth > 0 ? _tooltipBorder.ActualWidth : 200;
            double tipHeight = _tooltipBorder.ActualHeight > 0 ? _tooltipBorder.ActualHeight : 60;
            double winW = Application.Current.MainWindow?.ActualWidth ?? 1920;
            double winH = Application.Current.MainWindow?.ActualHeight ?? 1080;

            if (x + tipWidth > winW - 10) x = mousePos.X - tipWidth - 10;
            if (y + tipHeight > winH - 10) y = mousePos.Y - tipHeight - 10;
            if (x < 0) x = 10;
            if (y < 0) y = 10;
        }

        return new Point(x, y);
    }
}
