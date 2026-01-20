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

    // Constructor for composite shapes
    public Animal(PointF position, List<PointF[]> shapes, Color color) : base(position, shapes)
    {
        Color = color;
    }

    // --- Legacy Create methods (can still be used, but we prefer the Factory now) ---
    public static Animal CreateBox(PointF position, float size)
    {
        return new Animal(position, new SizeF(size, size));
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

    // --- Animal Factory ---

    public static class Factory
    {
        private static PointF[] CreateRect(float x, float y, float w, float h)
        {
            return new PointF[]
            {
                new PointF(x - w/2, y - h/2),
                new PointF(x + w/2, y - h/2),
                new PointF(x + w/2, y + h/2),
                new PointF(x - w/2, y + h/2)
            };
        }

        private static PointF[] CreateCirclePoly(float x, float y, float r, int segments = 8)
        {
            PointF[] verts = new PointF[segments];
            for(int i=0; i<segments; i++)
            {
                float ang = (float)(i * 2 * Math.PI / segments);
                verts[i] = new PointF(x + r * (float)Math.Cos(ang), y + r * (float)Math.Sin(ang));
            }
            return verts;
        }

        // 1. Elephant (Heavy, Grey)
        public static Animal CreateElephant(PointF pos)
        {
            var shapes = new List<PointF[]>();
            // Body
            shapes.Add(CreateRect(0, 0, 60, 45));
            // Head
            shapes.Add(CreateRect(-35, -10, 25, 30));
            // Trunk
            shapes.Add(CreateRect(-50, 10, 10, 30));
            // Legs (Visual mostly, but physics too)
            shapes.Add(CreateRect(-20, 25, 12, 15));
            shapes.Add(CreateRect(20, 25, 12, 15));

            var animal = new Animal(pos, shapes, Color.Gray);
            animal.Mass = 3.0f; // Heavy
            animal.Friction = 0.6f;
            return animal;
        }

        // 2. Giraffe (Tall, Yellow)
        public static Animal CreateGiraffe(PointF pos)
        {
            var shapes = new List<PointF[]>();
            // Body
            shapes.Add(CreateRect(0, 10, 45, 35));
            // Neck
            shapes.Add(CreateRect(-15, -25, 12, 50));
            // Head
            shapes.Add(CreateRect(-15, -55, 20, 15));

            var animal = new Animal(pos, shapes, Color.Orange);
            animal.Mass = 1.8f;
            return animal;
        }

        // 3. Hippo (Wide, Purple-ish)
        public static Animal CreateHippo(PointF pos)
        {
            var shapes = new List<PointF[]>();
            // Main Body
            shapes.Add(CreateRect(5, 0, 60, 40));
            // Head
            shapes.Add(CreateRect(-35, 5, 30, 35));
            // Legs
            shapes.Add(CreateRect(-20, 25, 15, 10));
            shapes.Add(CreateRect(20, 25, 15, 10));

            var animal = new Animal(pos, shapes, Color.MediumPurple);
            animal.Mass = 3.2f;
            animal.Friction = 0.5f;
            return animal;
        }

        // 4. Rhino (Grey, Horn)
        public static Animal CreateRhino(PointF pos)
        {
            var shapes = new List<PointF[]>();
            // Body
            shapes.Add(CreateRect(5, 0, 55, 40));
            // Head
            shapes.Add(CreateRect(-30, -5, 25, 30));
            // Horn
            shapes.Add(CreateTriangleLocal(-45, -15, 12));

            var animal = new Animal(pos, shapes, Color.DarkGray);
            animal.Mass = 2.8f;
            return animal;
        }

        private static PointF[] CreateTriangleLocal(float x, float y, float size)
        {
             // Triangle centered at x,y
             return new PointF[]
             {
                 new PointF(x, y - size),
                 new PointF(x + size, y + size/2),
                 new PointF(x - size, y + size/2)
             };
        }

        // 5. Lion (Mane, Yellow/Brown)
        public static Animal CreateLion(PointF pos)
        {
            var shapes = new List<PointF[]>();
            // Body
            shapes.Add(CreateRect(10, 5, 45, 30));
            // Head (Mane)
            shapes.Add(CreateCirclePoly(-20, 0, 22, 8)); // Octagon approximation

            var animal = new Animal(pos, shapes, Color.Goldenrod);
            animal.Mass = 2.0f;
            return animal;
        }

        // 6. Panda (Round, Black/White - Simple White for now with shapes)
        public static Animal CreatePanda(PointF pos)
        {
            var shapes = new List<PointF[]>();
            // Body
            shapes.Add(CreateCirclePoly(0, 10, 25, 10));
            // Head
            shapes.Add(CreateCirclePoly(0, -25, 20, 10));
            // Ears
            shapes.Add(CreateCirclePoly(-15, -40, 8, 6));
            shapes.Add(CreateCirclePoly(15, -40, 8, 6));

            var animal = new Animal(pos, shapes, Color.WhiteSmoke); // Approximation
            animal.Mass = 2.2f;
            return animal;
        }

        // 7. Rabbit (Long Ears, Pink/White)
        public static Animal CreateRabbit(PointF pos)
        {
            var shapes = new List<PointF[]>();
            // Body
            shapes.Add(CreateCirclePoly(0, 10, 20, 8));
            // Head
            shapes.Add(CreateCirclePoly(0, -15, 15, 8));
            // Ears
            shapes.Add(CreateRect(-5, -35, 6, 25));
            shapes.Add(CreateRect(5, -35, 6, 25));

            var animal = new Animal(pos, shapes, Color.Pink);
            animal.Mass = 1.0f;
            return animal;
        }

        // 8. Cat (Small, Pointy Ears)
        public static Animal CreateCat(PointF pos)
        {
            var shapes = new List<PointF[]>();
            // Body
            shapes.Add(CreateRect(0, 5, 35, 20));
            // Head
            shapes.Add(CreateCirclePoly(-10, -15, 12, 8));
            // Ears
            shapes.Add(CreateTriangleLocal(-18, -25, 6));
            shapes.Add(CreateTriangleLocal(-2, -25, 6));

            var animal = new Animal(pos, shapes, Color.SandyBrown);
            animal.Mass = 0.9f;
            return animal;
        }

        // 9. Chick (Small, Yellow)
        public static Animal CreateChick(PointF pos)
        {
            var shapes = new List<PointF[]>();
            // Body
            shapes.Add(CreateCirclePoly(0, 5, 15, 8));
            // Head
            shapes.Add(CreateCirclePoly(0, -15, 10, 8));

            var animal = new Animal(pos, shapes, Color.Yellow);
            animal.Mass = 0.5f; // Light
            return animal;
        }

        // 10. Turtle (Flat, Green)
        public static Animal CreateTurtle(PointF pos)
        {
            var shapes = new List<PointF[]>();
            // Shell
            shapes.Add(CreateRect(0, 0, 50, 25));
            // Head
            shapes.Add(CreateRect(-30, 0, 15, 15));

            var animal = new Animal(pos, shapes, Color.ForestGreen);
            animal.Mass = 1.5f;
            animal.Friction = 0.3f; // Slippery shell? Or grippy?
            return animal;
        }
    }
}
