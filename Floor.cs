namespace AnimalTower;

public sealed class Floor
{
    public float Y { get; set; }
    public float Width { get; set; }
    public float Friction { get; set; } = 0.5f;

    public Floor(float y, float width)
    {
        Y = y;
        Width = width;
    }
}
