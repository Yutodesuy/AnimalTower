using System.Drawing;

namespace AnimalTower;

public sealed class Animal : PhysicsBody
{
    public Color Color { get; set; } = Color.FromArgb(200, 100, 100);
    public float FloorContactTime { get; set; }
    public bool IsTouchingFloor { get; set; }

    // Constructor for backward compatibility (Box)
    public Animal(PointF position, SizeF size) : base(position, size)
    {
    }

    // Constructor for arbitrary shapes
    public Animal(PointF position, PointF[] vertices, Color color) : base(position, vertices)
    {
        Color = color;
    }

    public static Animal CreateBox(PointF position, float size)
    {
        return new Animal(position, new SizeF(size, size));
    }

    public static Animal CreateRectangle(PointF position, float width, float height)
    {
        return new Animal(position, new SizeF(width, height));
    }

    public static Animal CreateTriangle(PointF position, float size)
    {
        // Equilateral-ish triangle
        float h = size * (float)Math.Sqrt(3) / 2;
        PointF[] vertices = new PointF[]
        {
            new PointF(0, -h/2),
            new PointF(size/2, h/2),
            new PointF(-size/2, h/2)
        };
        return new Animal(position, vertices, Color.FromArgb(100, 200, 200));
    }

    public static Animal CreatePentagon(PointF position, float size)
    {
        // Regular Pentagon
        PointF[] vertices = new PointF[5];
        for (int i = 0; i < 5; i++)
        {
            float angle = (i * 72 - 90) * (float)Math.PI / 180f; // Start from top (-90 deg)
            vertices[i] = new PointF(size/2 * (float)Math.Cos(angle), size/2 * (float)Math.Sin(angle));
        }
        return new Animal(position, vertices, Color.FromArgb(200, 200, 100));
    }

    public static Animal CreateTrapezoid(PointF position, float size)
    {
        // Isosceles Trapezoid
        float topWidth = size * 0.6f;
        float bottomWidth = size;
        float height = size * 0.8f;

        PointF[] vertices = new PointF[]
        {
            new PointF(-topWidth/2, -height/2),
            new PointF(topWidth/2, -height/2),
            new PointF(bottomWidth/2, height/2),
            new PointF(-bottomWidth/2, height/2)
        };
        return new Animal(position, vertices, Color.FromArgb(200, 100, 200));
    }
}
