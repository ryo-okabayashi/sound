using NAudio.Wave;
using System;

namespace Gen
{
    public abstract class Gen
    {
        public int SampleRate = 44100;
    }

    public class Sine : Gen
    {
        double _freq;
        double _phase;


        public Sine(float f)
        {
            _freq = f;
        }


        public double val()
        {
            _phase += _freq / SampleRate;
            return Math.Sin(2 * Math.PI * _phase);
        }


        public double freq() { return _freq; }
        public Sine freq(double f)
        {
            _freq = f;
            return this;
        }
    }


    public class Triangle : Gen
    {
        double _freq;
        int direction;
        double _value;


        public Triangle(double frequency)
        {
            _freq = frequency;
            direction = 1;
        }


        public double val()
        {
            _value += (_freq / SampleRate) * 4 * direction;
            if (_value > 1)
            {
                _value = 2 - _value;
                direction = -1;
            }
            if (_value < -1)
            {
                _value = -2 - _value;
                direction = 1;
            }


            return _value;
        }


        public Triangle freq(double frequency)
        {
            _freq = frequency;
            return this;
        }
    }


    public class Pulse : Gen
    {
        int _range;
        int _sample;
        double _value;


        public Pulse(double freq)
        {
            _range = (int)(SampleRate / 2 / freq);
            _value = 0.5;
        }


        public double val()
        {
            if (_sample % _range == 0) _value *= -1;
            _sample++;
            return _value;
        }


        public Pulse freq(double freq)
        {
            _range = (int)(SampleRate / 2 / freq);
            return this;
        }
    }


    public class Saw : Gen
    {
        double _freq;
        double _phase;
        public Saw(double freqency)
        {
            _freq = freqency;
        }


        public double val()
        {
            _phase += _freq / SampleRate;
            _phase = (_phase > 1) ? -1 : _phase;
            return _phase;
        }


        public Saw freq(double frequency)
        {
            _freq = frequency;
            return this;
        }
    }


    public class Noise : Gen
    {
        Random _random = new Random();
        public double val()
        {
            return _random.NextDouble() * 2 - 1;
        }
    }


    public class Line : Gen
    {
        double _from;
        double _to;
        double _dur;
        double _length;
        int _sample;


        public Line()
        {
            _from = 0;
            _to = 0;
            _dur = 0;
            _length = 0;
            _sample = 0;
        }
        public Line(double f, double t, double d)
        {
            _from = f;
            _to = t;
            _dur = d;
            _length = _dur * SampleRate;
        }


        public double val()
        {
            _sample++;
            if (_sample > _length) return _to;
            return _from + (_to - _from) * ((float)_sample / _length);
        }


        public void set(double f, double t, double d)
        {
            _from = f;
            _to = t;
            _dur = d;
            _length = _dur * SampleRate;
            _sample = 0;
        }


        public void reset()
        {
            _sample = 0;
        }
        public bool done()
        {
            return (_sample > _length);
        }
    }


    public class Line2 : Gen
    {
        Line a = new Line();
        Line r = new Line();


        public Line2(double from, double to1, double dur1, double to2, double dur2)
        {
            a.set(from, to1, dur1);
            r.set(to1, to2, dur2);
        }


        public double val()
        {
            if (a.done())
            {
                return r.val();
            }
            return a.val();
        }


        public void reset()
        {
            a.reset();
            r.reset();
        }


        public bool done()
        {
            return r.done();
        }
    }


    public class Delay : Gen
    {
        int inputIndex, m;
        double delayTime, outputIndex, delta, feedback, output;
        double[] buffer;


        public Delay(double dt, double fb)
        {
            delayTime = dt;
            feedback = fb;
            inputIndex = 0;
            buffer = new double[SampleRate];
        }


        public double io(double input)
        {
            if (inputIndex >= SampleRate) inputIndex = 0;
            outputIndex = inputIndex - (SampleRate * delayTime);
            if (outputIndex < 0) outputIndex = SampleRate - 1 + outputIndex;


            m = (int)outputIndex;
            delta = outputIndex - (double)m;
            output = delta * buffer[m + 1] + (1.0 - delta) * buffer[m];


            buffer[inputIndex] = output * feedback + input;


            inputIndex++;


            return output;
        }


        public Delay setDelayTime(double dt)
        {
            delayTime = dt;
            return this;
        }
    }


    public class Filter : Gen
    {
        public double[] a = new double[3];
        public double[] b = new double[3];
        public double[] i = new double[2];
        public double[] o = new double[2];
        public double c, q;


        public double io(double input)
        {
            double output = b[0] * input + b[1] * i[0] + b[2] * i[1] - a[1] * o[0] - a[2] * o[1];


            i[1] = i[0];
            i[0] = input;
            o[1] = o[0];
            o[0] = output;


            return output;
        }
    }


    public class LPF : Filter /* q = 0.2 ~ 2.0 */
    {
        public LPF(double cutoff, double Q)
        {
            i[0] = i[1] = o[0] = o[1] = 0;
            c = cutoff / SampleRate;
            q = Q / Math.Sqrt(2);
            IIR_LPF();
        }


        public void cutoff(double cutoff, double Q)
        {
            c = cutoff / SampleRate;
            q = Q / Math.Sqrt(2);
            IIR_LPF();
        }


        void IIR_LPF()
        {
            c = Math.Tan(Math.PI * c) / (2.0 * Math.PI);


            a[0] = 1.0 + 2.0 * Math.PI * c / q + 4.0 * Math.PI * Math.PI * c * c;
            a[1] = (8.0 * Math.PI * Math.PI * c * c - 2.0) / a[0];
            a[2] = (1.0 - 2.0 * Math.PI * c / q + 4.0 * Math.PI * Math.PI * c * c) / a[0];
            b[0] = 4.0 * Math.PI * Math.PI * c * c / a[0];
            b[1] = 8.0 * Math.PI * Math.PI * c * c / a[0];
            b[2] = 4.0 * Math.PI * Math.PI * c * c / a[0];


            a[0] = 1.0;
        }
    }


    public class HPF : Filter
    {
        public HPF(double cutoff, double q)
        {
            i[0] = i[1] = o[0] = o[1] = 0;
            cutoff = cutoff / (double)SampleRate;
            q = q / Math.Sqrt(2);
            IIR_HPF(cutoff, q, a, b);
        }


        public void cutoff(double cutoff, double q)
        {
            cutoff = cutoff / (double)SampleRate;
            q = q / Math.Sqrt(2);
            IIR_HPF(cutoff, q, a, b);
        }


        void IIR_HPF(double fc, double Q, double[] a, double[] b)
        {
            fc = Math.Tan(Math.PI * fc) / (2.0 * Math.PI);

            a[0] = 1.0 + 2.0 * Math.PI * fc / Q + 4.0 * Math.PI * Math.PI * fc * fc;
            a[1] = (8.0 * Math.PI * Math.PI * fc * fc - 2.0) / a[0];
            a[2] = (1.0 - 2.0 * Math.PI * fc / Q + 4.0 * Math.PI * Math.PI * fc * fc) / a[0];
            b[0] = 1.0 / a[0];
            b[1] = -2.0 / a[0];
            b[2] = 1.0 / a[0];

            a[0] = 1.0;
        }
    }


    public class LP
    {
        double[] i = new double[3];
        double[] o = new double[3];
        double a1, a2, a3, b1, b2;


        public LP(double freq, double res)
        {
            set(freq, res);
        }


        public void set(double freq, double res)
        {
            double c = 1.0 / Math.Tan(Math.PI * freq / 44100.0);
            a1 = 1.0 / (1.0 + res * c + c * c);
            a2 = 2 * a1;
            a3 = a1;
            b1 = 2.0 * (1.0 - c * c) * a1;
            b2 = (1.0 - res * c + c * c) * a1;
        }


        public double io(double input)
        {
            i[2] = input;
            o[2] = a1 * i[2] + a2 * i[1] + a3 * i[0] - b1 * o[1] - b2 * o[0];


            o[0] = o[1];
            o[1] = o[2];
            i[0] = i[1];
            i[1] = i[2];


            return o[2];
        }
    }


    public class HP
    {
        double[] i = new double[3];
        double[] o = new double[3];
        double a1, a2, a3, b1, b2;


        public HP(double freq, double res)
        {
            set(freq, res);
        }


        public void set(double freq, double res)
        {
            double c = Math.Tan(Math.PI * freq / 44100);
            a1 = 1.0 / (1.0 + res * c + c * c);
            a2 = -2 * a1;
            a3 = a1;
            b1 = 2.0 * (c * c - 1.0) * a1;
            b2 = (1.0 - res * c + c * c) * a1;
        }


        public double io(double input)
        {
            i[2] = input;
            o[2] = a1 * i[2] + a2 * i[1] + a3 * i[0] - b1 * o[1] - b2 * o[0];


            o[0] = o[1];
            o[1] = o[2];
            i[0] = i[1];
            i[1] = i[2];


            return o[2];
        }
    }


    public class Play
    {
        double[] sampleBuffer;
        int ch, frame, n;
        public float speed { get; set; }
        public double phase { get; set; }
        double delta;
        public bool loop { get; set; }


        public Play(string file)
        {
            WaveFileReader reader = new WaveFileReader(file);
            sampleBuffer = new double[reader.Length];
            float[] buffer;
            while ((buffer = reader.ReadNextSampleFrame()) != null)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    sampleBuffer[n++] = buffer[i];
                }
            }
            WaveFormat format = reader.WaveFormat;
            ch = format.Channels;
            frame = sampleBuffer.Length / ch;
            n = 0;
            speed = 1;
        }

        public double val()
        {
            if (phase >= frame)
            {
                if (loop) phase = 0;
                else return 0;
            }
            else
            {
                phase = phase + speed;
            }
            if (phase < 0) phase += frame;
            n = (int)phase;
            delta = phase - (double)n;
            return delta * sampleBuffer[n + ch] + (1.0 - delta) * sampleBuffer[n];
        }
    }
}