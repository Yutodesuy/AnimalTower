using System.Drawing;

namespace AnimalTower;

public class PhysicsBody
{
    public PointF Position { get; set; }
    public PointF Velocity { get; set; }
    public PointF Acceleration { get; set; }

    // Internal vertices relative to (0,0) center
    protected PointF[] LocalVertices { get; set; }

    public float Rotation { get; set; } // In degrees
    public float AngularVelocity { get; set; }

    public float Mass { get; set; } = 1.0f;
    public float Restitution { get; set; } = 0.2f; // Bounciness
    public float Friction { get; set; } = 0.5f;

    // Moment of Inertia approximation using bounding box
    public float MomentOfInertia
    {
        get
        {
            // Approximate as a box
            var size = GetBoundingSize();
            return Mass * (size.Width * size.Width + size.Height * size.Height) / 12f;
        }
    }

    public RectangleF Bounds
    {
        get
        {
            var size = GetBoundingSize();
            return new RectangleF(
                Position.X - size.Width / 2,
                Position.Y - size.Height / 2,
                size.Width,
                size.Height
            );
        }
    }

    public PhysicsBody(PointF position, SizeF size)
    {
        Position = position;
        Velocity = PointF.Empty;
        Acceleration = PointF.Empty;

        // Default to box shape for backward compatibility
        float hw = size.Width / 2f;
        float hh = size.Height / 2f;
        LocalVertices = new PointF[]
        {
            new PointF(-hw, -hh),
            new PointF(hw, -hh),
            new PointF(hw, hh),
            new PointF(-hw, hh)
        };
    }

    public PhysicsBody(PointF position, PointF[] localVertices)
    {
        Position = position;
        Velocity = PointF.Empty;
        Acceleration = PointF.Empty;
        LocalVertices = localVertices;
    }

    private SizeF GetBoundingSize()
    {
        if (LocalVertices == null || LocalVertices.Length == 0) return SizeF.Empty;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var v in LocalVertices)
        {
            if (v.X < minX) minX = v.X;
            if (v.X > maxX) maxX = v.X;
            if (v.Y < minY) minY = v.Y;
            if (v.Y > maxY) maxY = v.Y;
        }
        return new SizeF(maxX - minX, maxY - minY);
    }

    public PointF[] GetTransformedVertices()
    {
        if (LocalVertices == null) return Array.Empty<PointF>();

        PointF[] transformed = new PointF[LocalVertices.Length];

        // Rotation matrix
        double rad = Rotation * Math.PI / 180.0;
        float cos = (float)Math.Cos(rad);
        float sin = (float)Math.Sin(rad);

        for (int i = 0; i < LocalVertices.Length; i++)
        {
            float rx = LocalVertices[i].X * cos - LocalVertices[i].Y * sin;
            float ry = LocalVertices[i].X * sin + LocalVertices[i].Y * cos;
            transformed[i] = new PointF(Position.X + rx, Position.Y + ry);
        }

        return transformed;
    }
}
