namespace AnimalTower;

public sealed class Floor
{
    public float Y { get; set; }
    public float Width { get; set; }

    public Floor(float y, float width)
    {
        Y = y;
        Width = width;
    }
}
