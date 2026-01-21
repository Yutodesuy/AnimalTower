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
    private Difficulty _difficulty = Difficulty.Normal;
    private Animal? _currentAnimal;
    private float _aimTimer;
    private readonly List<Animal> _landedAnimals = new();
    private readonly List<SupportBoard> _supportBoards = new();
    private SupportBoard? _currentBoard;
    private float _boardTimer;
    private Point _mousePosition;
    private readonly Floor _floor;
    private readonly Random _random = new();

    public Game(int width, int height)
    {
        _width = Math.Max(1, width);
        _height = Math.Max(1, height);
        _debugFont = new Font("Segoe UI", 14, FontStyle.Bold);

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
        float floorWidth = _width;
        _floor.Friction = 0.5f;

        switch (_difficulty)
        {
            case Difficulty.Easy:
                floorWidth = _width * 0.75f;
                break;
            case Difficulty.Normal:
                floorWidth = _width * 0.5f;
                _floor.Friction = 0.5f;
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
        _aimTimer = 5.0f;
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
            float speed = 10f;
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
        _currentBoard = new SupportBoard(_mousePosition, new SizeF(120, 20));
        _currentBoard.Rotation = 0;
    }

    public void Update(float dt)
    {
        if (_currentState == GameState.PlacingBoard && _currentBoard != null)
        {
            _boardTimer -= dt;
            _currentBoard.Rotation += 180f * dt;
            _currentBoard.Position = _mousePosition;

            if (_boardTimer <= 0)
            {
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

        bool physicsActive = _currentState == GameState.Falling
            || _currentState == GameState.Landed
            || _currentState == GameState.Aiming;

        if (physicsActive)
        {
            var activeBodies = new List<Animal>();
            activeBodies.AddRange(_landedAnimals);

            if (_currentState == GameState.Falling && _currentAnimal != null)
            {
                activeBodies.Add(_currentAnimal);
            }

            foreach (var body in activeBodies)
            {
                body.IsTouchingFloor = false;
            }

            float gravity = 500f;

            foreach (var body in activeBodies)
            {
                if (body.IsStatic) continue;

                body.Velocity = new PointF(body.Velocity.X, body.Velocity.Y + gravity * dt);
                body.Position = new PointF(
                    body.Position.X + body.Velocity.X * dt,
                    body.Position.Y + body.Velocity.Y * dt
                );
                body.Rotation += body.AngularVelocity * dt;
            }

            int iterations = 4;
            bool currentHitSomething = false;

            var allBodies = new List<PhysicsBody>();
            foreach (var body in activeBodies) allBodies.Add(body);
            foreach (var board in _supportBoards) allBodies.Add(board);

            for (int k = 0; k < iterations; k++)
            {
                foreach (var body in activeBodies)
                {
                    if (CheckAndResolveFloorCollision(body, dt))
                    {
                        if (body == _currentAnimal) currentHitSomething = true;
                    }
                }

                for (int i = 0; i < allBodies.Count; i++)
                {
                    for (int j = i + 1; j < allBodies.Count; j++)
                    {
                        var a = allBodies[i];
                        var b = allBodies[j];

                        if (a.IsStatic && b.IsStatic) continue;

                        if (ResolveCollision(a, b, dt))
                        {
                            if (a == _currentAnimal || b == _currentAnimal) currentHitSomething = true;
                        }
                    }
                }
            }

            if (_currentState == GameState.Falling && _currentAnimal != null && currentHitSomething)
            {
                float vSq = _currentAnimal.Velocity.X * _currentAnimal.Velocity.X + _currentAnimal.Velocity.Y * _currentAnimal.Velocity.Y;
                float angSq = _currentAnimal.AngularVelocity * _currentAnimal.AngularVelocity;

                if (vSq < 150 && angSq < 50)
                {
                    _currentState = GameState.Landed;
                    _landedAnimals.Add(_currentAnimal);
                    _currentAnimal = null;

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

            for (int i = activeBodies.Count - 1; i >= 0; i--)
            {
                var body = activeBodies[i];
                if (body.Position.Y > _height + 100)
                {
                    _currentState = GameState.GameOver;
                }
            }

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

    private bool ResolveCollision(PhysicsBody a, PhysicsBody b, float dt)
    {
        var shapesA = a.GetTransformedVertices();
        var shapesB = b.GetTransformedVertices();

        PointF bestNormal = PointF.Empty;
        float bestDepth = 0.0f;
        bool collisionFound = false;

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
                        collisionFound = true;
                    }
                }
            }
        }

        if (!collisionFound) return false;

        float slop = 0.2f;
        float correctionDepth = Math.Max(0, bestDepth - slop);

        if (correctionDepth > 0)
        {
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

        ApplyImpulse(a, b, bestNormal, dt);
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

            if (!Overlap(pA, pB)) return false;

            float axisDepth = Math.Min(pA.Max - pB.Min, pB.Max - pA.Min);
            if (axisDepth < depth)
            {
                depth = axisDepth;
                normal = axis;
            }
        }

        PointF dir = new PointF(centerA.X - centerB.X, centerA.Y - centerB.Y);
        if (Dot(dir, normal) < 0)
        {
            normal = new PointF(-normal.X, -normal.Y);
        }

        return true;
    }

    private bool CheckAndResolveFloorCollision(Animal a, float dt)
    {
        var shapes = a.GetTransformedVertices();
        bool hit = false;
        float maxDepth = 0f;
        PointF deepPoint = PointF.Empty;

        foreach (var vertices in shapes)
        {
            foreach (var p in vertices)
            {
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

            float slop = 0.2f;
            float correctionDepth = Math.Max(0, maxDepth - slop);

            if (correctionDepth > 0)
            {
                a.Position = new PointF(a.Position.X, a.Position.Y - correctionDepth);
            }

            ApplyImpulseStatic(a, deepPoint, normal, _floor.Friction, dt);
            return true;
        }

        return false;
    }

    private void ApplyImpulse(PhysicsBody a, PhysicsBody b, PointF normal, float dt)
    {
        PointF rv = new PointF(a.Velocity.X - b.Velocity.X, a.Velocity.Y - b.Velocity.Y);

        float velAlongNormal = Dot(rv, normal);
        if (velAlongNormal > 0) return;

        float e = Math.Min(a.Restitution, b.Restitution);
        if (Math.Abs(velAlongNormal) < 500f * dt * 2.0f)
        {
            e = 0.0f;
        }

        float j = -(1 + e) * velAlongNormal;
        j /= (1 / a.Mass + 1 / b.Mass);

        PointF impulse = new PointF(normal.X * j, normal.Y * j);

        a.Velocity = new PointF(a.Velocity.X + impulse.X / a.Mass, a.Velocity.Y + impulse.Y / a.Mass);
        b.Velocity = new PointF(b.Velocity.X - impulse.X / b.Mass, b.Velocity.Y - impulse.Y / b.Mass);

        PointF tangent = new PointF(-normal.Y, normal.X);
        float jt = -Dot(rv, tangent);
        jt /= (1 / a.Mass + 1 / b.Mass);

        float mu = (a.Friction + b.Friction) * 0.5f;
        float maxJt = j * mu;
        if (Math.Abs(jt) > maxJt) jt = Math.Sign(jt) * maxJt;

        PointF frictionImpulse = new PointF(tangent.X * jt, tangent.Y * jt);
        a.Velocity = new PointF(a.Velocity.X + frictionImpulse.X / a.Mass, a.Velocity.Y + frictionImpulse.Y / a.Mass);
        b.Velocity = new PointF(b.Velocity.X - frictionImpulse.X / b.Mass, b.Velocity.Y - frictionImpulse.Y / b.Mass);

        float dist = (float)Math.Sqrt(Math.Pow(a.Position.X - b.Position.X, 2) + Math.Pow(a.Position.Y - b.Position.Y, 2));
        if (dist > 1.0f)
        {
            PointF ba = new PointF(a.Position.X - b.Position.X, a.Position.Y - b.Position.Y);
            float torqueVal = ba.X * impulse.Y - ba.Y * impulse.X;
            a.AngularVelocity += (torqueVal * 0.1f) / a.MomentOfInertia;
            b.AngularVelocity -= (torqueVal * 0.1f) / b.MomentOfInertia;
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
                Brush brush = diff == _difficulty ? Brushes.Yellow : Brushes.White;
                if (diff == _difficulty) text = "> " + text + " <";

                SizeF textSize = g.MeasureString(text, subFont);
                g.DrawString(text, subFont, brush, (_width - textSize.Width) / 2, startY);
                startY += textSize.Height + 10;
            }
            return;
        }

        using var floorPen = new Pen(Color.FromArgb(180, 200, 210), 3f);
        float floorStart = (_width - _floor.Width) / 2;
        g.DrawLine(floorPen, floorStart, _floor.Y, floorStart + _floor.Width, _floor.Y);

        void DrawAnimal(Animal animal)
        {
            var shapes = animal.GetTransformedVertices();
            using var brush = new SolidBrush(animal.Color);
            foreach (var vertices in shapes)
            {
                g.FillPolygon(brush, vertices);
                g.DrawPolygon(Pens.Black, vertices);
            }
        }

        foreach (var animal in _landedAnimals)
        {
            DrawAnimal(animal);
        }

        using (var boardBrush = new SolidBrush(Color.BurlyWood))
        {
            foreach (var board in _supportBoards)
            {
                var shapes = board.GetTransformedVertices();
                foreach (var vertices in shapes)
                {
                    g.FillPolygon(boardBrush, vertices);
                    g.DrawPolygon(Pens.SaddleBrown, vertices);
                }
            }
        }

        if (_currentState == GameState.PlacingBoard && _currentBoard != null)
        {
            var shapes = _currentBoard.GetTransformedVertices();
            using var brush = new SolidBrush(Color.FromArgb(180, Color.BurlyWood));
            foreach (var vertices in shapes)
            {
                g.FillPolygon(brush, vertices);
                g.DrawPolygon(Pens.White, vertices);
            }

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

        if (_currentState == GameState.GameOver)
        {
            using var overlayBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
            g.FillRectangle(overlayBrush, 0, 0, _width, _height);

            using var gameOverFont = new Font("Segoe UI", 60, FontStyle.Bold);
            using var scoreFont = new Font("Segoe UI", 30, FontStyle.Bold);
            using var instructFont = new Font("Segoe UI", 20);

            string gameOverText = "GAME OVER";
            SizeF goSize = g.MeasureString(gameOverText, gameOverFont);
            g.DrawString(gameOverText, gameOverFont, Brushes.Red, (_width - goSize.Width) / 2, _height / 3);

            string scoreText = $"Final Score: {_landedAnimals.Count}";
            SizeF scoreSize = g.MeasureString(scoreText, scoreFont);
            g.DrawString(scoreText, scoreFont, Brushes.White, (_width - scoreSize.Width) / 2, _height / 2);

            string instructText = "Press R or Space to Restart";
            SizeF instSize = g.MeasureString(instructText, instructFont);
            g.DrawString(instructText, instructFont, Brushes.LightGray, (_width - instSize.Width) / 2, _height / 2 + 80);
        }
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
            _supportBoards.Add(_currentBoard);
            _currentBoard = null;
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
