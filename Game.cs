using System.Drawing.Drawing2D;

namespace AnimalTower;

public sealed class Game : IDisposable
{
    private int _width;
    private int _height;
    private readonly Font _debugFont;

    public Game(int width, int height)
    {
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);
        _debugFont = new Font("Segoe UI", 14, FontStyle.Bold);
    }

    public void Update(float dt)
    {
        // Placeholder: future physics + game logic.
    }

    public void Render(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(20, 24, 30));

        using var floorPen = new Pen(Color.FromArgb(180, 200, 210), 3f);
        int floorY = _height - 60;
        g.DrawLine(floorPen, 80, floorY, _width - 80, floorY);

        string label = "Animal Tower - skeleton";
        SizeF textSize = g.MeasureString(label, _debugFont);
        g.DrawString(label, _debugFont, Brushes.Gainsboro,
            (_width - textSize.Width) * 0.5f,
            (_height - textSize.Height) * 0.5f);
    }

    public void Resize(int width, int height)
    {
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);
    }

    public void HandleMouseMove(Point position)
    {
        // Placeholder for support board control.
    }

    public void HandleMouseDown(MouseButtons button, Point position)
    {
        // Placeholder for future input.
    }

    public void HandleMouseUp(MouseButtons button, Point position)
    {
        // Placeholder for future input.
    }

    public void Dispose()
    {
        _debugFont.Dispose();
    }
}
