using System.Diagnostics;

namespace AnimalTower;

/// <summary>
/// ゲームのメインウィンドウを表すフォームクラスです。
/// ゲームループの管理、描画、入力イベントのハンドリングを行います。
/// </summary>
public sealed class GameForm : Form
{
    // ゲームループを駆動するタイマー（約60FPSを目指す）
    private readonly System.Windows.Forms.Timer _timer;
    // デルタタイム（経過時間）を計測するためのストップウォッチ
    private readonly Stopwatch _stopwatch;
    // ゲームのコアロジックを管理するインスタンス
    private readonly Game _game;

    public GameForm()
    {
        Text = "Animal Tower (Skeleton)";
        ClientSize = new Size(960, 540);
        // 描画のちらつきを抑えるためのダブルバッファリング設定
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

        // ゲームロジックの初期化
        _game = new Game(ClientSize.Width, ClientSize.Height);
        _stopwatch = Stopwatch.StartNew();

        // タイマーの設定と開始
        _timer = new System.Windows.Forms.Timer
        {
            Interval = 16 // 約60FPS
        };
        _timer.Tick += OnTick;
        _timer.Start();

        // イベントハンドラの登録
        Resize += OnResize;
        MouseMove += OnMouseMove;
        MouseDown += OnMouseDown;
        MouseUp += OnMouseUp;
    }

    /// <summary>
    /// キー入力イベントを処理し、ゲームロジックに渡します。
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _game.HandleInput(e.KeyCode);
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

    /// <summary>
    /// 描画イベント。ゲームの描画処理を呼び出します。
    /// </summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        _game.Render(e.Graphics);
    }

    /// <summary>
    /// タイマーのティックイベント。ゲームの更新を行い、再描画を要求します。
    /// </summary>
    private void OnTick(object? sender, EventArgs e)
    {
        // 前回のフレームからの経過時間を計算
        float dt = (float)_stopwatch.Elapsed.TotalSeconds;
        _stopwatch.Restart();

        // ゲーム状態の更新
        _game.Update(dt);
        // 画面の再描画を要求（OnPaintが呼ばれる）
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
