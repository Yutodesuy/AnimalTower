using System.Drawing;

namespace AnimalTower;

public sealed class SupportBoard : PhysicsBody
{
    public SupportBoard(PointF center, SizeF size) : base(center, size)
    {
        IsStatic = true;
        Mass = float.PositiveInfinity;
        Friction = 0.5f;
    }
}
