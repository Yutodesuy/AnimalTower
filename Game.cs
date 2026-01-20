using System.Drawing.Drawing2D;

namespace AnimalTower;

public sealed class Game : IDisposable
{
    private int _width;
    private int _height;
    private readonly Font _debugFont;

    public enum GameState
    {
        Aiming,
        Falling,
        Landed, // Transitional state
        GameOver
    }

    private GameState _currentState;
    private Animal? _currentAnimal;
    private readonly List<Animal> _landedAnimals = new();
    private readonly Floor _floor; // We need a floor instance

    public Game(int width, int height)
    {
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);
        _debugFont = new Font("Segoe UI", 14, FontStyle.Bold);

        // Initialize Floor (fixed position for now)
        _floor = new Floor(_height - 50, _width);

        StartNewGame();
    }

    private void StartNewGame()
    {
        _landedAnimals.Clear();
        _currentState = GameState.Aiming;
        SpawnAnimal();
    }

    private void SpawnAnimal()
    {
        // MVP: Simple 40x40 box
        float startX = _width / 2f;
        float startY = 60f;
        _currentAnimal = new Animal(new PointF(startX, startY), new SizeF(40, 40));
        _currentState = GameState.Aiming;
    }

    public void HandleInput(Keys key)
    {
        if (_currentState == GameState.Aiming && _currentAnimal != null)
        {
            float speed = 10f; // pixels per keypress (rough)

            switch (key)
            {
                case Keys.Left:
                    _currentAnimal.Position = new PointF(_currentAnimal.Position.X - speed, _currentAnimal.Position.Y);
                    break;
                case Keys.Right:
                    _currentAnimal.Position = new PointF(_currentAnimal.Position.X + speed, _currentAnimal.Position.Y);
                    break;
                case Keys.Space:
                case Keys.Enter:
                    _currentState = GameState.Falling;
                    break;
            }
        }
    }

    public void Update(float dt)
    {
        if (_currentState == GameState.Falling && _currentAnimal != null)
        {
            // 1. Gravity
            float gravity = 500f; // pixels/s^2
            _currentAnimal.Velocity = new PointF(_currentAnimal.Velocity.X, _currentAnimal.Velocity.Y + gravity * dt);

            // 2. Integration
            _currentAnimal.Position = new PointF(
                _currentAnimal.Position.X + _currentAnimal.Velocity.X * dt,
                _currentAnimal.Position.Y + _currentAnimal.Velocity.Y * dt
            );

            // 3. Collision with Floor
            float animalBottom = _currentAnimal.Position.Y + _currentAnimal.Size.Height / 2;

            if (animalBottom >= _floor.Y)
            {
                // Simple collision response: Stop and Snap
                _currentAnimal.Position = new PointF(_currentAnimal.Position.X, _floor.Y - _currentAnimal.Size.Height / 2);
                _currentAnimal.Velocity = PointF.Empty;

                // Change State
                _currentState = GameState.Landed;
                _landedAnimals.Add(_currentAnimal);
                _currentAnimal = null;

                // Spawn next
                SpawnAnimal();
            }

            // 4. Game Over Check
            if (_currentAnimal != null && _currentAnimal.Position.Y > _height + 100)
            {
                _currentState = GameState.GameOver;
            }
        }
    }

    public void Render(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(20, 24, 30));

        // Draw Floor
        using var floorPen = new Pen(Color.FromArgb(180, 200, 210), 3f);
        g.DrawLine(floorPen, 0, _floor.Y, _width, _floor.Y);

        // Helper to draw animal
        void DrawAnimal(Animal animal, Brush brush)
        {
            // Center is Position. Top-Left is Position - Size/2
            float x = animal.Position.X - animal.Size.Width / 2;
            float y = animal.Position.Y - animal.Size.Height / 2;
            g.FillRectangle(brush, x, y, animal.Size.Width, animal.Size.Height);
            g.DrawRectangle(Pens.Black, x, y, animal.Size.Width, animal.Size.Height);
        }

        // Draw Landed Animals
        using var landedBrush = new SolidBrush(Color.FromArgb(100, 200, 100));
        foreach (var animal in _landedAnimals)
        {
            DrawAnimal(animal, landedBrush);
        }

        // Draw Current Animal
        if (_currentAnimal != null)
        {
            using var currentBrush = new SolidBrush(Color.FromArgb(200, 100, 100));
            DrawAnimal(_currentAnimal, currentBrush);
        }

        // UI / Debug
        string label = $"State: {_currentState} | Animals: {_landedAnimals.Count}";
        if (_currentState == GameState.GameOver)
        {
            label = "GAME OVER";
        }

        g.DrawString(label, _debugFont, Brushes.Gainsboro, 10, 10);
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
