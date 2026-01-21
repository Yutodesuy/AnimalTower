using System.Drawing;

namespace AnimalTower;

public sealed class Animal : PhysicsBody
{
    public Color Color { get; set; } = Color.FromArgb(200, 100, 100);
    public float FloorContactTime { get; set; }
    public bool IsTouchingFloor { get; set; }

    public Animal(PointF position, SizeF size) : base(position, size)
    {
    }

    public Animal(PointF position, PointF[] vertices, Color color) : base(position, vertices)
    {
        Color = color;
    }

    public Animal(PointF position, List<PointF[]> shapes, Color color) : base(position, shapes)
    {
        Color = color;
    }

    public static Animal CreateBox(PointF position, float size)
    {
        return new Animal(position, new SizeF(size, size));
    }

    public static Animal CreateTriangle(PointF position, float size)
    {
        float h = size * (float)Math.Sqrt(3) / 2;
        PointF[] vertices = new PointF[]
        {
            new PointF(0, -h / 2),
            new PointF(size / 2, h / 2),
            new PointF(-size / 2, h / 2)
        };
        return new Animal(position, vertices, Color.FromArgb(100, 200, 200));
    }

    public static Animal CreatePentagon(PointF position, float size)
    {
        PointF[] vertices = new PointF[5];
        for (int i = 0; i < 5; i++)
        {
            float angle = (i * 72 - 90) * (float)Math.PI / 180f;
            vertices[i] = new PointF(size / 2 * (float)Math.Cos(angle), size / 2 * (float)Math.Sin(angle));
        }
        return new Animal(position, vertices, Color.FromArgb(200, 200, 100));
    }

    public static Animal CreateTrapezoid(PointF position, float size)
    {
        float topWidth = size * 0.6f;
        float bottomWidth = size;
        float height = size * 0.8f;

        PointF[] vertices = new PointF[]
        {
            new PointF(-topWidth / 2, -height / 2),
            new PointF(topWidth / 2, -height / 2),
            new PointF(bottomWidth / 2, height / 2),
            new PointF(-bottomWidth / 2, height / 2)
        };
        return new Animal(position, vertices, Color.FromArgb(200, 100, 200));
    }

    public static class Factory
    {
        private const float Scale = 1.5f;

        private static PointF[] CreateRect(float x, float y, float w, float h)
        {
            x *= Scale;
            y *= Scale;
            w *= Scale;
            h *= Scale;

            return new PointF[]
            {
                new PointF(x - w / 2, y - h / 2),
                new PointF(x + w / 2, y - h / 2),
                new PointF(x + w / 2, y + h / 2),
                new PointF(x - w / 2, y + h / 2)
            };
        }

        private static PointF[] CreateCirclePoly(float x, float y, float r, int segments = 8)
        {
            x *= Scale;
            y *= Scale;
            r *= Scale;

            PointF[] verts = new PointF[segments];
            for (int i = 0; i < segments; i++)
            {
                float ang = (float)(i * 2 * Math.PI / segments);
                verts[i] = new PointF(x + r * (float)Math.Cos(ang), y + r * (float)Math.Sin(ang));
            }
            return verts;
        }

        private static PointF[] CreateTriangleLocal(float x, float y, float size)
        {
            x *= Scale;
            y *= Scale;
            size *= Scale;

            return new PointF[]
            {
                new PointF(x, y - size),
                new PointF(x + size, y + size / 2),
                new PointF(x - size, y + size / 2)
            };
        }

        public static Animal CreateElephant(PointF pos)
        {
            var shapes = new List<PointF[]>();
            shapes.Add(CreateRect(0, 0, 60, 45));
            shapes.Add(CreateRect(-35, -10, 25, 30));
            shapes.Add(CreateRect(-50, 10, 10, 30));
            shapes.Add(CreateRect(-20, 25, 12, 15));
            shapes.Add(CreateRect(20, 25, 12, 15));

            var animal = new Animal(pos, shapes, Color.Gray);
            animal.Mass = 3.0f;
            animal.Friction = 0.6f;
            return animal;
        }

        public static Animal CreateGiraffe(PointF pos)
        {
            var shapes = new List<PointF[]>();
            shapes.Add(CreateRect(0, 10, 45, 35));
            shapes.Add(CreateRect(-15, -25, 12, 50));
            shapes.Add(CreateRect(-15, -55, 20, 15));

            var animal = new Animal(pos, shapes, Color.Orange);
            animal.Mass = 1.8f;
            return animal;
        }

        public static Animal CreateHippo(PointF pos)
        {
            var shapes = new List<PointF[]>();
            shapes.Add(CreateRect(5, 0, 60, 40));
            shapes.Add(CreateRect(-35, 5, 30, 35));
            shapes.Add(CreateRect(-20, 25, 15, 10));
            shapes.Add(CreateRect(20, 25, 15, 10));

            var animal = new Animal(pos, shapes, Color.MediumPurple);
            animal.Mass = 3.2f;
            animal.Friction = 0.5f;
            return animal;
        }

        public static Animal CreateRhino(PointF pos)
        {
            var shapes = new List<PointF[]>();
            shapes.Add(CreateRect(5, 0, 55, 40));
            shapes.Add(CreateRect(-30, -5, 25, 30));
            shapes.Add(CreateTriangleLocal(-45, -15, 12));

            var animal = new Animal(pos, shapes, Color.DarkGray);
            animal.Mass = 2.8f;
            return animal;
        }

        public static Animal CreateLion(PointF pos)
        {
            var shapes = new List<PointF[]>();
            shapes.Add(CreateRect(10, 5, 45, 30));
            shapes.Add(CreateCirclePoly(-20, 0, 22, 8));

            var animal = new Animal(pos, shapes, Color.Goldenrod);
            animal.Mass = 2.0f;
            return animal;
        }

        public static Animal CreatePanda(PointF pos)
        {
            var shapes = new List<PointF[]>();
            shapes.Add(CreateCirclePoly(0, 10, 25, 10));
            shapes.Add(CreateCirclePoly(0, -25, 20, 10));
            shapes.Add(CreateCirclePoly(-15, -40, 8, 6));
            shapes.Add(CreateCirclePoly(15, -40, 8, 6));

            var animal = new Animal(pos, shapes, Color.WhiteSmoke);
            animal.Mass = 2.2f;
            return animal;
        }

        public static Animal CreateRabbit(PointF pos)
        {
            var shapes = new List<PointF[]>();
            shapes.Add(CreateCirclePoly(0, 10, 20, 8));
            shapes.Add(CreateCirclePoly(0, -15, 15, 8));
            shapes.Add(CreateRect(-5, -35, 6, 25));
            shapes.Add(CreateRect(5, -35, 6, 25));

            var animal = new Animal(pos, shapes, Color.Pink);
            animal.Mass = 1.0f;
            return animal;
        }

        public static Animal CreateCat(PointF pos)
        {
            var shapes = new List<PointF[]>();
            shapes.Add(CreateRect(0, 5, 35, 20));
            shapes.Add(CreateCirclePoly(-10, -15, 12, 8));
            shapes.Add(CreateTriangleLocal(-18, -25, 6));
            shapes.Add(CreateTriangleLocal(-2, -25, 6));

            var animal = new Animal(pos, shapes, Color.SandyBrown);
            animal.Mass = 0.9f;
            return animal;
        }

        public static Animal CreateChick(PointF pos)
        {
            var shapes = new List<PointF[]>();
            shapes.Add(CreateCirclePoly(0, 5, 15, 8));
            shapes.Add(CreateCirclePoly(0, -15, 10, 8));

            var animal = new Animal(pos, shapes, Color.Yellow);
            animal.Mass = 0.5f;
            return animal;
        }

        public static Animal CreateTurtle(PointF pos)
        {
            var shapes = new List<PointF[]>();
            shapes.Add(CreateRect(0, 0, 50, 25));
            shapes.Add(CreateRect(-30, 0, 15, 15));

            var animal = new Animal(pos, shapes, Color.ForestGreen);
            animal.Mass = 1.5f;
            animal.Friction = 0.3f;
            return animal;
        }
    }
}
