using System.Windows;
using System.Windows.Media;
using SimpleWPFGame.Settings;

namespace SimpleWPFGame.Game;

public abstract class GameObject
{
    public Vector Position { get; set; }
    public Vector Velocity { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsActive { get; set; } = true;
    public bool HasCollision { get; set; } = true;
    public Rect Bounds => new(Position.X, Position.Y, Width, Height);

    // Collision inset: 0 = exact bounds, positive = smaller collision box
    public double CollisionInset { get; set; } = 0;

    public Rect CollisionBounds => new(
        Position.X + CollisionInset,
        Position.Y + CollisionInset,
        Width - CollisionInset * 2,
        Height - CollisionInset * 2);

    public abstract void Update(double deltaTime);
    public abstract void Render(DrawingContext context);

    protected GameObject(double x, double y, double width, double height)
    {
        Position = new Vector(x, y);
        Width = width;
        Height = height;
    }

    public bool Intersects(GameObject other)
    {
        if (!HasCollision || !other.HasCollision) return false;
        return CollisionBounds.IntersectsWith(other.CollisionBounds);
    }

    public void RenderCollisionDebug(DrawingContext context)
    {
        if (!HasCollision || !GameSettings.Instance.ShowCollision) return;

        var cb = CollisionBounds;
        var pen = new Pen(Brushes.LimeGreen, 1.5) { DashStyle = DashStyles.Dash };
        context.DrawRectangle(null, pen, cb);

        // Corner ticks for visual clarity
        double tick = 6;
        var cornerPen = new Pen(Brushes.LimeGreen, 2);
        // Top-left
        context.DrawLine(cornerPen, new Point(cb.Left, cb.Top), new Point(cb.Left + tick, cb.Top));
        context.DrawLine(cornerPen, new Point(cb.Left, cb.Top), new Point(cb.Left, cb.Top + tick));
        // Top-right
        context.DrawLine(cornerPen, new Point(cb.Right, cb.Top), new Point(cb.Right - tick, cb.Top));
        context.DrawLine(cornerPen, new Point(cb.Right, cb.Top), new Point(cb.Right, cb.Top + tick));
        // Bottom-left
        context.DrawLine(cornerPen, new Point(cb.Left, cb.Bottom), new Point(cb.Left + tick, cb.Bottom));
        context.DrawLine(cornerPen, new Point(cb.Left, cb.Bottom), new Point(cb.Left, cb.Bottom - tick));
        // Bottom-right
        context.DrawLine(cornerPen, new Point(cb.Right, cb.Bottom), new Point(cb.Right - tick, cb.Bottom));
        context.DrawLine(cornerPen, new Point(cb.Right, cb.Bottom), new Point(cb.Right, cb.Bottom - tick));

        // Position label (top-left)
        var posLabel = new FormattedText(
            $"Pos [{(int)Position.X},{(int)Position.Y}]",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas"),
            9, Brushes.LimeGreen, 96);
        context.DrawText(posLabel, new Point(cb.Left, cb.Top - 26));

        // Size label (top-left, below position)
        var sizeLabel = new FormattedText(
            $"Col [{(int)cb.Width}x{(int)cb.Height}]",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Consolas"),
            9, Brushes.LimeGreen, 96);
        context.DrawText(sizeLabel, new Point(cb.Left, cb.Top - 14));
    }
}
