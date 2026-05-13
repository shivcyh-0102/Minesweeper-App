using System.Collections;
using System.ComponentModel;
using System.Windows.Input;
using MinesweeperApp.Models;

namespace MinesweeperApp.Controls
{
    public class MinesweeperBoardView : GraphicsView, IDrawable
    {
        public static readonly BindableProperty CellsProperty = BindableProperty.Create(
            nameof(Cells),
            typeof(IList),
            typeof(MinesweeperBoardView),
            null,
            propertyChanged: OnCellsChanged);

        public static readonly BindableProperty CellSizeProperty = BindableProperty.Create(
            nameof(CellSize),
            typeof(int),
            typeof(MinesweeperBoardView),
            24,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CellFontSizeProperty = BindableProperty.Create(
            nameof(CellFontSize),
            typeof(int),
            typeof(MinesweeperBoardView),
            12,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BoardSpacingProperty = BindableProperty.Create(
            nameof(BoardSpacing),
            typeof(int),
            typeof(MinesweeperBoardView),
            2,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CellCornerRadiusProperty = BindableProperty.Create(
            nameof(CellCornerRadius),
            typeof(int),
            typeof(MinesweeperBoardView),
            5,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BoardWidthProperty = BindableProperty.Create(
            nameof(BoardWidth),
            typeof(int),
            typeof(MinesweeperBoardView),
            320,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty BoardHeightProperty = BindableProperty.Create(
            nameof(BoardHeight),
            typeof(int),
            typeof(MinesweeperBoardView),
            320,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty PanelStrokeProperty = BindableProperty.Create(
            nameof(PanelStroke),
            typeof(Color),
            typeof(MinesweeperBoardView),
            Colors.Transparent,
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty RevealCommandProperty = BindableProperty.Create(
            nameof(RevealCommand),
            typeof(ICommand),
            typeof(MinesweeperBoardView));

        public static readonly BindableProperty FlagCommandProperty = BindableProperty.Create(
            nameof(FlagCommand),
            typeof(ICommand),
            typeof(MinesweeperBoardView));

        private readonly List<GameCell> _cells = new();
        private readonly Dictionary<(int row, int col), GameCell> _cellsByPosition = new();
        private CancellationTokenSource? _longPressToken;
        private PointF _startPoint;
        private PointF _lastPoint;
        private double _offsetX;
        private double _offsetY;
        private bool _isDragging;
        private bool _longPressHandled;

        public MinesweeperBoardView()
        {
            Drawable = this;
            HorizontalOptions = LayoutOptions.Fill;
            VerticalOptions = LayoutOptions.Fill;
            StartInteraction += OnStartInteraction;
            DragInteraction += OnDragInteraction;
            EndInteraction += OnEndInteraction;
            CancelInteraction += OnCancelInteraction;
            SizeChanged += (_, _) =>
            {
                ClampOffsets();
                Invalidate();
            };
        }

        public IList? Cells
        {
            get => (IList?)GetValue(CellsProperty);
            set => SetValue(CellsProperty, value);
        }

        public int CellSize
        {
            get => (int)GetValue(CellSizeProperty);
            set => SetValue(CellSizeProperty, value);
        }

        public int CellFontSize
        {
            get => (int)GetValue(CellFontSizeProperty);
            set => SetValue(CellFontSizeProperty, value);
        }

        public int BoardSpacing
        {
            get => (int)GetValue(BoardSpacingProperty);
            set => SetValue(BoardSpacingProperty, value);
        }

        public int CellCornerRadius
        {
            get => (int)GetValue(CellCornerRadiusProperty);
            set => SetValue(CellCornerRadiusProperty, value);
        }

        public int BoardWidth
        {
            get => (int)GetValue(BoardWidthProperty);
            set => SetValue(BoardWidthProperty, value);
        }

        public int BoardHeight
        {
            get => (int)GetValue(BoardHeightProperty);
            set => SetValue(BoardHeightProperty, value);
        }

        public Color PanelStroke
        {
            get => (Color)GetValue(PanelStrokeProperty);
            set => SetValue(PanelStrokeProperty, value);
        }

        public ICommand? RevealCommand
        {
            get => (ICommand?)GetValue(RevealCommandProperty);
            set => SetValue(RevealCommandProperty, value);
        }

        public ICommand? FlagCommand
        {
            get => (ICommand?)GetValue(FlagCommandProperty);
            set => SetValue(FlagCommandProperty, value);
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            canvas.Antialias = true;
            canvas.Translate((float)-_offsetX, (float)-_offsetY);

            float pitch = CellSize + BoardSpacing;
            int startCol = Math.Max(0, (int)Math.Floor(_offsetX / pitch) - 1);
            int startRow = Math.Max(0, (int)Math.Floor(_offsetY / pitch) - 1);
            int endCol = Math.Min(GetColumnCount(), (int)Math.Ceiling((_offsetX + dirtyRect.Width) / pitch) + 1);
            int endRow = Math.Min(GetRowCount(), (int)Math.Ceiling((_offsetY + dirtyRect.Height) / pitch) + 1);

            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.FontSize = CellFontSize;

            for (int row = startRow; row < endRow; row++)
            {
                for (int col = startCol; col < endCol; col++)
                {
                    if (!_cellsByPosition.TryGetValue((row, col), out var cell))
                        continue;

                    float x = col * pitch;
                    float y = row * pitch;
                    var rect = new RectF(x, y, CellSize, CellSize);

                    canvas.FillColor = cell.BackgroundColor;
                    canvas.FillRoundedRectangle(rect, CellCornerRadius);

                    canvas.StrokeColor = PanelStroke;
                    canvas.StrokeSize = 1;
                    canvas.DrawRoundedRectangle(rect, CellCornerRadius);

                    string text = cell.DisplayText;
                    if (string.IsNullOrEmpty(text))
                        continue;

                    canvas.FontColor = cell.TextColor;
                    canvas.DrawString(text, rect, HorizontalAlignment.Center, VerticalAlignment.Center);
                }
            }

            canvas.RestoreState();
        }

        private static void OnCellsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var board = (MinesweeperBoardView)bindable;
            board.SetCells(newValue as IList);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var board = (MinesweeperBoardView)bindable;
            board.ClampOffsets();
            board.Invalidate();
        }

        private void SetCells(IList? cells)
        {
            foreach (var cell in _cells)
                cell.PropertyChanged -= OnCellPropertyChanged;

            _cells.Clear();
            _cellsByPosition.Clear();

            if (cells != null)
            {
                foreach (var item in cells)
                {
                    if (item is not GameCell cell)
                        continue;

                    _cells.Add(cell);
                    _cellsByPosition[(cell.Row, cell.Column)] = cell;
                    cell.PropertyChanged += OnCellPropertyChanged;
                }
            }

            _offsetX = 0;
            _offsetY = 0;
            Invalidate();
        }

        private void OnCellPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Invalidate();
        }

        private void OnStartInteraction(object? sender, TouchEventArgs e)
        {
            if (e.Touches.Length == 0)
                return;

            _startPoint = e.Touches[0];
            _lastPoint = _startPoint;
            _isDragging = false;
            _longPressHandled = false;

            _longPressToken?.Cancel();
            _longPressToken = new CancellationTokenSource();
            _ = HandleLongPressAsync(_startPoint, _longPressToken.Token);
        }

        private void OnDragInteraction(object? sender, TouchEventArgs e)
        {
            if (e.Touches.Length == 0)
                return;

            var point = e.Touches[0];
            float dx = point.X - _lastPoint.X;
            float dy = point.Y - _lastPoint.Y;
            float totalDx = point.X - _startPoint.X;
            float totalDy = point.Y - _startPoint.Y;

            if (Math.Abs(totalDx) > 6 || Math.Abs(totalDy) > 6)
            {
                _isDragging = true;
                _longPressToken?.Cancel();
            }

            if (_isDragging)
            {
                _offsetX -= dx;
                _offsetY -= dy;
                ClampOffsets();
                Invalidate();
            }

            _lastPoint = point;
        }

        private void OnEndInteraction(object? sender, TouchEventArgs e)
        {
            _longPressToken?.Cancel();

            if (_isDragging || _longPressHandled)
                return;

            var cell = GetCellAt(_startPoint);
            if (cell != null && RevealCommand?.CanExecute(cell) == true)
                RevealCommand.Execute(cell);
        }

        private void OnCancelInteraction(object? sender, EventArgs e)
        {
            _longPressToken?.Cancel();
        }

        private async Task HandleLongPressAsync(PointF point, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(520, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (_isDragging)
                    return;

                var cell = GetCellAt(point);
                if (cell == null || FlagCommand?.CanExecute(cell) != true)
                    return;

                _longPressHandled = true;
                FlagCommand.Execute(cell);
            });
        }

        private GameCell? GetCellAt(PointF point)
        {
            float pitch = CellSize + BoardSpacing;
            int col = (int)Math.Floor((point.X + _offsetX) / pitch);
            int row = (int)Math.Floor((point.Y + _offsetY) / pitch);

            if (row < 0 || col < 0)
                return null;

            float localX = (float)(point.X + _offsetX - col * pitch);
            float localY = (float)(point.Y + _offsetY - row * pitch);
            if (localX > CellSize || localY > CellSize)
                return null;

            return _cellsByPosition.TryGetValue((row, col), out var cell) ? cell : null;
        }

        private void ClampOffsets()
        {
            double maxX = Math.Max(0, BoardWidth - Width);
            double maxY = Math.Max(0, BoardHeight - Height);
            _offsetX = Math.Clamp(_offsetX, 0, maxX);
            _offsetY = Math.Clamp(_offsetY, 0, maxY);
        }

        private int GetRowCount() => _cells.Count == 0 ? 0 : _cells.Max(cell => cell.Row) + 1;

        private int GetColumnCount() => _cells.Count == 0 ? 0 : _cells.Max(cell => cell.Column) + 1;
    }
}
