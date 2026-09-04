using System.Windows;
using System.Windows.Media;

namespace SimpleWPFGame.Combat;

public class DamageNumber
{
    public Vector Position { get; set; }
    public string Text { get; set; }
    public Color Color { get; set; }
    public double Life { get; set; }
    public double MaxLife { get; set; }
    public double Size { get; set; }
    public double VelocityY { get; set; }
    public bool IsCrit { get; set; }
    public bool IsHeal { get; set; }
    public bool IsPerfect { get; set; }
    public bool IsMiss { get; set; }
    public double Scale { get; set; } = 1.0;
    public double Opacity => Math.Max(0, Life / MaxLife);

    private static readonly Random _rng = new();

    public DamageNumber(Vector pos, double damage, bool crit, bool heal = false, bool perfect = false, bool miss = false)
    {
        Position = pos + new Vector(_rng.Next(-10, 10), _rng.Next(-15, 5));
        IsCrit = crit;
        IsHeal = heal;
        IsPerfect = perfect;
        IsMiss = miss;

        if (miss)
        {
            Text = "MISS";
            Color = Color.FromRgb(150, 150, 150);
            Size = 14;
            MaxLife = 0.8;
        }
        else if (heal)
        {
            Text = $"+{damage:F0}";
            Color = Color.FromRgb(50, 255, 50);
            Size = crit ? 20 : 16;
            MaxLife = 1.0;
        }
        else if (perfect)
        {
            Text = $"PERFECT {damage:F0}";
            Color = Color.FromRgb(255, 255, 50);
            Size = 22;
            MaxLife = 1.2;
            Scale = 1.3;
        }
        else if (crit)
        {
            Text = $"{damage:F0}!";
            Color = Color.FromRgb(255, 50, 50);
            Size = 22;
            MaxLife = 1.0;
            Scale = 1.2;
        }
        else
        {
            Text = $"{damage:F0}";
            Color = Color.FromRgb(255, 255, 255);
            Size = 16;
            MaxLife = 0.8;
        }

        Life = MaxLife;
        VelocityY = -80 - _rng.Next(0, 40);
    }

    public void Update(double deltaTime)
    {
        Life -= deltaTime;
        Position = new Vector(Position.X, Position.Y + VelocityY * deltaTime);
        VelocityY += 120 * deltaTime;

        if (Life < MaxLife * 0.3)
            Scale = Math.Max(0.5, Scale - deltaTime * 2);
    }

    public bool IsDead => Life <= 0;
}

public class DamageNumberSystem
{
    private static DamageNumberSystem? _instance;
    public static DamageNumberSystem Instance => _instance ??= new DamageNumberSystem();

    private readonly List<DamageNumber> _numbers = new();
    public int ActiveCount => _numbers.Count;

    private DamageNumberSystem() { }

    public void Add(Vector pos, double damage, bool crit, bool heal = false, bool perfect = false, bool miss = false)
    {
        _numbers.Add(new DamageNumber(pos, damage, crit, heal, perfect, miss));
    }

    public void Update(double deltaTime)
    {
        for (int i = _numbers.Count - 1; i >= 0; i--)
        {
            _numbers[i].Update(deltaTime);
            if (_numbers[i].IsDead)
                _numbers.RemoveAt(i);
        }
    }

    public void Render(DrawingContext context)
    {
        double dpi = 96.0 / 96.0;

        foreach (var num in _numbers)
        {
            var color = Color.FromArgb((byte)(num.Opacity * 255), num.Color.R, num.Color.G, num.Color.B);
            var brush = new SolidColorBrush(color);

            var ft = new FormattedText(
                num.Text,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                num.Size * num.Scale,
                brush,
                dpi);

            double x = num.Position.X - ft.Width / 2;
            double y = num.Position.Y - ft.Height / 2;

            if (num.IsCrit || num.IsPerfect)
            {
                var shadowBrush = new SolidColorBrush(Color.FromArgb((byte)(num.Opacity * 180), 0, 0, 0));
                var shadowFt = new FormattedText(
                    num.Text,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    num.Size * num.Scale,
                    shadowBrush,
                    dpi);
                context.DrawText(shadowFt, new Point(x + 2, y + 2));
            }

            context.DrawText(ft, new Point(x, y));
        }
    }
}
