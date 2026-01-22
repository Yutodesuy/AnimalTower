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
        PlacingBoard,
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
    private readonly List<SupportBoard> _supportBoards = new();
    private SupportBoard? _currentBoard;
    private float _boardTimer;
    private Point _mousePosition;
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
        _supportBoards.Clear();
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

        // Randomly select one of the 10 animals
        int animalType = _random.Next(10);
        PointF pos = new PointF(startX, startY);

        switch (animalType)
        {
            case 0: _currentAnimal = Animal.Factory.CreateElephant(pos); break;
            case 1: _currentAnimal = Animal.Factory.CreateGiraffe(pos); break;
            case 2: _currentAnimal = Animal.Factory.CreateHippo(pos); break;
            case 3: _currentAnimal = Animal.Factory.CreateRhino(pos); break;
            case 4: _currentAnimal = Animal.Factory.CreateLion(pos); break;
            case 5: _currentAnimal = Animal.Factory.CreatePanda(pos); break;
            case 6: _currentAnimal = Animal.Factory.CreateRabbit(pos); break;
            case 7: _currentAnimal = Animal.Factory.CreateCat(pos); break;
            case 8: _currentAnimal = Animal.Factory.CreateChick(pos); break;
            case 9: _currentAnimal = Animal.Factory.CreateTurtle(pos); break;
            default: _currentAnimal = Animal.Factory.CreateElephant(pos); break;
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

    private void StartBoardPlacement()
    {
        _currentState = GameState.PlacingBoard;
        _boardTimer = 10.0f;

        // Difficulty Settings
        float scale = 1.0f;
        List<BoardShape> availableShapes = new List<BoardShape> { BoardShape.Rectangle };

        if (_difficulty == Difficulty.Normal)
        {
            scale = 0.5f;
            availableShapes.Add(BoardShape.Triangle);
            availableShapes.Add(BoardShape.Star);
        }
        else if (_difficulty == Difficulty.Hard)
        {
            scale = 0.25f; // 0.5 * 0.5
            availableShapes.Add(BoardShape.Triangle);
            availableShapes.Add(BoardShape.Star);
        }

        // Pick Shape
        BoardShape shape = availableShapes[_random.Next(availableShapes.Count)];

        // Determine Size
        SizeF size;
        if (shape == BoardShape.Rectangle)
        {
            size = new SizeF(120 * scale, 20 * scale);
        }
        else
        {
            // Square aspect for non-rect shapes to avoid severe distortion
            // Use 60 (half of 120 width) as base dimension
            float dim = 60 * scale;
            size = new SizeF(dim, dim);
        }

        _currentBoard = new SupportBoard(_mousePosition, size, shape);
        _currentBoard.Rotation = 0;
    }

    public void Update(float dt)
    {
        if (_currentState == GameState.PlacingBoard && _currentBoard != null)
        {
            _boardTimer -= dt;

            // Continuous rotation
            _currentBoard.Rotation += 180f * dt; // 180 deg/sec

            // Move to mouse (interpolation or direct set handled in MouseMove, but good to refresh here if needed)
            _currentBoard.Position = _mousePosition;

            if (_boardTimer <= 0)
            {
                // Time's up - cancel placement
                _currentBoard = null;
                SpawnAnimal();
            }
        }

        if (_currentState == GameState.Aiming && _currentAnimal != null)
        {
            _aimTimer -= dt;
            if (_aimTimer <= 0)
            {
                _currentState = GameState.Falling;
                _aimTimer = 0;
            }
        }

        bool physicsActive = (_currentState == GameState.Falling || _currentState == GameState.Landed || _currentState == GameState.Aiming);

        if (physicsActive)
        {
            // Collect all bodies participating in physics
            var activeBodies = new List<Animal>();
            activeBodies.AddRange(_landedAnimals);

            if (_currentState == GameState.Falling && _currentAnimal != null)
            {
                activeBodies.Add(_currentAnimal);
            }

            // Reset floor touch flags
            foreach (var body in activeBodies)
            {
                body.IsTouchingFloor = false;
            }

            float gravity = 500f; // pixels/s^2

            // 1. Apply Gravity & Predict Position (Integration)
            foreach (var body in activeBodies)
            {
                if (body.IsStatic) continue;

                // Velocity Integration
                body.Velocity = new PointF(body.Velocity.X, body.Velocity.Y + gravity * dt);

                // Position Integration
                body.Position = new PointF(
                    body.Position.X + body.Velocity.X * dt,
                    body.Position.Y + body.Velocity.Y * dt
                );

                // Rotation Integration
                body.Rotation += body.AngularVelocity * dt;
            }

            // 2. Iterative Collision Solver
            int iterations = 4;
            bool currentHitSomething = false;

            // Combine active bodies with static support boards for collision checks
            var allBodies = new List<PhysicsBody>(activeBodies.Cast<PhysicsBody>());
            allBodies.AddRange(_supportBoards);

            for (int k = 0; k < iterations; k++)
            {
                // Floor Collisions (Only for active dynamic bodies)
                foreach (var body in activeBodies)
                {
                    if (CheckAndResolveFloorCollision(body, dt))
                    {
                        if (body == _currentAnimal) currentHitSomething = true;
                    }
                }

                // Body vs Body Collisions
                for (int i = 0; i < allBodies.Count; i++)
                {
                    for (int j = i + 1; j < allBodies.Count; j++)
                    {
                        var a = allBodies[i];
                        var b = allBodies[j];

                        // Skip static vs static
                        if (a.IsStatic && b.IsStatic) continue;

                        if (ResolveCollision(a, b, dt))
                        {
                            if (a == _currentAnimal || b == _currentAnimal) currentHitSomething = true;
                        }
                    }
                }
            }

            // 3. Game Logic: Check landing condition for _currentAnimal
            if (_currentState == GameState.Falling && _currentAnimal != null && currentHitSomething)
            {
                float vSq = _currentAnimal.Velocity.X * _currentAnimal.Velocity.X + _currentAnimal.Velocity.Y * _currentAnimal.Velocity.Y;
                float angSq = _currentAnimal.AngularVelocity * _currentAnimal.AngularVelocity;

                // Thresholds: velocity squared < 150 (approx 12 px/s) and angular velocity squared < 50 (~7 deg/s)
                if (vSq < 150 && angSq < 50)
                {
                    _currentState = GameState.Landed;
                    _landedAnimals.Add(_currentAnimal);
                    _currentAnimal = null;

                    // Trigger "Mini Plank" every 5 landed animals
                    if (_landedAnimals.Count > 0 && _landedAnimals.Count % 5 == 0)
                    {
                        StartBoardPlacement();
                    }
                    else
                    {
                        SpawnAnimal();
                    }
                }
            }

            // Check for Game Over (falling off screen)
            for (int i = activeBodies.Count - 1; i >= 0; i--)
            {
                var body = activeBodies[i];
                if (body.Position.Y > _height + 100)
                {
                    if (body == _currentAnimal)
                    {
                        _currentState = GameState.GameOver;
                    }
                    else
                    {
                        _currentState = GameState.GameOver;
                    }
                }
            }

            // Update Floor Contact Timers & Elasticity
            foreach (var body in activeBodies)
            {
                if (body.IsTouchingFloor)
                {
                    body.FloorContactTime += dt;
                    if (body.FloorContactTime >= 2.0f)
                    {
                        body.Restitution = 0.0f;
                    }
                }
                else
                {
                    body.FloorContactTime = 0.0f;
                }
            }
        }
    }

    // --- PHYSICS HELPERS ---

    // Separating Axis Theorem (SAT)
    // Returns true if collision resolved
    private bool ResolveCollision(PhysicsBody a, PhysicsBody b, float dt)
    {
        // Get all shapes for both bodies
        var shapesA = a.GetTransformedVertices();
        var shapesB = b.GetTransformedVertices();

        PointF bestNormal = PointF.Empty;
        float bestDepth = 0.0f;
        PointF[]? bestShapeA = null;
        PointF[]? bestShapeB = null;
        bool collisionFound = false;

        // Iterate through ALL shape pairs to find the deepest penetration
        foreach (var shapeA in shapesA)
        {
            foreach (var shapeB in shapesB)
            {
                PointF normal;
                float depth;
                if (CheckCollisionSAT(shapeA, shapeB, a.Position, b.Position, out normal, out depth))
                {
                    if (depth > bestDepth)
                    {
                        bestDepth = depth;
                        bestNormal = normal;
                        bestShapeA = shapeA;
                        bestShapeB = shapeB;
                        collisionFound = true;
                    }
                }
            }
        }

        if (!collisionFound || bestShapeA == null || bestShapeB == null) return false;

        // Determine Contact Point (Approximate)
        PointF contactPoint;
        PointF vA = GetSupport(bestShapeA, new PointF(-bestNormal.X, -bestNormal.Y));
        PointF vB = GetSupport(bestShapeB, bestNormal);

        // Simple heuristic for contact point
        bool aInB = IsPointInPolygon(vA, bestShapeB);
        bool bInA = IsPointInPolygon(vB, bestShapeA);

        if (aInB && bInA)
        {
             contactPoint = new PointF((vA.X + vB.X) / 2, (vA.Y + vB.Y) / 2);
        }
        else if (aInB)
        {
             contactPoint = vA;
        }
        else if (bInA)
        {
             contactPoint = vB;
        }
        else
        {
             // Edge-Edge or just touching
             contactPoint = new PointF((vA.X + vB.X) / 2, (vA.Y + vB.Y) / 2);
        }

        // Slop (Tolerance)
        float slop = 0.2f;
        float correctionDepth = Math.Max(0, bestDepth - slop);

        if (correctionDepth > 0)
        {
            // Resolve Penetration based on Inverse Mass
            float invMassA = a.IsStatic ? 0 : 1.0f / a.Mass;
            float invMassB = b.IsStatic ? 0 : 1.0f / b.Mass;
            float totalInvMass = invMassA + invMassB;

            if (totalInvMass > 0)
            {
                float movePerIM = correctionDepth / totalInvMass;

                if (!a.IsStatic)
                {
                    a.Position = new PointF(
                        a.Position.X + bestNormal.X * movePerIM * invMassA,
                        a.Position.Y + bestNormal.Y * movePerIM * invMassA
                    );
                }

                if (!b.IsStatic)
                {
                    b.Position = new PointF(
                        b.Position.X - bestNormal.X * movePerIM * invMassB,
                        b.Position.Y - bestNormal.Y * movePerIM * invMassB
                    );
                }
            }
        }

        // Compute Impulse
        ApplyImpulse(a, b, bestNormal, contactPoint, dt);

        return true;
    }

    private bool CheckCollisionSAT(PointF[] shapeA, PointF[] shapeB, PointF centerA, PointF centerB, out PointF normal, out float depth)
    {
        normal = PointF.Empty;
        depth = float.MaxValue;

        List<PointF> axes = new List<PointF>();
        axes.AddRange(GetAxes(shapeA));
        axes.AddRange(GetAxes(shapeB));

        foreach (var axis in axes)
        {
            var pA = Project(shapeA, axis);
            var pB = Project(shapeB, axis);

            if (!Overlap(pA, pB)) return false; // No collision on this axis

            float axisDepth = Math.Min(pA.Max - pB.Min, pB.Max - pA.Min);
            if (axisDepth < depth)
            {
                depth = axisDepth;
                normal = axis;
            }
        }

        // Ensure normal points from B to A
        PointF dir = new PointF(centerA.X - centerB.X, centerA.Y - centerB.Y);
        if (Dot(dir, normal) < 0)
        {
            normal = new PointF(-normal.X, -normal.Y);
        }

        return true;
    }

    private bool CheckAndResolveFloorCollision(Animal a, float dt)
    {
        // Floor check: Look for any vertex below floor Y in ANY shape
        var shapes = a.GetTransformedVertices();
        bool hit = false;
        float maxDepth = 0f;
        PointF deepPoint = PointF.Empty;

        foreach (var vertices in shapes)
        {
            foreach (var p in vertices)
            {
                // Check lateral bounds
                float floorStart = (_width - _floor.Width) / 2;
                float floorEnd = floorStart + _floor.Width;
                bool withinX = p.X >= floorStart && p.X <= floorEnd;

                if (p.Y > _floor.Y && withinX)
                {
                    hit = true;
                    float currentDepth = p.Y - _floor.Y;
                    if (currentDepth > maxDepth)
                    {
                        maxDepth = currentDepth;
                        deepPoint = p;
                    }
                }
            }
        }

        if (hit)
        {
            a.IsTouchingFloor = true;

            PointF normal = new PointF(0, -1);

            // Slop
            float slop = 0.2f;
            float correctionDepth = Math.Max(0, maxDepth - slop);

            if (correctionDepth > 0)
            {
                // Penetration resolution
                a.Position = new PointF(a.Position.X, a.Position.Y - correctionDepth);
            }

            // Impulse
            ApplyImpulseStatic(a, deepPoint, normal, _floor.Friction, dt);
            return true;
        }

        return false;
    }

    private void ApplyImpulse(PhysicsBody a, PhysicsBody b, PointF normal, PointF contactPoint, float dt)
    {
        PointF rA = new PointF(contactPoint.X - a.Position.X, contactPoint.Y - a.Position.Y);
        PointF rB = new PointF(contactPoint.X - b.Position.X, contactPoint.Y - b.Position.Y);

        PointF rv = new PointF(
            (a.Velocity.X - a.AngularVelocity * 0.01745f * rA.Y) - (b.Velocity.X - b.AngularVelocity * 0.01745f * rB.Y),
            (a.Velocity.Y + a.AngularVelocity * 0.01745f * rA.X) - (b.Velocity.Y + b.AngularVelocity * 0.01745f * rB.X)
        );

        float velAlongNormal = Dot(rv, normal);

        if (velAlongNormal > 0) return;

        float raCrossN = rA.X * normal.Y - rA.Y * normal.X;
        float rbCrossN = rB.X * normal.Y - rB.Y * normal.X;

        float invMassA = a.IsStatic ? 0 : 1.0f / a.Mass;
        float invMassB = b.IsStatic ? 0 : 1.0f / b.Mass;
        float invIA = a.IsStatic ? 0 : 1.0f / a.MomentOfInertia;
        float invIB = b.IsStatic ? 0 : 1.0f / b.MomentOfInertia;

        float effectiveMass = invMassA + invMassB +
                              (raCrossN * raCrossN) * invIA +
                              (rbCrossN * rbCrossN) * invIB;

        float e = Math.Min(a.Restitution, b.Restitution);
        if (Math.Abs(velAlongNormal) < 500f * dt * 2.0f) e = 0.0f;

        float j = -(1 + e) * velAlongNormal / effectiveMass;

        PointF impulse = new PointF(normal.X * j, normal.Y * j);

        if (!a.IsStatic)
        {
             a.Velocity = new PointF(a.Velocity.X + impulse.X * invMassA, a.Velocity.Y + impulse.Y * invMassA);
             a.AngularVelocity += (rA.X * impulse.Y - rA.Y * impulse.X) * invIA * 57.29f;
        }
        if (!b.IsStatic)
        {
             b.Velocity = new PointF(b.Velocity.X - impulse.X * invMassB, b.Velocity.Y - impulse.Y * invMassB);
             b.AngularVelocity -= (rB.X * impulse.Y - rB.Y * impulse.X) * invIB * 57.29f;
        }

        // Friction
        PointF tangent = new PointF(-normal.Y, normal.X);
        float rtA = rA.X * tangent.Y - rA.Y * tangent.X;
        float rtB = rB.X * tangent.Y - rB.Y * tangent.X;

        float effMassT = invMassA + invMassB + (rtA * rtA) * invIA + (rtB * rtB) * invIB;

        float jt = -Dot(rv, tangent) / effMassT;

        float mu = (a.Friction + b.Friction) * 0.5f;
        float maxJt = j * mu;
        if (Math.Abs(jt) > maxJt) jt = Math.Sign(jt) * maxJt;

        PointF frictionImpulse = new PointF(tangent.X * jt, tangent.Y * jt);

        if (!a.IsStatic)
        {
             a.Velocity = new PointF(a.Velocity.X + frictionImpulse.X * invMassA, a.Velocity.Y + frictionImpulse.Y * invMassA);
             a.AngularVelocity += (rA.X * frictionImpulse.Y - rA.Y * frictionImpulse.X) * invIA * 57.29f;
        }
        if (!b.IsStatic)
        {
             b.Velocity = new PointF(b.Velocity.X - frictionImpulse.X * invMassB, b.Velocity.Y - frictionImpulse.Y * invMassB);
             b.AngularVelocity -= (rB.X * frictionImpulse.Y - rB.Y * frictionImpulse.X) * invIB * 57.29f;
        }
    }

    private void ApplyImpulseStatic(Animal a, PointF contactPoint, PointF normal, float frictionCoeff, float dt)
    {
        PointF r = new PointF(contactPoint.X - a.Position.X, contactPoint.Y - a.Position.Y);

        PointF vRel = new PointF(
            a.Velocity.X + (-a.AngularVelocity * 0.01745f * r.Y),
            a.Velocity.Y + (a.AngularVelocity * 0.01745f * r.X)
        );

        float velAlongNormal = Dot(vRel, normal);

        if (velAlongNormal > 0) return;

        float rCrossN = r.X * normal.Y - r.Y * normal.X;
        float invMass = 1.0f / a.Mass;
        float invI = 1.0f / a.MomentOfInertia;

        float effectiveMass = invMass + (rCrossN * rCrossN) * invI;

        float e = a.Restitution;

        if (Math.Abs(velAlongNormal) < 500f * dt * 2.0f)
        {
            e = 0.0f;
        }

        float j = -(1 + e) * velAlongNormal / effectiveMass;

        PointF impulse = new PointF(normal.X * j, normal.Y * j);

        a.Velocity = new PointF(a.Velocity.X + impulse.X * invMass, a.Velocity.Y + impulse.Y * invMass);

        float torque = r.X * impulse.Y - r.Y * impulse.X;
        float angAccel = torque * invI;
        a.AngularVelocity += angAccel * 57.29f;

        // Friction
        PointF tangent = new PointF(-normal.Y, normal.X);
        float vRelTangent = Dot(vRel, tangent);

        float rCrossT = r.X * tangent.Y - r.Y * tangent.X;
        float effectiveMassT = invMass + (rCrossT * rCrossT) * invI;

        float jt = -vRelTangent / effectiveMassT;

        float maxJt = j * (a.Friction + frictionCoeff) * 0.5f;
        if (Math.Abs(jt) > maxJt) jt = Math.Sign(jt) * maxJt;

        PointF frictionImpulse = new PointF(tangent.X * jt, tangent.Y * jt);

        a.Velocity = new PointF(a.Velocity.X + frictionImpulse.X * invMass, a.Velocity.Y + frictionImpulse.Y * invMass);

        float torqueF = r.X * frictionImpulse.Y - r.Y * frictionImpulse.X;
        a.AngularVelocity += (torqueF * invI) * 57.29f;
    }

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

    private PointF GetSupport(PointF[] vertices, PointF dir)
    {
        float maxDot = float.MinValue;
        PointF bestV = PointF.Empty;
        foreach (var v in vertices)
        {
            float dot = v.X * dir.X + v.Y * dir.Y;
            if (dot > maxDot)
            {
                maxDot = dot;
                bestV = v;
            }
        }
        return bestV;
    }

    private bool IsPointInPolygon(PointF p, PointF[] poly)
    {
        int crossings = 0;
        for (int i = 0; i < poly.Length; i++)
        {
             PointF a = poly[i];
             PointF b = poly[(i + 1) % poly.Length];
             if ((a.Y > p.Y) != (b.Y > p.Y))
             {
                 float intersectX = (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X;
                 if (p.X < intersectX) crossings++;
             }
        }
        return (crossings % 2) != 0;
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
            var state = g.Save();

            // Get List of shapes (List<PointF[]>)
            var shapes = animal.GetTransformedVertices();

            using (var brush = new SolidBrush(animal.Color))
            {
                foreach (var vertices in shapes)
                {
                    g.FillPolygon(brush, vertices);
                    g.DrawPolygon(Pens.Black, vertices);
                }
            }
            // Optional: Draw Eye or detail?
            // Since we don't store "Eye Position" relative to shape, we skip for now.
            // But we could add a simple "Eye" relative to the first shape's center or similar later.

            g.Restore(state);
        }

        foreach (var animal in _landedAnimals)
        {
            DrawAnimal(animal);
        }

        // Draw Support Boards
        using (var boardBrush = new SolidBrush(Color.BurlyWood))
        {
            foreach (var board in _supportBoards)
            {
                var state = g.Save();
                var shapes = board.GetTransformedVertices();
                foreach (var vertices in shapes)
                {
                    g.FillPolygon(boardBrush, vertices);
                    g.DrawPolygon(Pens.SaddleBrown, vertices);
                }
                g.Restore(state);
            }
        }

        // Draw Current Board being placed
        if (_currentState == GameState.PlacingBoard && _currentBoard != null)
        {
            var state = g.Save();
            var shapes = _currentBoard.GetTransformedVertices();
            using (var brush = new SolidBrush(Color.FromArgb(180, Color.BurlyWood))) // Semi-transparent
            {
                foreach (var vertices in shapes)
                {
                    g.FillPolygon(brush, vertices);
                    g.DrawPolygon(Pens.White, vertices);
                }
            }
            g.Restore(state);

            // Draw Timer
            string timerText = $"PLACE BOARD: {_boardTimer:0.0}";
            SizeF size = g.MeasureString(timerText, _debugFont);
            g.DrawString(timerText, _debugFont, Brushes.LightGreen, (_width - size.Width) / 2, 100);
        }

        if (_currentAnimal != null)
        {
            DrawAnimal(_currentAnimal);
        }

        if (_currentState == GameState.Aiming)
        {
            string timerText = $"{_aimTimer:0.0}";
            SizeF size = g.MeasureString(timerText, _debugFont);
            g.DrawString(timerText, _debugFont, Brushes.Orange, (_width - size.Width) / 2, 100);
        }

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
        _mousePosition = position;
        if (_currentState == GameState.PlacingBoard && _currentBoard != null)
        {
            _currentBoard.Position = new PointF(position.X, position.Y);
        }
    }

    public void HandleMouseDown(MouseButtons button, Point position)
    {
        if (_currentState == GameState.PlacingBoard && button == MouseButtons.Left && _currentBoard != null)
        {
            // Place the board
            _supportBoards.Add(_currentBoard);
            _currentBoard = null;

            // Resume Game
            SpawnAnimal();
        }
    }

    public void HandleMouseUp(MouseButtons button, Point position)
    {
    }

    public void Dispose()
    {
        _debugFont.Dispose();
    }
}
