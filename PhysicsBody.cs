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
}
