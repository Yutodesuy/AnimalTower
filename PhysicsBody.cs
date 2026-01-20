using System.Drawing;

namespace AnimalTower;

public class PhysicsBody
{
    public PointF Position { get; set; }
    public PointF Velocity { get; set; }
    public PointF Acceleration { get; set; }
    public SizeF Size { get; set; }
    public float Rotation { get; set; } // In degrees
    public float AngularVelocity { get; set; }

    public float Mass { get; set; } = 1.0f;
    public float Restitution { get; set; } = 0.2f; // Bounciness
    public float Friction { get; set; } = 0.5f;

    // Moment of Inertia for a rectangle: I = m * (w^2 + h^2) / 12
    public float MomentOfInertia
    {
        get
        {
            return Mass * (Size.Width * Size.Width + Size.Height * Size.Height) / 12f;
        }
    }

    public RectangleF Bounds => new RectangleF(
        Position.X - Size.Width / 2,
        Position.Y - Size.Height / 2,
        Size.Width,
        Size.Height
    );

    public PhysicsBody(PointF position, SizeF size)
    {
        Position = position;
        Size = size;
        Velocity = PointF.Empty;
        Acceleration = PointF.Empty;
    }

    public PointF[] GetCorners()
    {
        float hw = Size.Width / 2f;
        float hh = Size.Height / 2f;

        // Corners relative to center (unrotated)
        // TL, TR, BR, BL
        PointF[] corners = new PointF[]
        {
            new PointF(-hw, -hh),
            new PointF(hw, -hh),
            new PointF(hw, hh),
            new PointF(-hw, hh)
        };

        // Rotation matrix
        double rad = Rotation * Math.PI / 180.0;
        float cos = (float)Math.Cos(rad);
        float sin = (float)Math.Sin(rad);

        for (int i = 0; i < 4; i++)
        {
            float rx = corners[i].X * cos - corners[i].Y * sin;
            float ry = corners[i].X * sin + corners[i].Y * cos;
            corners[i] = new PointF(Position.X + rx, Position.Y + ry);
        }

        return corners;
    }
}
