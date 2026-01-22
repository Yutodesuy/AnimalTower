using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AnimalTower;

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

    public void Update(float dt)
    {
        // Simple animation logic for expansion
        foreach (var item in _items)
        {
            float target = item.IsExpanded ? 1f : 0f;
            item.ExpandAnimation += (target - item.ExpandAnimation) * 10f * dt;
        }
    }

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
        // Basic keep-in-view logic would go here if list is long
        // For now, simple centering or fixed offset
        if (_selectedIndex < _scrollOffset) _scrollOffset = _selectedIndex;
        if (_selectedIndex > _scrollOffset + 5) _scrollOffset = _selectedIndex - 5;
    }

    public void Render(Graphics g, int width, int height)
    {
        // Background
        using (var bgBrush = new LinearGradientBrush(new Point(0,0), new Point(0, height),
            Color.FromArgb(40, 44, 55), Color.FromArgb(20, 24, 30)))
        {
            g.FillRectangle(bgBrush, 0, 0, width, height);
        }

        // Title
        g.DrawString("HELP / FAQ", _titleFont, Brushes.White, 40, 30);
        g.DrawString("Esc / Backspace: 戻る", _hintFont, Brushes.LightGray, width - 200, 45);

        float startY = 100;
        float x = 50;
        float contentWidth = width - 100;
        float itemHeightBase = 40;

        // Draw List
        for (int i = 0; i < _items.Count; i++)
        {
            // Simple scrolling: Skip items outside view?
            // For this implementation, we just draw everything with offset
            // But to keep it simple and robust, let's just draw list.

            float y = startY + (i - _scrollOffset) * (itemHeightBase + 5);
            // Note: This simple formula doesn't account for expanded height shifting subsequent items.
            // We need a proper layout pass.
        }

        // Correct Layout Pass
        float currentY = startY;
        // Only draw a subset or clamp? Let's implement scrolling by shifting Y.
        // Better: calculate total height and shift `currentY` by `-_scrollOffset * itemHeight`.
        // Actually, let's just iterate and draw, shifting Y as we go.

        // To handle scrolling with variable heights (expanded items), we need to track visual position.
        // Let's force focus to center the selected item if possible, or just standard scroll.
        // Simplest: Always draw from top, but shift up based on _scrollOffset (pixels).
        // But _scrollOffset was index-based. Let's make it index-based "top item".

        int itemsToShow = _items.Count; // Draw all, clip by screen
        int startIndex = _scrollOffset;

        float drawY = 100;

        for (int i = startIndex; i < _items.Count; i++)
        {
            var item = _items[i];
            float itemH = 40;
            float expandedH = 0;

            if (item.ExpandAnimation > 0.01f)
            {
                // Measure answer height
                SizeF size = g.MeasureString(item.Answer, _answerFont, (int)contentWidth - 20);
                expandedH = (size.Height + 20) * item.ExpandAnimation;
            }

            // Check if off screen
            if (drawY > height) break;

            // Draw Item Background
            RectangleF rect = new RectangleF(x, drawY, contentWidth, itemH + expandedH);
            bool isSelected = (i == _selectedIndex);

            using (var brush = new SolidBrush(isSelected ? Color.FromArgb(80, 100, 140) : Color.FromArgb(50, 55, 65)))
            {
                // Rounded rect manual
                g.FillRectangle(brush, rect);
                g.DrawRectangle(Pens.Gray, rect.X, rect.Y, rect.Width, rect.Height);
            }

            if (isSelected)
            {
                 g.DrawRectangle(Pens.Cyan, rect.X, rect.Y, rect.Width, rect.Height);
                 // Highlight marker
                 g.FillRectangle(Brushes.Cyan, x, drawY, 5, itemH + expandedH);
            }

            // Draw Category (Small tag)
            SizeF catSize = g.MeasureString(item.Category, _hintFont);
            g.DrawString(item.Category, _hintFont, Brushes.LightSkyBlue, x + 15, drawY + 5);

            // Draw Question
            g.DrawString(item.Question, _questionFont, Brushes.White, x + 15 + catSize.Width + 10, drawY + 8);

            // Draw Answer (Clipped/Masked by height)
            if (expandedH > 0)
            {
                RectangleF textRect = new RectangleF(x + 20, drawY + itemH, contentWidth - 40, expandedH - 10);
                Region r = g.Clip;
                g.SetClip(textRect);
                g.DrawString(item.Answer, _answerFont, Brushes.Gainsboro, textRect);
                g.Clip = r;
            }

            drawY += itemH + expandedH + 10; // Spacing
        }

        // Scrollbar indicator (optional)
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
