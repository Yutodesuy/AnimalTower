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
        // MVP: Simple 40x40 box
        float startX = _width / 2f;
        float startY = 60f;
        _currentAnimal = new Animal(new PointF(startX, startY), new SizeF(40, 40));
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
            // Simplified "Landed" logic: if it hit something and is moving slowly.
            if (collisionOccurred)
            {
                float vSq = _currentAnimal.Velocity.X * _currentAnimal.Velocity.X + _currentAnimal.Velocity.Y * _currentAnimal.Velocity.Y;
                float angSq = _currentAnimal.AngularVelocity * _currentAnimal.AngularVelocity;

                if (vSq < 50 && angSq < 10)
                {
                   // _currentState = GameState.Landed;
                   // Logic to freeze it? For now, we keep simulating physics until it truly stops or user spawns new?
                   // The original game spawned a new one immediately on hit.
                   // Let's implement a delay or check if it's stable.
                   // For MVP rigid body: just Land it if it hits the floor or stack and is moving down.
                }
            }

            // If it fell off screen
            if (_currentAnimal.Position.Y > _height + 100)
            {
                _currentState = GameState.GameOver;
            }

            // Hack for "Landed": If it stays roughly in place for a bit, or if we want to allow stacking.
            // The original code landed immediately on AABB intersection.
            // With rigid body, we want it to settle.
            // BUT, the prompt implies "stacking".
            // Let's settle for: If velocity is low after collision, freeze it.
            if (collisionOccurred)
            {
                 float vSq = _currentAnimal.Velocity.X * _currentAnimal.Velocity.X + _currentAnimal.Velocity.Y * _currentAnimal.Velocity.Y;
                 if (vSq < 100) // Threshold
                 {
                     _currentState = GameState.Landed;
                     _landedAnimals.Add(_currentAnimal);
                     _currentAnimal = null;
                     SpawnAnimal();
                 }
            }
        }
    }

    // --- PHYSICS HELPERS ---

    // Separating Axis Theorem (SAT)
    // Returns true if collision resolved
    private bool ResolveCollision(Animal a, PhysicsBody b)
    {
        PointF[] shapeA = a.GetCorners();
        PointF[] shapeB = b.GetCorners();

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
        // Simple Floor check: Look for corners below floor Y
        PointF[] corners = a.GetCorners();
        bool hit = false;

        // Find deepest point
        float maxY = float.MinValue;
        PointF deepPoint = PointF.Empty;

        foreach (var p in corners)
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
        // Contact point estimation (simplified: use center + radius approx or just mid point)
        // For accurate physics, we need the exact contact point.
        // Approximating contact point as the midpoint of the overlap or just using A's closest corner.
        // Let's use A's position + radius in direction of -normal as rough contact.

        // BETTER: Use relative velocity at contact.
        // Let's assume contact point 'r' relative to Center of Mass.
        // Simplifying for MVP: Treat as linear collision for impulse, but apply some torque based on offset.

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

        // Angular Impulse (Simplified)
        // Torque = r x F.
        // We need 'r'. approximating r as (Contact - Center).
        // Since we didn't calculate exact contact point in SAT, we can try to guess or skip precise torque for MVP.
        // HACK: Add random small rotation or based on X offset?
        // Let's try to do it right. Find vector from center to impact.
        // Impact roughly at a.Pos - normal * size.
        // rA = contact - a.Pos
        // rB = contact - b.Pos
        // This requires the contact point.

        // Heuristic for Rotation:
        // If normal is (0, -1) (Vertical hit), check X offset.
        // r x n gives torque direction.
        // Let's assume contact is on the surface.
        // This is getting complex for a single function without a ContactPoint solver.

        // Fallback: Induce rotation based on velocity difference at edges?
        // Or just apply a simple "If not centered, rotate" logic.

        // Let's implement a fake torque based on horizontal offset if hitting top/bottom.
        if (Math.Abs(normal.Y) > 0.8f) // Vertical hit
        {
            float xOffset = a.Position.X - b.Position.X; // Relative X
            float torque = -xOffset * j * 0.1f; // Fake lever arm
            a.AngularVelocity += torque / a.MomentOfInertia;
            b.AngularVelocity -= torque / b.MomentOfInertia;
        }
    }

    private void ApplyImpulseStatic(Animal a, PointF contactPoint, PointF normal, float frictionCoeff)
    {
        // r = vector from COM to contact point
        PointF r = new PointF(contactPoint.X - a.Position.X, contactPoint.Y - a.Position.Y);

        // Relative velocity at contact point: V_p = V_cm + omega x r
        // 2D Cross product of scalar omega and vector r is (-omega * r.y, omega * r.x)
        PointF vRel = new PointF(
            a.Velocity.X + (-a.AngularVelocity * 0.01745f * r.Y), // Deg to Rad approx needed? AngularVelocity is usually rad/s or deg/s?
            // In PhysicsBody I said Rotation is deg. Let's assume AngVel is deg/s.
            // Physics formulas need radians.
            a.Velocity.Y + (a.AngularVelocity * 0.01745f * r.X)
        );

        float velAlongNormal = Dot(vRel, normal);

        if (velAlongNormal > 0) return;

        // Impulse scalar
        // J = -(1+e) * vRel.n / (1/m + (r x n)^2 / I)
        float rCrossN = r.X * normal.Y - r.Y * normal.X; // 2D Cross product
        float invMass = 1.0f / a.Mass;
        float invI = 1.0f / a.MomentOfInertia;

        // Convert I to handle radians? No, I is Mass*Dist^2.
        // The cross product term needs to be consistent.
        // AngVel in Rad/s.

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
        void DrawAnimal(Animal animal, Brush brush)
        {
            // Save state
            var state = g.Save();

            // Translate to center, rotate, then translate back
            g.TranslateTransform(animal.Position.X, animal.Position.Y);
            g.RotateTransform(animal.Rotation);

            // Draw relative to center (0,0)
            float hw = animal.Size.Width / 2;
            float hh = animal.Size.Height / 2;

            g.FillRectangle(brush, -hw, -hh, animal.Size.Width, animal.Size.Height);
            g.DrawRectangle(Pens.Black, -hw, -hh, animal.Size.Width, animal.Size.Height);

            // Restore
            g.Restore(state);
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
