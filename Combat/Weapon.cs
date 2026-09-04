using System.Windows;
using System.Windows.Media;
using SimpleWPFGame.Config;

namespace SimpleWPFGame.Combat;

public abstract class Weapon
{
    public string Name { get; set; } = "Weapon";
    public WeaponType Type { get; set; }
    public DamageType DamageType { get; set; } = DamageType.Physical;
    public double BaseDamage { get; set; } = 10;
    public double AttackSpeed { get; set; } = 1.0;
    public double Range { get; set; } = 60;
    public double CritMultiplier { get; set; } = 1.5;
    public int ComboLength { get; set; } = 3;
    public double KnockbackForce { get; set; } = 100;

    public abstract FrameData GetFrameData(int comboIndex);
    public abstract Hitbox[] GetHitboxes(int comboIndex, Vector ownerPos, double ownerRotation);
    public abstract void RenderSlash(DrawingContext context, Vector pos, double rotation, int frame, int comboIndex);
}

public enum WeaponType
{
    Sword,
    Axe,
    Spear,
    Dagger,
    Staff,
    Bow,
    Shield
}

public class Sword : Weapon
{
    private static readonly Color _slashColor = Color.FromRgb(200, 220, 255);
    private static readonly Color _slashTrailColor = Color.FromArgb(120, 100, 150, 255);
    private static readonly Color _hitSparkColor = Color.FromRgb(255, 255, 200);

    public Sword()
    {
        Name = "Iron Sword";
        Type = WeaponType.Sword;
        DamageType = DamageType.Physical;
        BaseDamage = 15;
        AttackSpeed = 1.0;
        Range = 55;
        CritMultiplier = 1.5;
        ComboLength = 3;
        KnockbackForce = 120;
    }

    public override FrameData GetFrameData(int comboIndex)
    {
        return comboIndex switch
        {
            0 => FrameData.SwordSlash,
            1 => new FrameData
            {
                StartupFrames = 3,
                ActiveFrames = 5,
                RecoveryFrames = 10,
                ParryWindowStart = 1,
                ParryWindowFrames = 7,
                PerfectParryWindowFrames = 3,
                DodgeIframeFrames = 12,
                PerfectDodgeWindowFrames = 4
            },
            2 => FrameData.SwordHeavy,
            _ => FrameData.SwordSlash
        };
    }

    public override Hitbox[] GetHitboxes(int comboIndex, Vector ownerPos, double ownerRotation)
    {
        var fd = GetFrameData(comboIndex);
        double range = Range;
        double arcAngle = comboIndex == 2 ? 120 : 90;
        double halfArc = arcAngle / 2.0;

        var hitboxes = new List<Hitbox>();
        int segments = comboIndex == 2 ? 3 : 2;

        for (int i = 0; i < segments; i++)
        {
            double t = (double)i / segments;
            double angle = ownerRotation + (-halfArc + arcAngle * t) * Math.PI / 180;
            double offsetX = Math.Cos(angle) * range * 0.6;
            double offsetY = Math.Sin(angle) * range * 0.6;

            var rect = new Rect(
                ownerPos.X + offsetX - range * 0.3,
                ownerPos.Y + offsetY - range * 0.3,
                range * 0.6, range * 0.6);

            hitboxes.Add(new Hitbox(
                rect,
                fd.StartupFrames,
                fd.StartupFrames + fd.ActiveFrames - 1,
                KnockbackForce)
            {
                DamageType = DamageType
            });
        }

        return hitboxes.ToArray();
    }

    public override void RenderSlash(DrawingContext context, Vector pos, double rotation, int frame, int comboIndex)
    {
        var fd = GetFrameData(comboIndex);
        if (frame < fd.StartupFrames || frame >= fd.StartupFrames + fd.ActiveFrames)
            return;

        int activeFrame = frame - fd.StartupFrames;
        double progress = (double)activeFrame / fd.ActiveFrames;
        double arcAngle = comboIndex == 2 ? 140 : 100;
        double halfArc = arcAngle / 2.0;
        double range = Range * (0.8 + progress * 0.4);

        int alpha = (int)(255 * (1 - progress));
        var slashColor = Color.FromArgb((byte)alpha, _slashColor.R, _slashColor.G, _slashColor.B);
        var trailColor = Color.FromArgb((byte)(alpha * 0.5), _slashTrailColor.R, _slashTrailColor.G, _slashTrailColor.B);

        var slashBrush = new SolidColorBrush(slashColor);
        var trailBrush = new SolidColorBrush(trailColor);
        var pen = new Pen(slashBrush, 3);

        int arcPoints = comboIndex == 2 ? 16 : 10;
        var points = new PointCollection();

        for (int i = 0; i <= arcPoints; i++)
        {
            double t = (double)i / arcPoints;
            double angle = rotation + (-halfArc + arcAngle * t) * Math.PI / 180;

            double trailT = Math.Max(0, t - 0.15);
            double trailAngle = rotation + (-halfArc + arcAngle * trailT) * Math.PI / 180;
            double trailR = range * 0.85;

            points.Add(new Point(
                pos.X + Math.Cos(angle) * range,
                pos.Y + Math.Sin(angle) * range));
        }

        for (int i = 0; i < points.Count - 1; i++)
        {
            double thickness = 4 * (1 - (double)i / points.Count);
            var thickPen = new Pen(slashBrush, thickness);
            context.DrawLine(thickPen, points[i], points[i + 1]);
        }

        double glowRadius = range * 0.4 * (1 - progress);
        var glowBrush = new SolidColorBrush(Color.FromArgb((byte)(alpha * 0.3), 150, 180, 255));
        context.DrawEllipse(glowBrush, null, new Point(pos.X, pos.Y), glowRadius, glowRadius);

        if (progress > 0.3 && progress < 0.8)
        {
            double sparkAlpha = Math.Sin((progress - 0.3) / 0.5 * Math.PI);
            var sparkBrush = new SolidColorBrush(Color.FromArgb(
                (byte)(sparkAlpha * 200), _hitSparkColor.R, _hitSparkColor.G, _hitSparkColor.B));
            double sparkRange = range * 0.7;
            double sparkAngle = rotation + (halfArc * 0.3) * Math.PI / 180;

            for (int s = 0; s < 3; s++)
            {
                double sa = sparkAngle + (s - 1) * 0.2;
                double sr = sparkRange + (s - 1) * 5;
                context.DrawEllipse(sparkBrush, null,
                    new Point(pos.X + Math.Cos(sa) * sr, pos.Y + Math.Sin(sa) * sr),
                    2 + s, 2 + s);
            }
        }
    }
}
