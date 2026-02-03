using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AnimalTower;

/// <summary>
/// FAQ（ヘルプ）画面を管理するクラスです。
/// 質問と回答のリスト表示、展開アニメーション、スクロール処理を担当します。
/// </summary>
public class FAQManager
{
    private class FAQItem
    {
        public string Category { get; set; } = "";
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
        public bool IsExpanded { get; set; } = false;
        public float ExpandAnimation { get; set; } = 0f; // 0 to 1
    }

    private List<FAQItem> _items = new();
    private int _selectedIndex = 0;
    private int _scrollOffset = 0;
    private readonly Font _titleFont = new Font("Segoe UI", 24, FontStyle.Bold);
    private readonly Font _categoryFont = new Font("Segoe UI", 14, FontStyle.Bold);
    private readonly Font _questionFont = new Font("Segoe UI", 12, FontStyle.Bold);
    private readonly Font _answerFont = new Font("Segoe UI", 11);
    private readonly Font _hintFont = new Font("Segoe UI", 10, FontStyle.Italic);

    /// <summary>
    /// FAQ項目を初期化します。
    /// </summary>
    public void Initialize()
    {
        _items.Clear();

        // Rules
        Add("ルール", "このゲームの目的は？", "動物たちをできるだけ高く積み上げることです。\n崩れて画面外に出たり、バランスを崩して落ちたらゲームオーバーです。");
        Add("ルール", "勝利条件は？", "明確な勝利はありません。自分の限界に挑戦するスコアアタック形式です。\n友達や過去の自分とスコア（積み上げた数）を競いましょう。");
        Add("ルール", "難易度の違いは？", "【Easy】床が広く、摩擦が強く、安定しやすいです。\n【Normal】標準的な設定です。\n【Hard】床が非常に狭く(30%)、摩擦も低いため、慎重な操作が必要です。");

        // Controls
        Add("操作方法", "動物の移動", "【← / →】キーで左右に移動します。\nマウス操作には対応していません（動物の落下時）。");
        Add("操作方法", "動物を落とす", "【Space】または【Enter】キーで落下させます。\n一度落下を始めると、操作はできません。");
        Add("操作方法", "サポート板の設置", "一定数動物を積むと「板（Board）」モードになります。\nマウスで位置を決め、クリックで設置します。\n板は回転しながら落下するため、タイミングが重要です。");
        Add("操作方法", "回転させるには？", "直接回転させるキーはありません。\n物理演算により、他の動物にぶつかったり、重心のズレで自然に回転します。");

        // Tips
        Add("コツ", "動物が滑る！", "摩擦の低い設定や、丸い動物（ヒヨコなど）は滑りやすいです。\n完全に静止するまで次の動物を落とさないのがコツです。");
        Add("コツ", "「Landed」にならない", "動物が激しく動いている間は次のターンに進みません。\n3秒経過すると強制的に判定されます。");
        Add("コツ", "板（Board）の使い方", "デコボコした足場を平らにしたり、\n崩れそうなタワーを支える土台として使いましょう。");
    }

    private void Add(string category, string q, string a)
    {
        _items.Add(new FAQItem { Category = category, Question = q, Answer = a });
    }

    /// <summary>
    /// アニメーションの更新を行います。
    /// </summary>
    public void Update(float dt)
    {
        // 展開アニメーションの更新
        foreach (var item in _items)
        {
            float target = item.IsExpanded ? 1f : 0f;
            item.ExpandAnimation += (target - item.ExpandAnimation) * 10f * dt;
        }
    }

    /// <summary>
    /// FAQ画面でのキー入力を処理します（選択、展開、スクロール）。
    /// </summary>
    public void HandleInput(Keys key)
    {
        switch (key)
        {
            case Keys.Up:
                _selectedIndex--;
                if (_selectedIndex < 0) _selectedIndex = _items.Count - 1;
                AdjustScroll();
                break;
            case Keys.Down:
                _selectedIndex++;
                if (_selectedIndex >= _items.Count) _selectedIndex = 0;
                AdjustScroll();
                break;
            case Keys.Enter:
            case Keys.Space:
            case Keys.Right:
                _items[_selectedIndex].IsExpanded = !_items[_selectedIndex].IsExpanded;
                break;
            case Keys.Left:
                if (_items[_selectedIndex].IsExpanded)
                    _items[_selectedIndex].IsExpanded = false;
                break;
        }
    }

    private void AdjustScroll()
    {
        // 単純なスクロール制御
        if (_selectedIndex < _scrollOffset) _scrollOffset = _selectedIndex;
        if (_selectedIndex > _scrollOffset + 5) _scrollOffset = _selectedIndex - 5;
    }

    /// <summary>
    /// FAQ画面を描画します。
    /// </summary>
    public void Render(Graphics g, int width, int height)
    {
        // 背景の描画
        using (var bgBrush = new LinearGradientBrush(new Point(0,0), new Point(0, height),
            Color.FromArgb(40, 44, 55), Color.FromArgb(20, 24, 30)))
        {
            g.FillRectangle(bgBrush, 0, 0, width, height);
        }

        // タイトルの描画
        g.DrawString("HELP / FAQ", _titleFont, Brushes.White, 40, 30);
        g.DrawString("Esc / Backspace: 戻る", _hintFont, Brushes.LightGray, width - 200, 45);

        float startY = 100;
        float x = 50;
        float contentWidth = width - 100;
        float itemHeightBase = 40;

        int itemsToShow = _items.Count;
        int startIndex = _scrollOffset;

        float drawY = 100;

        for (int i = startIndex; i < _items.Count; i++)
        {
            var item = _items[i];
            float itemH = 40;
            float expandedH = 0;

            if (item.ExpandAnimation > 0.01f)
            {
                // 回答部分の高さを計算
                SizeF size = g.MeasureString(item.Answer, _answerFont, (int)contentWidth - 20);
                expandedH = (size.Height + 20) * item.ExpandAnimation;
            }

            // 画面外なら描画終了
            if (drawY > height) break;

            // 項目の背景描画
            RectangleF rect = new RectangleF(x, drawY, contentWidth, itemH + expandedH);
            bool isSelected = (i == _selectedIndex);

            using (var brush = new SolidBrush(isSelected ? Color.FromArgb(80, 100, 140) : Color.FromArgb(50, 55, 65)))
            {
                g.FillRectangle(brush, rect);
                g.DrawRectangle(Pens.Gray, rect.X, rect.Y, rect.Width, rect.Height);
            }

            if (isSelected)
            {
                 g.DrawRectangle(Pens.Cyan, rect.X, rect.Y, rect.Width, rect.Height);
                 // 選択中のマーカー
                 g.FillRectangle(Brushes.Cyan, x, drawY, 5, itemH + expandedH);
            }

            // カテゴリの描画
            SizeF catSize = g.MeasureString(item.Category, _hintFont);
            g.DrawString(item.Category, _hintFont, Brushes.LightSkyBlue, x + 15, drawY + 5);

            // 質問の描画
            g.DrawString(item.Question, _questionFont, Brushes.White, x + 15 + catSize.Width + 10, drawY + 8);

            // 回答の描画（クリッピング使用）
            if (expandedH > 0)
            {
                RectangleF textRect = new RectangleF(x + 20, drawY + itemH, contentWidth - 40, expandedH - 10);
                Region r = g.Clip;
                g.SetClip(textRect);
                g.DrawString(item.Answer, _answerFont, Brushes.Gainsboro, textRect);
                g.Clip = r;
            }

            drawY += itemH + expandedH + 10; // スペースを空ける
        }
    }

    public void Dispose()
    {
        _titleFont.Dispose();
        _categoryFont.Dispose();
        _questionFont.Dispose();
        _answerFont.Dispose();
        _hintFont.Dispose();
    }
}
