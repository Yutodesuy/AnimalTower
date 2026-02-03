using System.Drawing;
using System.Drawing.Drawing2D;

namespace AnimalTower;

public enum BoardShape
{
    Rectangle,
    Triangle,
    Star
}

/// <summary>
/// タワーを支えるための板（ボード）クラスです。
/// 静的オブジェクトとして扱われ、長方形、三角形、星形などの形状を持ちます。
/// </summary>
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

    /// <summary>
    /// 指定された形状とサイズに基づいて頂点データを生成します。
    /// </summary>
    private List<PointF[]> GenerateShape(BoardShape shape, SizeF size)
    {
        var shapes = new List<PointF[]>();
        float w = size.Width;
        float h = size.Height;

        if (shape == BoardShape.Triangle)
        {
            // 上向きの三角形
            shapes.Add(new PointF[]
            {
                new PointF(0, -h/2),       // Top
                new PointF(w/2, h/2),      // Bottom Right
                new PointF(-w/2, h/2)      // Bottom Left
            });
        }
        else if (shape == BoardShape.Star)
        {
            // 星形（凸多角形の組み合わせで表現）
            float R = Math.Min(w, h) / 2f;
            float r = R * 0.4f;

            PointF[] innerPoints = new PointF[5];
            PointF[] outerPoints = new PointF[5];

            double angleStep = Math.PI * 2 / 5;
            double offset = -Math.PI / 2; // 上から開始

            for (int i = 0; i < 5; i++)
            {
                double angleO = offset + i * angleStep;
                double angleI = offset + i * angleStep + angleStep / 2.0;

                outerPoints[i] = new PointF((float)(R * Math.Cos(angleO)), (float)(R * Math.Sin(angleO)));
                innerPoints[i] = new PointF((float)(r * Math.Cos(angleI)), (float)(r * Math.Sin(angleI)));
            }

            // 中央の五角形
            shapes.Add(innerPoints);

            // 5つの頂点（三角形）
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
