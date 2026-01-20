using System.Diagnostics;

namespace AnimalTower;

public sealed class GameForm : Form
{
    private readonly Timer _timer;
    private readonly Stopwatch _stopwatch;
    private readonly Game _game;

    public GameForm()
    {
        Text = "Animal Tower (Skeleton)";
        ClientSize = new Size(960, 540);
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

        _game = new Game(ClientSize.Width, ClientSize.Height);
        _stopwatch = Stopwatch.StartNew();

        _timer = new Timer
        {
            Interval = 16
        };
        _timer.Tick += OnTick;
        _timer.Start();

        Resize += OnResize;
        MouseMove += OnMouseMove;
        MouseDown += OnMouseDown;
        MouseUp += OnMouseUp;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
            _timer.Dispose();
            _game.Dispose();
            _stopwatch.Stop();
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        _game.Render(e.Graphics);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        float dt = (float)_stopwatch.Elapsed.TotalSeconds;
        _stopwatch.Restart();

        _game.Update(dt);
        Invalidate();
    }

    private void OnResize(object? sender, EventArgs e)
    {
        _game.Resize(ClientSize.Width, ClientSize.Height);
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        _game.HandleMouseMove(e.Location);
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        _game.HandleMouseDown(e.Button, e.Location);
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        _game.HandleMouseUp(e.Button, e.Location);
    }
}
