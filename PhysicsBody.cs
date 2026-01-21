using System.Drawing;

namespace AnimalTower;

public class PhysicsBody
{
    public PointF Position { get; set; }
    public PointF Velocity { get; set; }
    public PointF Acceleration { get; set; }

    protected List<PointF[]> LocalShapes { get; set; }

    public float Rotation { get; set; }
    public float AngularVelocity { get; set; }
    public bool IsStatic { get; set; }
    public float Mass { get; set; } = 1.0f;
    public float Restitution { get; set; } = 0.2f;
    public float Friction { get; set; } = 0.5f;

    public float MomentOfInertia
    {
        get
        {
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
        LocalShapes = new List<PointF[]>();

        float hw = size.Width / 2f;
        float hh = size.Height / 2f;
        var box = new PointF[]
        {
            new PointF(-hw, -hh),
            new PointF(hw, -hh),
            new PointF(hw, hh),
            new PointF(-hw, hh)
        };
        LocalShapes.Add(box);
    }

    public PhysicsBody(PointF position, PointF[] localVertices)
    {
        Position = position;
        Velocity = PointF.Empty;
        Acceleration = PointF.Empty;
        LocalShapes = new List<PointF[]> { localVertices };
    }

    public PhysicsBody(PointF position, List<PointF[]> localShapes)
    {
        Position = position;
        Velocity = PointF.Empty;
        Acceleration = PointF.Empty;
        LocalShapes = localShapes;
    }

    private SizeF GetBoundingSize()
    {
        if (LocalShapes == null || LocalShapes.Count == 0) return SizeF.Empty;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var shape in LocalShapes)
        {
            foreach (var v in shape)
            {
                if (v.X < minX) minX = v.X;
                if (v.X > maxX) maxX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Y > maxY) maxY = v.Y;
            }
        }

        if (minX > maxX) return SizeF.Empty;

        return new SizeF(maxX - minX, maxY - minY);
    }

    public List<PointF[]> GetTransformedVertices()
    {
        if (LocalShapes == null) return new List<PointF[]>();

        var result = new List<PointF[]>();
        double rad = Rotation * Math.PI / 180.0;
        float cos = (float)Math.Cos(rad);
        float sin = (float)Math.Sin(rad);

        foreach (var shape in LocalShapes)
        {
            PointF[] transformed = new PointF[shape.Length];
            for (int i = 0; i < shape.Length; i++)
            {
                float rx = shape[i].X * cos - shape[i].Y * sin;
                float ry = shape[i].X * sin + shape[i].Y * cos;
                transformed[i] = new PointF(Position.X + rx, Position.Y + ry);
            }
            result.Add(transformed);
        }

        return result;
    }
}
