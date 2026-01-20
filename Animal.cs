namespace AnimalTower;

public sealed class Animal
{
    public PointF Position { get; set; }
    public SizeF Size { get; set; }
    public float Rotation { get; set; }

    public Animal(PointF position, SizeF size)
    {
        Position = position;
        Size = size;
        Rotation = 0f;
    }
}
