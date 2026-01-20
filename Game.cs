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
    private float _aimTimer; // Timer for forced drop
    private readonly List<Animal> _landedAnimals = new();
    private readonly Floor _floor; // We need a floor instance
    private readonly Random _random = new();

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
        _floor.Friction = 0.5f; // Default

        switch (_difficulty)
        {
            case Difficulty.Easy:
                floorWidth = _width * 0.75f;
                break;
            case Difficulty.Normal:
                floorWidth = _width * 0.5f;
                _floor.Friction = 0.5f; // Explicit request
                break;
            case Difficulty.Hard:
                floorWidth = _width * 0.25f;
                break;
        }
        _floor.Width = floorWidth;
    }

    private void SpawnAnimal()
    {
        float startX = _width / 2f;
        float startY = 60f;

        int shapeType = _random.Next(4); // 0 to 3
        float size = 45f;

        switch (shapeType)
        {
            case 0:
                _currentAnimal = Animal.CreateBox(new PointF(startX, startY), size);
                break;
            case 1:
                _currentAnimal = Animal.CreateTriangle(new PointF(startX, startY), size);
                break;
            case 2:
                _currentAnimal = Animal.CreatePentagon(new PointF(startX, startY), size);
                break;
            case 3:
                _currentAnimal = Animal.CreateTrapezoid(new PointF(startX, startY), size);
                break;
            default:
                _currentAnimal = Animal.CreateBox(new PointF(startX, startY), size);
                break;
        }

        _currentState = GameState.Aiming;
        _aimTimer = 5.0f; // Reset timer
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
        if (_currentState == GameState.Aiming && _currentAnimal != null)
        {
            _aimTimer -= dt;
            if (_aimTimer <= 0)
            {
                _currentState = GameState.Falling;
                _aimTimer = 0;
            }
        }

        if (_currentState == GameState.Falling && _currentAnimal != null)
        {
            // --- PHYSICS UPDATE ---

            // 1. Gravity and Forces
            float gravity = 500f; // pixels/s^2
            _currentAnimal.Velocity = new PointF(_currentAnimal.Velocity.X, _currentAnimal.Velocity.Y + gravity * dt);

            // 2. Integration (Euler)
            _currentAnimal.Position = new PointF(
                _currentAnimal.Position.X + _currentAnimal.Velocity.X * dt,
                _currentAnimal.Position.Y + _currentAnimal.Velocity.Y * dt
            );

            // Integrate Rotation
            _currentAnimal.Rotation += _currentAnimal.AngularVelocity * dt;

            // 3. Collision Resolution
            bool collisionOccurred = false;

            // 3a. Collision with Floor
            if (CheckAndResolveFloorCollision(_currentAnimal))
            {
                collisionOccurred = true;
            }

            // 3b. Collision with Landed Animals
            foreach (var landed in _landedAnimals)
            {
                if (ResolveCollision(_currentAnimal, landed))
                {
                    collisionOccurred = true;
                }
            }

            // State Transition: If speed is very low and hitting something, land it.
            if (collisionOccurred)
            {
                float vSq = _currentAnimal.Velocity.X * _currentAnimal.Velocity.X + _currentAnimal.Velocity.Y * _currentAnimal.Velocity.Y;
                float angSq = _currentAnimal.AngularVelocity * _currentAnimal.AngularVelocity;

                // Thresholds: velocity squared < 100 (10 px/s) and angular velocity squared < 10 (~3 deg/s)
                // Relaxed slightly to allow easier stacking
                if (vSq < 150 && angSq < 50)
                {
                    _currentState = GameState.Landed;
                    _landedAnimals.Add(_currentAnimal);
                    _currentAnimal = null;
                    SpawnAnimal();
                }
            }

            // If it fell off screen
            if (_currentAnimal != null && _currentAnimal.Position.Y > _height + 100)
            {
                _currentState = GameState.GameOver;
            }
        }
    }

    // --- PHYSICS HELPERS ---

    // Separating Axis Theorem (SAT)
    // Returns true if collision resolved
    private bool ResolveCollision(Animal a, PhysicsBody b)
    {
        PointF[] shapeA = a.GetTransformedVertices();
        PointF[] shapeB = b.GetTransformedVertices();

        PointF normal = PointF.Empty;
        float depth = float.MaxValue;

        // Axes to test: Normals of all edges of both shapes
        List<PointF> axes = new List<PointF>();
        axes.AddRange(GetAxes(shapeA));
        axes.AddRange(GetAxes(shapeB));

        foreach (var axis in axes)
        {
            var pA = Project(shapeA, axis);
            var pB = Project(shapeB, axis);

            if (!Overlap(pA, pB)) return false; // No collision

            float axisDepth = Math.Min(pA.Max - pB.Min, pB.Max - pA.Min);
            if (axisDepth < depth)
            {
                depth = axisDepth;
                normal = axis;
            }
        }

        // Ensure normal points from B to A
        PointF centerA = a.Position;
        PointF centerB = b.Position;
        PointF dir = new PointF(centerA.X - centerB.X, centerA.Y - centerB.Y);
        if (Dot(dir, normal) < 0)
        {
            normal = new PointF(-normal.X, -normal.Y);
        }

        // Resolve Penetration
        a.Position = new PointF(a.Position.X + normal.X * depth, a.Position.Y + normal.Y * depth);

        // Compute Impulse
        ApplyImpulse(a, b, normal);

        return true;
    }

    private bool CheckAndResolveFloorCollision(Animal a)
    {
        // Floor check: Look for any vertex below floor Y
        PointF[] vertices = a.GetTransformedVertices();
        bool hit = false;

        // Find deepest point
        float maxY = float.MinValue;
        PointF deepPoint = PointF.Empty;

        foreach (var p in vertices)
        {
            // Check lateral bounds
            float floorStart = (_width - _floor.Width) / 2;
            float floorEnd = floorStart + _floor.Width;
            bool withinX = p.X >= floorStart && p.X <= floorEnd;

            if (p.Y > _floor.Y && withinX)
            {
                hit = true;
                if (p.Y > maxY)
                {
                    maxY = p.Y;
                    deepPoint = p;
                }
            }
        }

        if (hit)
        {
            PointF normal = new PointF(0, -1);
            float depth = maxY - _floor.Y;

            // Penetration resolution
            a.Position = new PointF(a.Position.X, a.Position.Y - depth);

            // Impulse
            // Treat floor as infinite mass, stationary body
            ApplyImpulseStatic(a, deepPoint, normal, _floor.Friction);
            return true;
        }

        return false;
    }

    private void ApplyImpulse(Animal a, PhysicsBody b, PointF normal)
    {
        // Simple impulse resolution
        // Velocity Relative
        PointF rv = new PointF(a.Velocity.X - b.Velocity.X, a.Velocity.Y - b.Velocity.Y);

        float velAlongNormal = Dot(rv, normal);

        if (velAlongNormal > 0) return; // Moving away

        float e = Math.Min(a.Restitution, b.Restitution);

        float j = -(1 + e) * velAlongNormal;
        j /= (1 / a.Mass + 1 / b.Mass);

        PointF impulse = new PointF(normal.X * j, normal.Y * j);

        // Apply Linear
        a.Velocity = new PointF(a.Velocity.X + impulse.X / a.Mass, a.Velocity.Y + impulse.Y / a.Mass);
        b.Velocity = new PointF(b.Velocity.X - impulse.X / b.Mass, b.Velocity.Y - impulse.Y / b.Mass);

        // Friction
        PointF tangent = new PointF(-normal.Y, normal.X); // Perpendicular
        float jt = -Dot(rv, tangent);
        jt /= (1 / a.Mass + 1 / b.Mass);

        // Coulomb's law: Clamp friction to mu * normalImpulse
        float mu = (a.Friction + b.Friction) * 0.5f;
        float maxJt = j * mu;
        if (Math.Abs(jt) > maxJt) jt = Math.Sign(jt) * maxJt; // Dynamic friction

        PointF frictionImpulse = new PointF(tangent.X * jt, tangent.Y * jt);
        a.Velocity = new PointF(a.Velocity.X + frictionImpulse.X / a.Mass, a.Velocity.Y + frictionImpulse.Y / a.Mass);
        b.Velocity = new PointF(b.Velocity.X - frictionImpulse.X / b.Mass, b.Velocity.Y - frictionImpulse.Y / b.Mass);

        // Induce rotation based on offset (Simple Approximation)
        // If normal is Vertical, check X offset relative to center diff
        // This simulates torque without complex contact point manifold generation
        float dist = (float)Math.Sqrt(Math.Pow(a.Position.X - b.Position.X, 2) + Math.Pow(a.Position.Y - b.Position.Y, 2));
        if (dist > 1.0f)
        {
            // Vector from B to A
            PointF ba = new PointF(a.Position.X - b.Position.X, a.Position.Y - b.Position.Y);
            // Torque depends on where the impact is relative to COM
            // Cross product of BA and Impulse gives direction
            float torqueVal = ba.X * impulse.Y - ba.Y * impulse.X;

            // Dampen it - this is an approximation
            a.AngularVelocity += (torqueVal * 0.1f) / a.MomentOfInertia;
            b.AngularVelocity -= (torqueVal * 0.1f) / b.MomentOfInertia;
        }
    }

    private void ApplyImpulseStatic(Animal a, PointF contactPoint, PointF normal, float frictionCoeff)
    {
        // r = vector from COM to contact point
        PointF r = new PointF(contactPoint.X - a.Position.X, contactPoint.Y - a.Position.Y);

        // Relative velocity at contact point: V_p = V_cm + omega x r
        // 2D Cross product of scalar omega and vector r is (-omega * r.y, omega * r.x)
        PointF vRel = new PointF(
            a.Velocity.X + (-a.AngularVelocity * 0.01745f * r.Y),
            a.Velocity.Y + (a.AngularVelocity * 0.01745f * r.X)
        );

        float velAlongNormal = Dot(vRel, normal);

        if (velAlongNormal > 0) return;

        // Impulse scalar
        // J = -(1+e) * vRel.n / (1/m + (r x n)^2 / I)
        float rCrossN = r.X * normal.Y - r.Y * normal.X; // 2D Cross product
        float invMass = 1.0f / a.Mass;
        float invI = 1.0f / a.MomentOfInertia;

        float effectiveMass = invMass + (rCrossN * rCrossN) * invI;

        float e = a.Restitution;
        float j = -(1 + e) * velAlongNormal / effectiveMass;

        PointF impulse = new PointF(normal.X * j, normal.Y * j);

        // Apply Linear
        a.Velocity = new PointF(a.Velocity.X + impulse.X * invMass, a.Velocity.Y + impulse.Y * invMass);

        // Apply Angular (Degrees)
        // Torque = r x J
        float torque = r.X * impulse.Y - r.Y * impulse.X;
        float angAccel = torque * invI;
        a.AngularVelocity += angAccel * 57.29f; // Rad to Deg

        // Friction
        PointF tangent = new PointF(-normal.Y, normal.X); // Perpendicular
        float vRelTangent = Dot(vRel, tangent);

        // Tangent impulse
        float rCrossT = r.X * tangent.Y - r.Y * tangent.X;
        float effectiveMassT = invMass + (rCrossT * rCrossT) * invI;

        float jt = -vRelTangent / effectiveMassT;

        // Clamp
        float maxJt = j * (a.Friction + frictionCoeff) * 0.5f;
        if (Math.Abs(jt) > maxJt) jt = Math.Sign(jt) * maxJt;

        PointF frictionImpulse = new PointF(tangent.X * jt, tangent.Y * jt);

        a.Velocity = new PointF(a.Velocity.X + frictionImpulse.X * invMass, a.Velocity.Y + frictionImpulse.Y * invMass);

        float torqueF = r.X * frictionImpulse.Y - r.Y * frictionImpulse.X;
        a.AngularVelocity += (torqueF * invI) * 57.29f;
    }

    // SAT Helpers
    private List<PointF> GetAxes(PointF[] corners)
    {
        var axes = new List<PointF>();
        for (int i = 0; i < corners.Length; i++)
        {
            var p1 = corners[i];
            var p2 = corners[(i + 1) % corners.Length];
            var edge = new PointF(p1.X - p2.X, p1.Y - p2.Y);
            var normal = Normalize(new PointF(-edge.Y, edge.X));
            axes.Add(normal);
        }
        return axes;
    }

    private (float Min, float Max) Project(PointF[] corners, PointF axis)
    {
        float min = Dot(corners[0], axis);
        float max = min;
        for (int i = 1; i < corners.Length; i++)
        {
            float p = Dot(corners[i], axis);
            if (p < min) min = p;
            if (p > max) max = p;
        }
        return (min, max);
    }

    private bool Overlap((float Min, float Max) a, (float Min, float Max) b)
    {
        return !(a.Min > b.Max || b.Min > a.Max);
    }

    private float Dot(PointF a, PointF b) => a.X * b.X + a.Y * b.Y;

    private PointF Normalize(PointF p)
    {
        float len = (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
        if (len == 0) return new PointF(0, 0);
        return new PointF(p.X / len, p.Y / len);
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
        void DrawAnimal(Animal animal)
        {
            // Save state
            var state = g.Save();

            // We do NOT use TranslateTransform/RotateTransform because we have pre-transformed vertices for SAT?
            // Actually, we usually draw based on local shape + transform.
            // But PhysicsBody now has LocalVertices.
            // Let's use the standard Transform way to draw so we don't have to manually transform points for drawing every frame.

            // Wait, GetTransformedVertices is used for Physics.
            // For drawing, we can either use GetTransformedVertices and DrawPolygon, OR use transform matrix.
            // Using GetTransformedVertices is safer because it matches the physics 1:1.

            PointF[] vertices = animal.GetTransformedVertices();

            using (var brush = new SolidBrush(animal.Color))
            {
                g.FillPolygon(brush, vertices);
            }
            g.DrawPolygon(Pens.Black, vertices);

            // Restore
            g.Restore(state);
        }

        // Draw Landed Animals
        foreach (var animal in _landedAnimals)
        {
            DrawAnimal(animal);
        }

        // Draw Current Animal
        if (_currentAnimal != null)
        {
            DrawAnimal(_currentAnimal);
        }

        // Draw Timer if Aiming
        if (_currentState == GameState.Aiming)
        {
            string timerText = $"{_aimTimer:0.0}";
            SizeF size = g.MeasureString(timerText, _debugFont);
            g.DrawString(timerText, _debugFont, Brushes.Orange, (_width - size.Width) / 2, 100);
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
    }

    public void HandleMouseDown(MouseButtons button, Point position)
    {
    }

    public void HandleMouseUp(MouseButtons button, Point position)
    {
    }

    public void Dispose()
    {
        _debugFont.Dispose();
    }
}
