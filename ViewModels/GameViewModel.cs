using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using MinesweeperApp.Models;
using MinesweeperApp.Services;

namespace MinesweeperApp.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged
    {
        private int _rows;
        private int _cols;
        private int _mineCount;
        private GameCell[,]? _grid;
        private bool _gameOver;
        private bool _gameWon;
        private bool _hasRevealedCell;
        private int _flagsPlaced;
        private bool _isGameStarted;
        private bool _isFlagMode;
        private bool _isDifficultySelectionVisible = true;
        private bool _showResultDialog;
        private bool _isFeedbackEnabled = true;
        private int _elapsedSeconds;
        private int _revealedSafeCells;
        private int _safeCellCount;
        private double _boardZoom = 1.0;
        private IDispatcherTimer? _gameTimer;
        private DifficultyLevel _selectedDifficulty = DifficultyLevel.Easy;
        private string _statusMessage = "Select Difficulty";
        private string _themeName = "Classic";
        private string _bestTimeEasy = "--:--";
        private string _bestTimeMedium = "--:--";
        private string _bestTimeHard = "--:--";
        private string _bestTimeCustom = "--:--";
        private string _resultTitle = "";
        private string _resultSummary = "";
        private string _resultDetail = "";
        private int _customRows = 12;
        private int _customCols = 12;
        private int _customMines = 20;
        private int _gamesPlayed;
        private int _gamesWon;
        private int _gamesLost;
        private int _currentStreak;
        private int _bestStreak;

        public List<GameCell> Cells { get; private set; } = new();

        public bool IsGameStarted
        {
            get => _isGameStarted;
            set { if (_isGameStarted == value) return; _isGameStarted = value; OnPropertyChanged(); }
        }

        public bool IsFlagMode
        {
            get => _isFlagMode;
            set { if (_isFlagMode == value) return; _isFlagMode = value; OnPropertyChanged(); }
        }

        public bool IsDifficultySelectionVisible
        {
            get => _isDifficultySelectionVisible;
            set { if (_isDifficultySelectionVisible == value) return; _isDifficultySelectionVisible = value; OnPropertyChanged(); }
        }

        public bool ShowResultDialog
        {
            get => _showResultDialog;
            set { if (_showResultDialog == value) return; _showResultDialog = value; OnPropertyChanged(); }
        }

        public bool IsFeedbackEnabled
        {
            get => _isFeedbackEnabled;
            set
            {
                if (_isFeedbackEnabled == value) return;
                _isFeedbackEnabled = value;
                Preferences.Default.Set("feedback_enabled", value);
                OnPropertyChanged();
            }
        }

        public DifficultyLevel SelectedDifficulty
        {
            get => _selectedDifficulty;
            set { if (_selectedDifficulty == value) return; _selectedDifficulty = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { if (_statusMessage == value) return; _statusMessage = value; OnPropertyChanged(); }
        }

        public int ElapsedSeconds
        {
            get => _elapsedSeconds;
            set { if (_elapsedSeconds == value) return; _elapsedSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedTime)); }
        }

        public string FormattedTime => FormatStoredTime(ElapsedSeconds);

        public string BestTimeEasy
        {
            get => _bestTimeEasy;
            set { _bestTimeEasy = value; OnPropertyChanged(); }
        }

        public string BestTimeMedium
        {
            get => _bestTimeMedium;
            set { _bestTimeMedium = value; OnPropertyChanged(); }
        }

        public string BestTimeHard
        {
            get => _bestTimeHard;
            set { _bestTimeHard = value; OnPropertyChanged(); }
        }

        public string BestTimeCustom
        {
            get => _bestTimeCustom;
            set { _bestTimeCustom = value; OnPropertyChanged(); }
        }

        public string ResultTitle
        {
            get => _resultTitle;
            set { _resultTitle = value; OnPropertyChanged(); }
        }

        public string ResultSummary
        {
            get => _resultSummary;
            set { _resultSummary = value; OnPropertyChanged(); }
        }

        public string ResultDetail
        {
            get => _resultDetail;
            set { _resultDetail = value; OnPropertyChanged(); }
        }

        public int CustomRows
        {
            get => _customRows;
            set { _customRows = Math.Clamp(value, 6, 24); OnPropertyChanged(); OnPropertyChanged(nameof(CustomSummary)); }
        }

        public int CustomCols
        {
            get => _customCols;
            set { _customCols = Math.Clamp(value, 6, 30); OnPropertyChanged(); OnPropertyChanged(nameof(CustomSummary)); }
        }

        public int CustomMines
        {
            get => _customMines;
            set { _customMines = Math.Clamp(value, 1, Math.Max(1, CustomRows * CustomCols - 9)); OnPropertyChanged(); OnPropertyChanged(nameof(CustomSummary)); }
        }

        public string CustomSummary => $"{CustomRows}x{CustomCols}, {CustomMines} mines";

        public int GamesPlayed
        {
            get => _gamesPlayed;
            set { _gamesPlayed = value; OnPropertyChanged(); OnPropertyChanged(nameof(WinRateText)); }
        }

        public int GamesWon
        {
            get => _gamesWon;
            set { _gamesWon = value; OnPropertyChanged(); OnPropertyChanged(nameof(WinRateText)); }
        }

        public int GamesLost
        {
            get => _gamesLost;
            set { _gamesLost = value; OnPropertyChanged(); }
        }

        public int CurrentStreak
        {
            get => _currentStreak;
            set { _currentStreak = value; OnPropertyChanged(); }
        }

        public int BestStreak
        {
            get => _bestStreak;
            set { _bestStreak = value; OnPropertyChanged(); }
        }

        public string WinRateText => GamesPlayed == 0 ? "0%" : $"{(int)Math.Round(GamesWon * 100d / GamesPlayed)}%";

        public int MinesLeft => _mineCount - _flagsPlaced;

        public string DifficultyLabel => $"{SelectedDifficulty} • {_rows}x{_cols}";

        public string ProgressText => _safeCellCount == 0 ? "0%" : $"{Math.Clamp((int)Math.Round(_revealedSafeCells * 100d / _safeCellCount), 0, 100)}% clear";

        public double Progress => _safeCellCount == 0 ? 0 : Math.Clamp(_revealedSafeCells / (double)_safeCellCount, 0, 1);

        public int GridColumns => _cols > 0 ? _cols : 9;

        public int CellSize => Math.Max(18, (int)Math.Round(BaseCellSize * BoardZoom));

        public int CellFontSize => Math.Max(10, (int)Math.Round(BaseCellFontSize * BoardZoom));

        public int BoardSpacing => _cols > 16 ? 2 : 3;

        public int CellCornerRadius => _cols > 16 ? 4 : 6;

        public int BoardWidth => _cols <= 0 ? 320 : _cols * CellSize + Math.Max(0, _cols - 1) * BoardSpacing;

        public int BoardHeight => _rows <= 0 ? 320 : _rows * CellSize + Math.Max(0, _rows - 1) * BoardSpacing;

        public double BoardZoom
        {
            get => _boardZoom;
            set
            {
                var next = Math.Clamp(value, 0.8, 1.8);
                if (Math.Abs(_boardZoom - next) < 0.01) return;
                _boardZoom = next;
                NotifyBoardSize();
            }
        }

        public string ThemeName
        {
            get => _themeName;
            set
            {
                if (_themeName == value) return;
                _themeName = value;
                Preferences.Default.Set("theme_name", value);
                ApplyThemeToCells();
                NotifyTheme();
            }
        }

        public Color PageBackground => ThemeName switch
        {
            "Light" => Color.FromArgb("#F4F7F5"),
            "Retro" => Color.FromArgb("#2F261B"),
            "Neon" => Color.FromArgb("#08121E"),
            _ => Color.FromArgb("#0F0F23")
        };

        public Color SurfaceColor => ThemeName switch
        {
            "Light" => Color.FromArgb("#EEF4F0"),
            "Retro" => Color.FromArgb("#382C1D"),
            "Neon" => Color.FromArgb("#0C1828"),
            _ => Color.FromArgb("#15162B")
        };

        public Color PanelColor => ThemeName switch
        {
            "Light" => Color.FromArgb("#FFFFFF"),
            "Retro" => Color.FromArgb("#463822"),
            "Neon" => Color.FromArgb("#101E2F"),
            _ => Color.FromArgb("#1A1A32")
        };

        public Color PanelStroke => ThemeName switch
        {
            "Light" => Color.FromArgb("#DDE6E0"),
            "Retro" => Color.FromArgb("#7C6538"),
            "Neon" => Color.FromArgb("#1E88A8"),
            _ => Color.FromArgb("#3A3A5A")
        };

        public Color HeaderText => ThemeName == "Light" ? Color.FromArgb("#18221D") : Colors.White;

        public Color MutedText => ThemeName switch
        {
            "Light" => Color.FromArgb("#50635A"),
            "Retro" => Color.FromArgb("#E5D6A7"),
            "Neon" => Color.FromArgb("#9DEBF2"),
            _ => Color.FromArgb("#B0B0D0")
        };

        public Color PrimaryAccent => ThemeName switch
        {
            "Light" => Color.FromArgb("#1976D2"),
            "Retro" => Color.FromArgb("#D0A33A"),
            "Neon" => Color.FromArgb("#00BCD4"),
            _ => Color.FromArgb("#1E88E5")
        };

        public Color DangerAccent => Color.FromArgb("#D94343");

        public Color SuccessAccent => Color.FromArgb("#28A66A");

        public Color ResultAccentColor => _gameWon ? Color.FromArgb("#2E7D32") : Color.FromArgb("#D32F2F");

        private int BaseCellSize => _cols switch
        {
            <= 9 => 34,
            <= 16 => 22,
            _ => 20
        };

        private int BaseCellFontSize => _cols switch
        {
            <= 9 => 15,
            <= 16 => 12,
            _ => 11
        };

        public ICommand RevealCommand { get; }
        public ICommand FlagCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand ToggleFlagModeCommand { get; }
        public ICommand StartGameCommand { get; }
        public ICommand QuitGameCommand { get; }
        public ICommand SelectDifficultyCommand { get; }
        public ICommand BackToDifficultyCommand { get; }
        public ICommand DismissResultCommand { get; }
        public ICommand PlayAgainCommand { get; }
        public ICommand SelectThemeCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }

        public GameViewModel()
        {
            RevealCommand = new Command<GameCell>(RevealCell);
            FlagCommand = new Command<GameCell>(FlagCell);
            RestartCommand = new Command(async () => await RestartCurrentGameAsync());
            ToggleFlagModeCommand = new Command(ToggleFlagMode);
            StartGameCommand = new Command(async () => await StartGameAsync());
            QuitGameCommand = new Command(QuitGame);
            SelectDifficultyCommand = new Command<DifficultyLevel>(async difficulty => await SelectDifficultyAsync(difficulty));
            BackToDifficultyCommand = new Command(BackToDifficulty);
            DismissResultCommand = new Command(() => ShowResultDialog = false);
            PlayAgainCommand = new Command(async () => await RestartCurrentGameAsync());
            SelectThemeCommand = new Command<string>(theme => ThemeName = string.IsNullOrWhiteSpace(theme) ? "Classic" : theme);
            ZoomInCommand = new Command(() => BoardZoom += 0.1);
            ZoomOutCommand = new Command(() => BoardZoom -= 0.1);

            LoadSettings();
            LoadBestTimes();
            LoadStats();
            NotifyTheme();
        }

        private void LoadSettings()
        {
            ThemeName = Preferences.Default.Get("theme_name", "Classic");
            IsFeedbackEnabled = Preferences.Default.Get("feedback_enabled", true);
            CustomRows = Preferences.Default.Get("custom_rows", 12);
            CustomCols = Preferences.Default.Get("custom_cols", 12);
            CustomMines = Preferences.Default.Get("custom_mines", 20);
        }

        private void LoadBestTimes()
        {
            BestTimeEasy = FormatStoredTime(Preferences.Default.Get("best_time_easy", -1));
            BestTimeMedium = FormatStoredTime(Preferences.Default.Get("best_time_medium", -1));
            BestTimeHard = FormatStoredTime(Preferences.Default.Get("best_time_hard", -1));
            BestTimeCustom = FormatStoredTime(Preferences.Default.Get("best_time_custom", -1));
        }

        private void LoadStats()
        {
            GamesPlayed = Preferences.Default.Get("stats_played", 0);
            GamesWon = Preferences.Default.Get("stats_won", 0);
            GamesLost = Preferences.Default.Get("stats_lost", 0);
            CurrentStreak = Preferences.Default.Get("stats_current_streak", 0);
            BestStreak = Preferences.Default.Get("stats_best_streak", 0);
        }

        private void SaveStats()
        {
            Preferences.Default.Set("stats_played", GamesPlayed);
            Preferences.Default.Set("stats_won", GamesWon);
            Preferences.Default.Set("stats_lost", GamesLost);
            Preferences.Default.Set("stats_current_streak", CurrentStreak);
            Preferences.Default.Set("stats_best_streak", BestStreak);
        }

        private static string FormatStoredTime(int seconds)
        {
            if (seconds < 0) return "--:--";
            return $"{seconds / 60:D2}:{seconds % 60:D2}";
        }

        private string BestTimeKey() => SelectedDifficulty switch
        {
            DifficultyLevel.Easy => "best_time_easy",
            DifficultyLevel.Medium => "best_time_medium",
            DifficultyLevel.Hard => "best_time_hard",
            DifficultyLevel.Custom => "best_time_custom",
            _ => "best_time_easy"
        };

        private bool SaveBestTime(int seconds)
        {
            string prefsKey = BestTimeKey();
            int currentBest = Preferences.Default.Get(prefsKey, -1);
            if (currentBest >= 0 && seconds >= currentBest) return false;

            Preferences.Default.Set(prefsKey, seconds);
            LoadBestTimes();
            return true;
        }

        private async Task SelectDifficultyAsync(DifficultyLevel difficulty)
        {
            SelectedDifficulty = difficulty;
            await StartGameAsync();
        }

        private void BackToDifficulty()
        {
            StopGameTimer();
            ShowResultDialog = false;
            IsGameStarted = false;
            IsDifficultySelectionVisible = true;
            StatusMessage = "Select Difficulty";
        }

        private async Task StartGameAsync()
        {
            Preferences.Default.Set("custom_rows", CustomRows);
            Preferences.Default.Set("custom_cols", CustomCols);
            Preferences.Default.Set("custom_mines", CustomMines);
            StatusMessage = "Preparing board...";
            ShowResultDialog = false;
            IsGameStarted = true;
            IsDifficultySelectionVisible = false;
            await InitGameAsync();
        }

        private async Task RestartCurrentGameAsync()
        {
            ShowResultDialog = false;
            StatusMessage = "Restarting...";
            await InitGameAsync();
        }

        private async Task InitGameAsync()
        {
            var settings = GameSettings.GetSettings(SelectedDifficulty, CustomRows, CustomCols, CustomMines);
            int rows = settings.Rows;
            int cols = settings.Cols;
            int mineCount = settings.MineCount;

            var gameData = await Task.Run(() =>
            {
                var grid = new GameCell[rows, cols];
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        grid[r, c] = new GameCell { Row = r, Column = c, Theme = ThemeName };

                return grid.Cast<GameCell>().ToList();
            });

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _rows = rows;
                _cols = cols;
                _mineCount = mineCount;
                _grid = new GameCell[rows, cols];

                for (int i = 0; i < gameData.Count; i++)
                {
                    int r = i / cols;
                    int c = i % cols;
                    _grid[r, c] = gameData[i];
                }

                _gameOver = false;
                _gameWon = false;
                _hasRevealedCell = false;
                _flagsPlaced = 0;
                _revealedSafeCells = 0;
                _safeCellCount = rows * cols - mineCount;
                IsFlagMode = false;
                BoardZoom = 1.0;
                ElapsedSeconds = 0;
                StatusMessage = "Tap a tile to begin";

                Cells = gameData;
                OnPropertyChanged(nameof(Cells));
                OnPropertyChanged(nameof(MinesLeft));
                OnPropertyChanged(nameof(DifficultyLabel));
                NotifyProgress();
                OnPropertyChanged(nameof(ResultAccentColor));
                NotifyBoardSize();
                StartGameTimer();
            });
        }

        private void PlaceMinesAvoiding(int safeRow, int safeCol)
        {
            if (_grid == null) return;

            var safe = new HashSet<int> { safeRow * _cols + safeCol };
            foreach (var (dr, dc) in Neighbors())
            {
                int nr = safeRow + dr;
                int nc = safeCol + dc;
                if (nr >= 0 && nr < _rows && nc >= 0 && nc < _cols)
                    safe.Add(nr * _cols + nc);
            }

            var positions = Enumerable.Range(0, _rows * _cols)
                .Where(pos => !safe.Contains(pos))
                .ToArray();

            var random = Random.Shared;
            for (int i = positions.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (positions[i], positions[j]) = (positions[j], positions[i]);
            }

            foreach (var pos in positions.Take(_mineCount))
            {
                int r = pos / _cols;
                int c = pos % _cols;
                _grid[r, c].IsMine = true;
            }

            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                    _grid[r, c].AdjacentMines = _grid[r, c].IsMine ? 0 : CountAdjacentMines(_grid, _rows, _cols, r, c);
        }

        private int CountAdjacentMines(GameCell[,] grid, int rows, int cols, int r, int c)
        {
            return Neighbors().Count(n =>
            {
                int nr = r + n.dr;
                int nc = c + n.dc;
                return nr >= 0 && nr < rows && nc >= 0 && nc < cols && grid[nr, nc].IsMine;
            });
        }

        private void StartGameTimer()
        {
            StopGameTimer();
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.GetForCurrentThread();
            if (dispatcher == null) return;

            _gameTimer = dispatcher.CreateTimer();
            _gameTimer.Interval = TimeSpan.FromSeconds(1);
            _gameTimer.Tick += (_, _) =>
            {
                if (_hasRevealedCell && !_gameOver && !_gameWon)
                    ElapsedSeconds++;
            };
            _gameTimer.Start();
        }

        private void StopGameTimer()
        {
            if (_gameTimer == null) return;
            _gameTimer.Stop();
            _gameTimer = null;
        }

        private void ToggleFlagMode()
        {
            IsFlagMode = !IsFlagMode;
            PlayFeedback(FeedbackKind.Flag);
        }

        private static void QuitGame()
        {
            Application.Current?.Quit();
        }

        private void RevealCell(GameCell? cell)
        {
            if (cell == null || _gameOver || _gameWon || cell.IsRevealed) return;

            if (IsFlagMode)
            {
                FlagCell(cell);
                return;
            }

            if (cell.IsFlagged) return;

            if (!_hasRevealedCell)
            {
                PlaceMinesAvoiding(cell.Row, cell.Column);
                _hasRevealedCell = true;
                StatusMessage = "Watch your step";
            }

            if (cell.IsMine)
            {
                cell.IsRevealed = true;
                RevealAllMines();
                FinishGame(false);
                return;
            }

            FloodReveal(cell.Row, cell.Column);
            PlayFeedback(FeedbackKind.Reveal);

            if (_revealedSafeCells >= _safeCellCount)
                FinishGame(true);
        }

        private void FlagCell(GameCell? cell)
        {
            if (cell == null || _gameOver || _gameWon || cell.IsRevealed) return;
            cell.IsFlagged = !cell.IsFlagged;
            _flagsPlaced += cell.IsFlagged ? 1 : -1;
            OnPropertyChanged(nameof(MinesLeft));
            PlayFeedback(FeedbackKind.Flag);
        }

        private int FloodReveal(int r, int c)
        {
            if (_grid == null || r < 0 || r >= _rows || c < 0 || c >= _cols) return 0;

            var queue = new Queue<(int r, int c)>();
            var revealed = new HashSet<(int, int)>();
            queue.Enqueue((r, c));
            revealed.Add((r, c));

            int revealCount = 0;
            while (queue.Count > 0)
            {
                var (cr, cc) = queue.Dequeue();
                var cell = _grid[cr, cc];
                if (cell.IsRevealed || cell.IsFlagged || cell.IsMine) continue;

                cell.IsRevealed = true;
                revealCount++;
                _revealedSafeCells++;

                if (cell.AdjacentMines != 0) continue;

                foreach (var (dr, dc) in Neighbors())
                {
                    int nr = cr + dr;
                    int nc = cc + dc;
                    if (nr < 0 || nr >= _rows || nc < 0 || nc >= _cols) continue;
                    if (revealed.Contains((nr, nc))) continue;

                    var neighbor = _grid[nr, nc];
                    if (!neighbor.IsRevealed && !neighbor.IsFlagged && !neighbor.IsMine)
                    {
                        queue.Enqueue((nr, nc));
                        revealed.Add((nr, nc));
                    }
                }
            }

            if (revealCount > 0)
            {
                OnPropertyChanged(nameof(Cells));
                NotifyProgress();
            }

            return revealCount;
        }

        private void RevealAllMines()
        {
            foreach (var cell in Cells.Where(c => c.IsMine))
                cell.IsRevealed = true;

            OnPropertyChanged(nameof(Cells));
        }

        private void FinishGame(bool won)
        {
            _gameWon = won;
            _gameOver = !won;
            StopGameTimer();

            GamesPlayed++;
            if (won)
            {
                GamesWon++;
                CurrentStreak++;
                BestStreak = Math.Max(BestStreak, CurrentStreak);
            }
            else
            {
                GamesLost++;
                CurrentStreak = 0;
            }

            SaveStats();
            bool newBest = won && SaveBestTime(ElapsedSeconds);
            StatusMessage = won ? "You Win!" : "Boom! Game Over!";
            ResultTitle = won ? "You Win!" : "Game Over";
            ResultSummary = won
                ? $"{SelectedDifficulty} cleared in {FormattedTime}"
                : $"{SelectedDifficulty} ended at {FormattedTime}";
            ResultDetail = won && newBest ? "New best time!" : $"Wins: {GamesWon}  Losses: {GamesLost}  Streak: {CurrentStreak}";
            OnPropertyChanged(nameof(ResultAccentColor));
            ShowResultDialog = true;
            PlayFeedback(won ? FeedbackKind.Win : FeedbackKind.Lose);
        }

        private void PlayFeedback(FeedbackKind kind)
        {
            if (!IsFeedbackEnabled) return;

            try
            {
                GameFeedback.Play(kind switch
                {
                    FeedbackKind.Flag => GameFeedbackTone.Flag,
                    FeedbackKind.Win => GameFeedbackTone.Win,
                    FeedbackKind.Lose => GameFeedbackTone.Lose,
                    _ => GameFeedbackTone.Reveal
                });
            }
            catch
            {
                // Sound is optional; keep gameplay responsive if the device blocks it.
            }

            try
            {
                HapticFeedback.Default.Perform(kind == FeedbackKind.Lose ? HapticFeedbackType.LongPress : HapticFeedbackType.Click);
            }
            catch
            {
                // Some desktops and emulators do not expose haptics.
            }

            try
            {
                int duration = kind switch
                {
                    FeedbackKind.Lose => 180,
                    FeedbackKind.Win => 90,
                    FeedbackKind.Flag => 35,
                    _ => 20
                };
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(duration));
            }
            catch
            {
                // Vibration is best-effort and device-dependent.
            }
        }

        private void ApplyThemeToCells()
        {
            foreach (var cell in Cells)
                cell.Theme = ThemeName;
        }

        private void NotifyTheme()
        {
            OnPropertyChanged(nameof(ThemeName));
            OnPropertyChanged(nameof(PageBackground));
            OnPropertyChanged(nameof(SurfaceColor));
            OnPropertyChanged(nameof(PanelColor));
            OnPropertyChanged(nameof(PanelStroke));
            OnPropertyChanged(nameof(HeaderText));
            OnPropertyChanged(nameof(MutedText));
            OnPropertyChanged(nameof(PrimaryAccent));
            OnPropertyChanged(nameof(DangerAccent));
            OnPropertyChanged(nameof(SuccessAccent));
        }

        private void NotifyBoardSize()
        {
            OnPropertyChanged(nameof(GridColumns));
            OnPropertyChanged(nameof(CellSize));
            OnPropertyChanged(nameof(CellFontSize));
            OnPropertyChanged(nameof(BoardSpacing));
            OnPropertyChanged(nameof(CellCornerRadius));
            OnPropertyChanged(nameof(BoardWidth));
            OnPropertyChanged(nameof(BoardHeight));
            OnPropertyChanged(nameof(BoardZoom));
        }

        private void NotifyProgress()
        {
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(ProgressText));
        }

        private static IEnumerable<(int dr, int dc)> Neighbors() =>
            new[] { (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1) };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private enum FeedbackKind
        {
            Reveal,
            Flag,
            Win,
            Lose
        }
    }

    public class BoolToFlagTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool?)value == true ? "Flag" : "Tap";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool?)value == true ? Color.FromArgb("#FF5722") : Color.FromArgb("#4CAF50");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToInstructionConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool?)value == true ? "Flag mode is on. Tap tiles to flag." : "Tap to reveal. Long-press to flag.";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isVisible = (bool?)value == true;
            if (parameter?.ToString() == "inverse")
                isVisible = !isVisible;
            return isVisible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToActionTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool?)value == true ? "flag/unflag" : "reveal";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
