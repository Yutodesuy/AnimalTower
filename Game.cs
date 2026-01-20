using System.Drawing.Drawing2D;

namespace AnimalTower;

public sealed class Game : IDisposable
{
    private int _width;
    private int _height;
    private readonly Font _debugFont;

    public enum GameState
    {
        Title,
        Aiming,
        Falling,
        Landed, // Transitional state
        GameOver
    }

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    private GameState _currentState;
    private Difficulty _difficulty = Difficulty.Normal; // Explicitly set default
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

        _currentState = GameState.Title;
    }

    private void StartNewGame()
    {
        _landedAnimals.Clear();
        _currentState = GameState.Aiming;
        UpdateFloorDimensions();
        SpawnAnimal();
    }

    private void UpdateFloorDimensions()
    {
        float floorWidth = _width; // Default
        switch (_difficulty)
        {
            case Difficulty.Easy:
                floorWidth = _width * 0.75f;
                break;
            case Difficulty.Normal:
                floorWidth = _width * 0.5f;
                break;
            case Difficulty.Hard:
                floorWidth = _width * 0.25f;
                break;
        }
        _floor.Width = floorWidth;
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
        if (_currentState == GameState.Title)
        {
            int difficultyCount = Enum.GetValues(typeof(Difficulty)).Length;
            switch (key)
            {
                case Keys.Up:
                    _difficulty = (Difficulty)(((int)_difficulty + difficultyCount - 1) % difficultyCount);
                    break;
                case Keys.Down:
                    _difficulty = (Difficulty)(((int)_difficulty + 1) % difficultyCount);
                    break;
                case Keys.Enter:
                    StartNewGame();
                    break;
                case Keys.D1:
                case Keys.NumPad1:
                    _difficulty = Difficulty.Easy;
                    break;
                case Keys.D2:
                case Keys.NumPad2:
                    _difficulty = Difficulty.Normal;
                    break;
                case Keys.D3:
                case Keys.NumPad3:
                    _difficulty = Difficulty.Hard;
                    break;
            }
        }
        else if (_currentState == GameState.Aiming && _currentAnimal != null)
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
        else if (_currentState == GameState.GameOver)
        {
            if (key == Keys.R || key == Keys.Space || key == Keys.Enter)
            {
                _currentState = GameState.Title;
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

            // 2.5 Collision with Landed Animals
            foreach (var landed in _landedAnimals)
            {
                // Simple AABB Collision
                if (_currentAnimal.Bounds.IntersectsWith(landed.Bounds))
                {
                    RectangleF intersection = RectangleF.Intersect(_currentAnimal.Bounds, landed.Bounds);

                    // Determine dominant axis for resolution
                    if (intersection.Width > intersection.Height)
                    {
                        // Vertical Collision
                        if (_currentAnimal.Position.Y < landed.Position.Y) // Hits from top
                        {
                            // Push up
                            _currentAnimal.Position = new PointF(_currentAnimal.Position.X, _currentAnimal.Position.Y - intersection.Height);
                            _currentAnimal.Velocity = new PointF(_currentAnimal.Velocity.X, 0);

                            // Land
                            _currentState = GameState.Landed;
                            _landedAnimals.Add(_currentAnimal);
                            _currentAnimal = null;
                            SpawnAnimal();
                            return;
                        }
                        else // Hits from bottom
                        {
                            // Push down
                            _currentAnimal.Position = new PointF(_currentAnimal.Position.X, _currentAnimal.Position.Y + intersection.Height);
                            _currentAnimal.Velocity = new PointF(_currentAnimal.Velocity.X, 0);
                        }
                    }
                    else
                    {
                        // Horizontal Collision
                        float push = intersection.Width;
                        if (_currentAnimal.Position.X < landed.Position.X)
                            _currentAnimal.Position = new PointF(_currentAnimal.Position.X - push, _currentAnimal.Position.Y);
                        else
                            _currentAnimal.Position = new PointF(_currentAnimal.Position.X + push, _currentAnimal.Position.Y);
                    }
                }
            }

            // 3. Collision with Floor
            float animalBottom = _currentAnimal.Position.Y + _currentAnimal.Size.Height / 2;
            float animalLeft = _currentAnimal.Position.X - _currentAnimal.Size.Width / 2;
            float animalRight = _currentAnimal.Position.X + _currentAnimal.Size.Width / 2;

            float floorStart = (_width - _floor.Width) / 2;
            float floorEnd = floorStart + _floor.Width;

            // Check if within floor X bounds
            bool overFloor = animalRight > floorStart && animalLeft < floorEnd;

            if (overFloor && animalBottom >= _floor.Y)
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
            if (_currentAnimal != null && _currentAnimal.Position.Y > _height + 50)
            {
                _currentState = GameState.GameOver;
            }
        }
    }

    public void Render(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(20, 24, 30));

        if (_currentState == GameState.Title)
        {
            using var titleBrush = new SolidBrush(Color.White);
            using var titleFont = new Font("Segoe UI", 32, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 16);

            string title = "ANIMAL TOWER";
            SizeF titleSize = g.MeasureString(title, titleFont);
            g.DrawString(title, titleFont, titleBrush, (_width - titleSize.Width) / 2, _height / 3);

            float startY = _height / 2;
            foreach (Difficulty diff in Enum.GetValues(typeof(Difficulty)))
            {
                string text = diff.ToString();
                Brush brush = (diff == _difficulty) ? Brushes.Yellow : Brushes.White;
                if (diff == _difficulty) text = "> " + text + " <";

                SizeF textSize = g.MeasureString(text, subFont);
                g.DrawString(text, subFont, brush, (_width - textSize.Width) / 2, startY);
                startY += textSize.Height + 10;
            }
            return;
        }

        // Draw Floor
        using var floorPen = new Pen(Color.FromArgb(180, 200, 210), 3f);
        float floorStart = (_width - _floor.Width) / 2;
        g.DrawLine(floorPen, floorStart, _floor.Y, floorStart + _floor.Width, _floor.Y);

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
        string label = $"State: {_currentState} | Animals: {_landedAnimals.Count} | Diff: {_difficulty}";
        if (_currentState == GameState.GameOver)
        {
            label = "GAME OVER - Press R or Space to Restart";
        }

        g.DrawString(label, _debugFont, Brushes.Gainsboro, 10, 10);
    }

    public void Resize(int width, int height)
    {
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);
        _floor.Y = _height - 50;
        UpdateFloorDimensions();
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
