using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using TextExtractorWin.Helpers;

namespace TextExtractorWin.Views;

public class SelectionOverlay : Form
{
    private Point _startPoint;
    private Point _currentPoint;
    private bool _isSelecting;
    private readonly Bitmap _screenCapture;
    private Rectangle _selectionRect;

    public Rectangle SelectionRectangle => _selectionRect;
    public bool SelectionMade { get; private set; }

    public SelectionOverlay()
    {
        // Capture the entire virtual screen
        var bounds = ScreenCapture.GetVirtualScreenBounds();

        // Set form properties for fullscreen overlay
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(bounds.Left, bounds.Top);
        Size = new Size(bounds.Width, bounds.Height);
        TopMost = true;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        BackColor = Color.Black;
        Opacity = 1.0;

        // Capture screen before showing overlay
        _screenCapture = ScreenCapture.CaptureScreen();

        // Handle keyboard
        KeyPreview = true;
        KeyDown += OnKeyDown;

        // Handle mouse events
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Draw the captured screen with dark overlay
        g.DrawImage(_screenCapture, 0, 0);

        // Draw dark overlay
        using var overlayBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        g.FillRectangle(overlayBrush, ClientRectangle);

        if (_isSelecting && _selectionRect.Width > 0 && _selectionRect.Height > 0)
        {
            // Draw the selected region without overlay (clear area)
            g.SetClip(_selectionRect);
            g.DrawImage(_screenCapture, 0, 0);
            g.ResetClip();

            // Draw selection border
            using var borderPen = new Pen(Color.FromArgb(99, 102, 241), 2); // Indigo-500
            g.DrawRectangle(borderPen, _selectionRect);

            // Draw corner handles
            DrawCornerHandles(g, _selectionRect);

            // Draw dimension label
            DrawDimensionLabel(g, _selectionRect);
        }

        // Draw instructions at top
        DrawInstructions(g);
    }

    private void DrawCornerHandles(Graphics g, Rectangle rect)
    {
        const int handleSize = 8;
        using var handleBrush = new SolidBrush(Color.FromArgb(99, 102, 241));

        // Corner handles
        var corners = new[]
        {
            new Rectangle(rect.Left - handleSize / 2, rect.Top - handleSize / 2, handleSize, handleSize),
            new Rectangle(rect.Right - handleSize / 2, rect.Top - handleSize / 2, handleSize, handleSize),
            new Rectangle(rect.Left - handleSize / 2, rect.Bottom - handleSize / 2, handleSize, handleSize),
            new Rectangle(rect.Right - handleSize / 2, rect.Bottom - handleSize / 2, handleSize, handleSize)
        };

        foreach (var corner in corners)
        {
            g.FillEllipse(handleBrush, corner);
        }
    }

    private void DrawDimensionLabel(Graphics g, Rectangle rect)
    {
        var text = $"{rect.Width} × {rect.Height}";
        using var font = new Font("Segoe UI", 11, FontStyle.Regular);
        var textSize = g.MeasureString(text, font);

        var labelX = rect.Left + (rect.Width - textSize.Width) / 2;
        var labelY = rect.Bottom + 8;

        // Ensure label stays on screen
        if (labelY + textSize.Height > ClientRectangle.Height)
        {
            labelY = (int)(rect.Top - textSize.Height - 8);
        }

        // Draw background
        using var bgBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
        var bgRect = new RectangleF(labelX - 8, labelY - 4, textSize.Width + 16, textSize.Height + 8);
        g.FillRoundedRectangle(bgBrush, bgRect, 4);

        // Draw text
        using var textBrush = new SolidBrush(Color.White);
        g.DrawString(text, font, textBrush, labelX, labelY);
    }

    private void DrawInstructions(Graphics g)
    {
        var text = "Click and drag to select region  •  ESC to cancel";
        using var font = new Font("Segoe UI", 12, FontStyle.Regular);
        var textSize = g.MeasureString(text, font);

        var x = (ClientRectangle.Width - textSize.Width) / 2;
        var y = 20;

        // Draw background
        using var bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
        var bgRect = new RectangleF(x - 16, y - 8, textSize.Width + 32, textSize.Height + 16);
        g.FillRoundedRectangle(bgBrush, bgRect, 8);

        // Draw text
        using var textBrush = new SolidBrush(Color.White);
        g.DrawString(text, font, textBrush, x, y);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            SelectionMade = false;
            Close();
        }
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isSelecting = true;
            _startPoint = e.Location;
            _currentPoint = e.Location;
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (_isSelecting)
        {
            _currentPoint = e.Location;
            UpdateSelectionRect();
            Invalidate();
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _isSelecting)
        {
            _isSelecting = false;
            _currentPoint = e.Location;
            UpdateSelectionRect();

            if (_selectionRect.Width > 5 && _selectionRect.Height > 5)
            {
                SelectionMade = true;

                // Adjust for virtual screen offset
                var bounds = ScreenCapture.GetVirtualScreenBounds();
                _selectionRect.X += bounds.Left;
                _selectionRect.Y += bounds.Top;
            }

            Close();
        }
    }

    private void UpdateSelectionRect()
    {
        var x = Math.Min(_startPoint.X, _currentPoint.X);
        var y = Math.Min(_startPoint.Y, _currentPoint.Y);
        var width = Math.Abs(_currentPoint.X - _startPoint.X);
        var height = Math.Abs(_currentPoint.Y - _startPoint.Y);

        _selectionRect = new Rectangle(x, y, width, height);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _screenCapture?.Dispose();
        }
        base.Dispose(disposing);
    }
}

// Extension for rounded rectangles
public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using var path = CreateRoundedRectanglePath(rect, radius);
        g.FillPath(brush, path);
    }

    private static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}
