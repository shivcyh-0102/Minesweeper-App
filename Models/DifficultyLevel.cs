namespace MinesweeperApp.Models
{
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard,
        Custom
    }

    public class GameSettings
    {
        public DifficultyLevel Difficulty { get; set; }
        public int Rows { get; set; }
        public int Cols { get; set; }
        public int MineCount { get; set; }
        public string Name { get; set; } = "Easy";

        public static GameSettings GetSettings(DifficultyLevel difficulty, int customRows = 12, int customCols = 12, int customMines = 20) => difficulty switch
        {
            DifficultyLevel.Easy => new GameSettings
            {
                Difficulty = DifficultyLevel.Easy,
                Name = "Easy",
                Rows = 9,
                Cols = 9,
                MineCount = 10
            },
            DifficultyLevel.Medium => new GameSettings
            {
                Difficulty = DifficultyLevel.Medium,
                Name = "Medium",
                Rows = 16,
                Cols = 16,
                MineCount = 40
            },
            DifficultyLevel.Hard => new GameSettings
            {
                Difficulty = DifficultyLevel.Hard,
                Name = "Hard",
                Rows = 16,
                Cols = 30,
                MineCount = 99
            },
            DifficultyLevel.Custom => new GameSettings
            {
                Difficulty = DifficultyLevel.Custom,
                Name = "Custom",
                Rows = Math.Clamp(customRows, 6, 24),
                Cols = Math.Clamp(customCols, 6, 30),
                MineCount = Math.Clamp(customMines, 1, Math.Clamp(customRows, 6, 24) * Math.Clamp(customCols, 6, 30) - 9)
            },
            _ => throw new ArgumentException("Unknown difficulty level")
        };
    }
}
