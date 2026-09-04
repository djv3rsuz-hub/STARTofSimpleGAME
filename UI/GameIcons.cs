using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace SimpleWPFGame.UI;

public static class GameIcons
{
    public static class Stats
    {
        public const string Symbol = "\u2694";       // ⚔ crossed swords
        public const string Color1 = "#FFFF4757";     // Red
        public const string Color2 = "#FFFF6B81";     // Light red
        public const string Glow = "#FFFF0000";
    }

    public class NewGame
    {
        public const string Symbol = "\u2726";        // ✦ star
        public const string Color1 = "#FF00D4FF";     // Cyan
        public const string Color2 = "#FF00FF88";     // Green
        public const string Glow = "#FF00D4FF";
    }

    public class Save
    {
        public const string Symbol = "\u2B50";        // ⭐ star
        public const string Color1 = "#FFFFD700";     // Gold
        public const string Color2 = "#FFFFA500";     // Orange
        public const string Glow = "#FFFFD700";
    }

    public class Load
    {
        public const string Symbol = "\u21BB";        // ↻ reload
        public const string Color1 = "#FF9B59B6";     // Purple
        public const string Color2 = "#FF8E44AD";     // Dark purple
        public const string Glow = "#FF9B59B6";
    }

    public class Options
    {
        public const string Symbol = "\u2699";        // ⚙ gear
        public const string Color1 = "#FF888888";     // Gray
        public const string Color2 = "#FFAAAAAA";     // Light gray
        public const string Glow = "#FF888888";
    }

    public class Exit
    {
        public const string Symbol = "\u2716";        // ✖ cross
        public const string Color1 = "#FFFF6B6B";     // Red
        public const string Color2 = "#FFFF4757";     // Dark red
        public const string Glow = "#FFFF4444";
    }

    public class Character
    {
        public const string Symbol = "\u265F";        // ♟ character
        public const string Color1 = "#FF00D4FF";     // Cyan
        public const string Color2 = "#FF0099CC";     // Dark cyan
        public const string Glow = "#FF00D4FF";
    }

    public class Dashboard
    {
        public const string Symbol = "\u25A3";        // ▣ dashboard
        public const string Color1 = "#FF00FF88";     // Green
        public const string Color2 = "#FF00CC6A";     // Dark green
        public const string Glow = "#FF00FF88";
    }

    public static Button CreateIconButton(string symbol, string label, string color1, string color2, string glowColor, int size = 60, RoutedEventHandler? click = null)
    {
        var iconText = new TextBlock
        {
            Text = symbol,
            FontSize = size * 0.45,
            FontWeight = FontWeights.Bold,
            Foreground = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString(color1),
                (Color)ColorConverter.ConvertFromString(color2),
                45),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(glowColor),
                BlurRadius = 12,
                ShadowDepth = 0,
                Opacity = 0.8
            }
        };

        var labelText = new TextBlock
        {
            Text = label.ToUpper(),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color1)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children = { iconText, labelText }
        };

        var border = new Border
        {
            Width = size,
            Height = size + 18,
            Margin = new Thickness(0, 3, 0, 3),
            CornerRadius = new CornerRadius(8),
            Background = new LinearGradientBrush(
                Color.FromArgb(40, 0, 0, 0),
                Color.FromArgb(80, 0, 0, 0),
                90),
            BorderBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString(color1),
                (Color)ColorConverter.ConvertFromString(color2),
                45),
            BorderThickness = new Thickness(1.5),
            Child = stack,
            Cursor = Cursors.Hand,
            Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(glowColor),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.3
            }
        };

        var button = new Button
        {
            Template = CreateIconTemplate(border),
            Width = size + 4,
            Height = size + 22,
            Margin = new Thickness(0, 1, 0, 1),
            Cursor = Cursors.Hand
        };

        if (click != null)
            button.Click += click;

        return button;
    }

    private static ControlTemplate CreateIconTemplate(Border innerBorder)
    {
        var template = new ControlTemplate(typeof(Button));

        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "border";
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
        border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = RelativeSource.TemplatedParent });
        border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = RelativeSource.TemplatedParent });
        border.SetBinding(Border.CornerRadiusProperty, new System.Windows.Data.Binding("CornerRadius") { RelativeSource = RelativeSource.TemplatedParent });

        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(contentPresenter);

        template.VisualTree = border;

        var triggerMouseOver = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
        triggerMouseOver.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2), "border"));

        var triggerPressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
        triggerPressed.Setters.Add(new Setter(Border.OpacityProperty, 0.8, "border"));

        template.Triggers.Add(triggerMouseOver);
        template.Triggers.Add(triggerPressed);

        return template;
    }

    public static Border CreateIcon(string symbol, string color1, string color2, string glowColor, double size = 40)
    {
        var iconText = new TextBlock
        {
            Text = symbol,
            FontSize = size * 0.5,
            FontWeight = FontWeights.Bold,
            Foreground = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString(color1),
                (Color)ColorConverter.ConvertFromString(color2),
                45),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(glowColor),
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.9
            }
        };

        return new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size * 0.2),
            Background = new LinearGradientBrush(
                Color.FromArgb(30, 0, 0, 0),
                Color.FromArgb(60, 0, 0, 0),
                45),
            BorderBrush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString(color1),
                (Color)ColorConverter.ConvertFromString(color2),
                45),
            BorderThickness = new Thickness(1.5),
            Child = iconText,
            Effect = new DropShadowEffect
            {
                Color = (Color)ColorConverter.ConvertFromString(glowColor),
                BlurRadius = 14,
                ShadowDepth = 0,
                Opacity = 0.4
            }
        };
    }
}
