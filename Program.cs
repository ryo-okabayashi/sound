using NAudio.Wave;
using System;

namespace Sound
{
    class Program
    {
        public static WaveFileWriter waveFileWriter;
        public static WaveOut waveOut;

        static void Main(string[] args)
        {
            // for recording
            waveFileWriter = new WaveFileWriter(@"C:\rec\out.wav", new WaveFormat(44100, 2));

            var sound = new MySound();
            sound.SetWaveFormat(44100, 2);
            sound.init();
            waveOut = new WaveOut();
            waveOut.Init(sound);
            waveOut.Play();

            ConsoleKeyInfo keyInfo;
            bool loop = true;
            while (loop)
            {
                keyInfo = Console.ReadKey();
                if (keyInfo.Key == ConsoleKey.Q)
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                    waveFileWriter.Close();
                    waveFileWriter.Dispose();
                    loop = false;
                }
            }
        }
    }
}
