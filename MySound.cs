using System;

namespace Sound
{
    public class MySound : WaveProvider
    {
        double L;
        double R;
        uint count;
        Random random = new Random();

        Gen.Sine sine = new Gen.Sine(440);
        Gen.Play s1 = new Gen.Play(@"C:\Users\ryo\Desktop\test.wav");

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            for (int n = 0; n < sampleCount; )
            {
                if (count % (44100/4) == 0)
                {
                    s1.phase = random.Next(0, 88200);
                }
                L = s1.val() * 0.5;
                R = s1.val() * 0.5;

                buffer[n++ + offset] = (float)L;
                buffer[n++ + offset] = (float)R;

                count++;
            }

            return sampleCount;
        }
    }
}
