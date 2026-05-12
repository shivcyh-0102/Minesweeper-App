namespace MinesweeperApp.Services
{
    public enum GameFeedbackTone
    {
        Reveal,
        Flag,
        Win,
        Lose
    }

    public static partial class GameFeedback
    {
        public static void Play(GameFeedbackTone tone)
        {
            PlayPlatform(tone);
        }

        static partial void PlayPlatform(GameFeedbackTone tone);
    }
}
