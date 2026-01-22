using System.Drawing;
using System.Drawing.Drawing2D;

namespace AnimalTower;

public enum BoardShape
{
    Rectangle,
    Triangle,
    Star
}

public sealed class SupportBoard : PhysicsBody
{
    public SupportBoard(PointF center, SizeF size, BoardShape shape = BoardShape.Rectangle) : base(center, size)
    {
        IsStatic = true;
        Mass = float.PositiveInfinity;
        Friction = 0.5f;

        if (shape != BoardShape.Rectangle)
        {
            LocalShapes.Clear();
            LocalShapes.AddRange(GenerateShape(shape, size));
        }
    }

    private List<PointF[]> GenerateShape(BoardShape shape, SizeF size)
    {
        var shapes = new List<PointF[]>();
        float w = size.Width;
        float h = size.Height;

        if (shape == BoardShape.Triangle)
        {
            // Point-up Triangle
            shapes.Add(new PointF[]
            {
                new PointF(0, -h/2),       // Top
                new PointF(w/2, h/2),      // Bottom Right
                new PointF(-w/2, h/2)      // Bottom Left
            });
        }
        else if (shape == BoardShape.Star)
        {
            // 5-pointed star fitting in size
            float R = Math.Min(w, h) / 2f;
            float r = R * 0.4f;

            PointF[] innerPoints = new PointF[5];
            PointF[] outerPoints = new PointF[5];

            double angleStep = Math.PI * 2 / 5;
            double offset = -Math.PI / 2; // Start at top

            for (int i = 0; i < 5; i++)
            {
                double angleO = offset + i * angleStep;
                double angleI = offset + i * angleStep + angleStep / 2.0;

                outerPoints[i] = new PointF((float)(R * Math.Cos(angleO)), (float)(R * Math.Sin(angleO)));
                innerPoints[i] = new PointF((float)(r * Math.Cos(angleI)), (float)(r * Math.Sin(angleI)));
            }

            // Central Pentagon (Convex)
            shapes.Add(innerPoints);

            // 5 Point Triangles
            for (int i = 0; i < 5; i++)
            {
                // Triangle: Inner[i-1], Outer[i], Inner[i]
                int prev = (i + 4) % 5;
                shapes.Add(new PointF[]
                {
                    innerPoints[prev],
                    outerPoints[i],
                    innerPoints[i]
                });
            }
        }

        return shapes;
    }
}
