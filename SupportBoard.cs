namespace AnimalTower;

public sealed class SupportBoard
{
    public PointF Center { get; set; }
    public SizeF Size { get; set; }
    public float Rotation { get; set; }

    public SupportBoard(PointF center, SizeF size)
    {
        Center = center;
        Size = size;
        Rotation = 0f;
    }
}
