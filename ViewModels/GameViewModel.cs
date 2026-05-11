using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MinesweeperApp.Models;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace MinesweeperApp.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged
    {
        private GameSettings? _currentSettings;
        private int _rows;
        private int _cols;
        private int _mineCount;
        private GameCell[,]? _grid;
        private bool _gameOver;
        private bool _gameWon;
        private int _flagsPlaced;
        private bool _isGameStarted;
        private bool _isFlagMode;
        private string _statusMessage = "Select Difficulty 🎯";
        private DifficultyLevel _selectedDifficulty = DifficultyLevel.Easy;
        private bool _isDifficultySelectionVisible = true;
        private int _elapsedSeconds;
        private IDispatcherTimer? _gameTimer;
        private string _bestTimeEasy = "--:--";
        private string _bestTimeMedium = "--:--";

        public List<GameCell> Cells { get; private set; } = new();

        public bool IsGameStarted
        {
            get => _isGameStarted;
            set { _isGameStarted = value; OnPropertyChanged(); }
        }

        public bool IsFlagMode
        {
            get => _isFlagMode;
            set { _isFlagMode = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public DifficultyLevel SelectedDifficulty
        {
            get => _selectedDifficulty;
            set { _selectedDifficulty = value; OnPropertyChanged(); }
        }

        public bool IsDifficultySelectionVisible
        {
            get => _isDifficultySelectionVisible;
            set { _isDifficultySelectionVisible = value; OnPropertyChanged(); }
        }

        public int ElapsedSeconds
        {
            get => _elapsedSeconds;
            set { _elapsedSeconds = value; OnPropertyChanged(nameof(FormattedTime)); }
        }

        public string FormattedTime
        {
            get
            {
                int minutes = ElapsedSeconds / 60;
                int seconds = ElapsedSeconds % 60;
                return $"{minutes:D2}:{seconds:D2}";
            }
        }

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

        public int MinesLeft => _mineCount - _flagsPlaced;

        public int GridColumns => _cols > 0 ? _cols : 9;

        public int CellSize => _cols switch
        {
            <= 9 => 42,
            <= 16 => 34,
            _ => 26
        };

        public ICommand RevealCommand { get; }
        public ICommand FlagCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand ToggleFlagModeCommand { get; }
        public ICommand StartGameCommand { get; }
        public ICommand QuitGameCommand { get; }
        public ICommand SelectDifficultyCommand { get; }
        public ICommand BackToDifficultyCommand { get; }

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

            LoadBestTimes();
        }

        private void LoadBestTimes()
        {
            BestTimeEasy = FormatStoredTime(Preferences.Default.Get("best_time_easy", -1));
            BestTimeMedium = FormatStoredTime(Preferences.Default.Get("best_time_medium", -1));
        }

        private string FormatStoredTime(int seconds)
        {
            if (seconds < 0) return "--:--";
            int minutes = seconds / 60;
            int secs = seconds % 60;
            return $"{minutes:D2}:{secs:D2}";
        }

        private void SaveBestTime(int seconds)
        {
            string prefsKey = SelectedDifficulty switch
            {
                DifficultyLevel.Easy => "best_time_easy",
                DifficultyLevel.Medium => "best_time_medium",
                _ => "best_time_easy"
            };

            int currentBest = Preferences.Default.Get(prefsKey, -1);
            if (currentBest < 0 || seconds < currentBest)
            {
                Preferences.Default.Set(prefsKey, seconds);
                LoadBestTimes();
            }
        }

        private async Task SelectDifficultyAsync(DifficultyLevel difficulty)
        {
            SelectedDifficulty = difficulty;
            await StartGameAsync();
        }

        private void BackToDifficulty()
        {
            StopGameTimer();
            IsGameStarted = false;
            IsDifficultySelectionVisible = true;
            StatusMessage = "Select Difficulty 🎯";
        }

        private async Task StartGameAsync()
        {
            StatusMessage = "Preparing board...";
            IsGameStarted = true;
            IsDifficultySelectionVisible = false;
            await InitGameAsync();
        }

        private async Task RestartCurrentGameAsync()
        {
            StatusMessage = "Restarting...";
            await InitGameAsync();
        }

        private async Task InitGameAsync()
        {
            var settings = GameSettings.GetSettings(SelectedDifficulty);
            int rows = settings.Rows;
            int cols = settings.Cols;
            int mineCount = settings.MineCount;

            // Run heavy computations off main thread
            var gameData = await Task.Run(() =>
            {
                var grid = new GameCell[rows, cols];
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        grid[r, c] = new GameCell { Row = r, Column = c };

                // Fisher-Yates shuffle for mine placement (more efficient)
                var positions = Enumerable.Range(0, rows * cols).OrderBy(x => Guid.NewGuid()).Take(mineCount).ToList();
                foreach (var pos in positions)
                {
                    int r = pos / cols;
                    int c = pos % cols;
                    grid[r, c].IsMine = true;
                }

                // Calculate adjacent mines
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        if (!grid[r, c].IsMine)
                            grid[r, c].AdjacentMines = CountAdjacentMines(grid, rows, cols, r, c);

                return grid.Cast<GameCell>().ToList();
            });

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _currentSettings = settings;
                _rows = rows;
                _cols = cols;
                _mineCount = mineCount;

                // Reconstruct grid from flat list
                _grid = new GameCell[rows, cols];
                for (int i = 0; i < gameData.Count; i++)
                {
                    int r = i / cols;
                    int c = i % cols;
                    _grid[r, c] = gameData[i];
                }

                _gameOver = false;
                _gameWon = false;
                _flagsPlaced = 0;
                IsFlagMode = false;
                ElapsedSeconds = 0;
                StatusMessage = "Good luck! 🎯";

                Cells = gameData;
                OnPropertyChanged(nameof(Cells));
                OnPropertyChanged(nameof(MinesLeft));
                OnPropertyChanged(nameof(GridColumns));
                OnPropertyChanged(nameof(CellSize));

                StartGameTimer();
            });
        }

        private int CountAdjacentMines(GameCell[,] grid, int rows, int cols, int r, int c)
        {
            return Neighbors().Count(n =>
            {
                int nr = r + n.dr, nc = c + n.dc;
                return nr >= 0 && nr < rows && nc >= 0 && nc < cols && grid[nr, nc].IsMine;
            });
        }

        private void StartGameTimer()
        {
            if (_gameTimer != null)
                StopGameTimer();

            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.GetForCurrentThread();
            if (dispatcher == null) return;

            _gameTimer = dispatcher.CreateTimer();
            _gameTimer.Interval = TimeSpan.FromSeconds(1);
            _gameTimer.Tick += (s, e) =>
            {
                ElapsedSeconds++;
            };
            _gameTimer.Start();
        }

        private void StopGameTimer()
        {
            if (_gameTimer != null)
            {
                _gameTimer.Stop();
                _gameTimer = null;
            }
        }

        private void ToggleFlagMode()
        {
            IsFlagMode = !IsFlagMode;
        }

        private void QuitGame()
        {
            Application.Current?.Quit();
        }

        private void RevealCell(GameCell cell)
        {
            if (_gameOver || _gameWon || cell.IsRevealed) return;

            if (IsFlagMode)
            {
                FlagCell(cell);
                return;
            }

            if (cell.IsFlagged) return;

            if (cell.IsMine)
            {
                cell.IsRevealed = true;
                RevealAllMines();
                _gameOver = true;
                StatusMessage = "💥 Boom! Game Over!";
                StopGameTimer();
                return;
            }

            FloodReveal(cell.Row, cell.Column);

            if (CheckWin())
            {
                _gameWon = true;
                StatusMessage = "🎉 You Win!";
                StopGameTimer();
                SaveBestTime(ElapsedSeconds);
            }
        }

        private void FlagCell(GameCell cell)
        {
            if (_gameOver || _gameWon || cell.IsRevealed) return;
            cell.IsFlagged = !cell.IsFlagged;
            _flagsPlaced += cell.IsFlagged ? 1 : -1;
            OnPropertyChanged(nameof(MinesLeft));
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

            // Notify UI about all changes at once
            if (revealCount > 0)
            {
                OnPropertyChanged(nameof(Cells));
            }

            return revealCount;
        }

        private void RevealAllMines()
        {
            int count = 0;
            foreach (var cell in Cells.Where(c => c.IsMine))
            {
                if (!cell.IsRevealed)
                {
                    cell.IsRevealed = true;
                    count++;
                }
            }
            if (count > 0)
                OnPropertyChanged(nameof(Cells));
        }

        private bool CheckWin() =>
            Cells.Where(c => !c.IsMine).All(c => c.IsRevealed);

        private static IEnumerable<(int dr, int dc)> Neighbors() =>
            new[] { (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1) };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class BoolToFlagTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (bool?)value == true ? "🚩" : "💣";
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
            return (bool?)value == true ? "Tap to flag/unflag 🚩" : "Tap to reveal 💣";
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