using System;

namespace Sound
{
    public class MySound : WaveProvider
    {
        double L;
        double R;
        uint count;
        Random rnd = new Random();

        int sample_rate = 44100;

        Gen.Play2 s1 = new Gen.Play2(@"C:\Users\ryo\Desktop\test.wav");
        Gen.Line2 s1_env = new Gen.Line2(0, 1, 10, 0, 190);
        Gen.Delay s1_delay_L = new Gen.Delay(300, 0.8);
        Gen.Delay s1_delay_R = new Gen.Delay(400, 0.8);

        Gen.Play2 s2 = new Gen.Play2(@"C:/Users/ryo/Desktop/s2.wav");
        Gen.Line2 s2_env = new Gen.Line2(0, 1, 10, 0, 190);

        Gen.Play2 s3 = new Gen.Play2(@"C:/Users/ryo/Desktop/s3.wav");
        Gen.Line2 s3_env = new Gen.Line2(0, 1, 1, 0, 2);

        Gen.Play2 test = new Gen.Play2(@"C:/Users/ryo/Desktop/speedtest.wav");

        public void init()
        {
            test.loop = true;
            test.speed = 1.33;
        }

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            bool clip = false;

            for (int n = 0; n < sampleCount; )
            {
                L = 0; R = 0;

                // s1
                if (count % (44100/4) == 0)
                {
                    if (rnd.Next(0, 16) == 0)
                    {
                        s1_env.reset();
                        s1.phase = rnd.Next(0, 44100 * 2 * 6);
                    }
                }
                var s1_amp = s1_env.val();
                var s1_vals = s1.val();
                var s1_L = s1_vals[0] * s1_amp * 0.5;
                var s1_R = s1_vals[1] * s1_amp * 0.5;
                L += s1_L + s1_delay_L.io(s1_L);
                R += s1_R + s1_delay_R.io(s1_R);

                // s2
                if (count % (44100/16) == 0)
                {
                    if (rnd.Next(0, 16) == 0)
                    {
                        s2.speed = rnd.Next(50, 150) / 100f;
                        s2_env.reset();
                        s2.phase = 0;
                    }
                }
                var s2_amp = s2_env.val() * 0.5;
                var s2_vals = s2.val();
                L += s2_vals[0] * s2_amp;
                R += s2_vals[1] * s2_amp;

                // s3
                if (count % (44100 / 16) == 0)
                {
                    if (rnd.Next(0, 8) > 0)
                    {
                        s3.speed = 15.0f;
                        s3_env.reset();
                        s3.phase = 0;
                    }
                }
                var s3_amp = s3_env.val() * 0.1;
                var s3_vals = s3.val();
                L += s3_vals[0] * s3_amp;
                R += s3_vals[1] * s3_amp;

                // test
                if (count % sample_rate == 0)
                {
                    test.speed = rnd.Next(5, 10) / 10f;
                }
                var tmp = test.val();
                L += tmp[0];
                R += tmp[1];

                if (L > 1 || L < -1 || R > 1 || R < -1) clip = true;

                buffer[n++ + offset] = (float)L;
                buffer[n++ + offset] = (float)R;

                Program.waveFileWriter.WriteSample((float)L);
                Program.waveFileWriter.WriteSample((float)R);

                count++;
            }

            if (clip) Console.WriteLine("clip");

            return sampleCount;
        }
    }
}
