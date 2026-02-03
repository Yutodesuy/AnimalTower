namespace AnimalTower;

/// <summary>
/// ゲームの床（地面）を表すクラスです。
/// 位置、幅、摩擦係数を保持します。
/// </summary>
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
