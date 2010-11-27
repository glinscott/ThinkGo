namespace ThinkGo
{
    using Microsoft.Xna.Framework;
    using System.IO;
    using Microsoft.Xna.Framework.Audio;

    public static class Sounds
    {
        public const string PlaceStone = "Sounds/PlayStone.wav";
        public const string Capture = "Sounds/Capture.wav";
        public const string Undo = "Sounds/Undo.wav";

        public static void PlaySound(string soundFile)
        {
            if (!ThinkGoModel.Instance.SoundEnabled)
                return;

            using (Stream stream = TitleContainer.OpenStream(soundFile))
            {
                SoundEffect effect = SoundEffect.FromStream(stream);
                FrameworkDispatcher.Update();
                effect.Play();
            }
        }
    }
}

