using Android.Media;
using MinesweeperApp.Services;

namespace MinesweeperApp.Services
{
    public static partial class GameFeedback
    {
        static partial void PlayPlatform(GameFeedbackTone tone)
        {
            try
            {
                var androidTone = tone switch
                {
                    GameFeedbackTone.Flag => Tone.PropAck,
                    GameFeedbackTone.Win => Tone.PropPrompt,
                    GameFeedbackTone.Lose => Tone.PropNack,
                    _ => Tone.PropBeep
                };

                int duration = tone switch
                {
                    GameFeedbackTone.Lose => 220,
                    GameFeedbackTone.Win => 160,
                    GameFeedbackTone.Flag => 80,
                    _ => 50
                };

                _ = Task.Run(async () =>
                {
                    using var generator = new ToneGenerator(Android.Media.Stream.Music, Volume.Max);
                    generator.StartTone(androidTone, duration);
                    await Task.Delay(duration + 40);
                });
            }
            catch
            {
                // Sound feedback is best-effort because devices can mute or restrict tones.
            }
        }
    }
}
