namespace MinesweeperApp.Models
{
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }

    public class GameSettings
    {
        public DifficultyLevel Difficulty { get; set; }
        public int Rows { get; set; }
        public int Cols { get; set; }
        public int MineCount { get; set; }

        public static GameSettings GetSettings(DifficultyLevel difficulty) => difficulty switch
        {
            DifficultyLevel.Easy => new GameSettings
            {
                Difficulty = DifficultyLevel.Easy,
                Rows = 9,
                Cols = 9,
                MineCount = 10
            },
            DifficultyLevel.Medium => new GameSettings
            {
                Difficulty = DifficultyLevel.Medium,
                Rows = 16,
                Cols = 16,
                MineCount = 40
            },
            DifficultyLevel.Hard => new GameSettings
            {
                Difficulty = DifficultyLevel.Hard,
                Rows = 16,
                Cols = 30,
                MineCount = 99
            },
            _ => throw new ArgumentException("Unknown difficulty level")
        };
    }
}
