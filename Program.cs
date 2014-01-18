using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio;
using NAudio.Wave;

namespace NAudioTest
{
	class Program
	{

		public static int SampleRate = 44100;
		public static int Channels = 2;

		public static WaveFileWriter waveFileWriter;

		static void Main(string[] args)
		{
			waveFileWriter = new WaveFileWriter(@"C:\rec\out.wav", new WaveFormat(SampleRate, Channels));

			var sound = new MySound();
			sound.SetWaveFormat(SampleRate, Channels);
			WaveOut waveOut = new WaveOut();
			waveOut.Init(sound);
			waveOut.Play();

			Console.ReadKey();

			waveOut.Stop();
			waveOut.Dispose();

			waveFileWriter.Close();
			waveFileWriter.Dispose();

		}
	}

	public abstract class WaveProvider : IWaveProvider
	{
		private WaveFormat waveFormat;

		public WaveProvider()
			: this(Program.SampleRate, Program.Channels)
		{
		}

		public WaveProvider(int sampleRate, int channels)
		{
			SetWaveFormat(sampleRate, channels);
		}

		public void SetWaveFormat(int sampleRate, int channels)
		{
			this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
		}

		public int Read(byte[] buffer, int offset, int count)
		{
			WaveBuffer waveBuffer = new WaveBuffer(buffer);
			int samplesRequired = count / 4;
			int samplesRead = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired);
			return samplesRead * 4;
		}

		public abstract int Read(float[] buffer, int offset, int sampleCount);

		public WaveFormat WaveFormat
		{
			get { return waveFormat; }
		}
	}

	public class MySound : WaveProvider
	{
		double L;
		double R;
		long count;
		Random random = new Random();

		Pulse pulse1 = new Pulse(20);
		AR pulse1Amp = new AR(0, 1, 0.01, 0, 0.09);

		Triangle tri1 = new Triangle(14000);
		AR tri1Amp = new AR(0, 1, 0.0001, 0, 0.004);

		Sine kick = new Sine(0);
		Line kickEnv = new Line(700, 50, 0.02);
		AR kickAmp = new AR(0, 1, 0.01, 0, 0.1);

		Noise snare = new Noise();
		AR snareAmp = new AR(0, 1, 0.001, 0, 0.05);

		int[] sawFreq = new int[16];
		Saw saw1 = new Saw(0);
		Saw saw2 = new Saw(0);
		Saw saw3 = new Saw(0);
		AR sawAmp = new AR(0, 1, 0.01, 0, 0.1);
		int sawSeq = 0;
		Delay sawDelay = new Delay(0.3, 0.5);

		public MySound()
		{
			for (var i = 0; i < 16; i++)
			{
				sawFreq[i] = random.Next(12, 36);
			}
		}

		public override int Read(float[] buffer, int offset, int sampleCount)
		{
			for (int n = 0; n < sampleCount;)
			{
				if (trigger(0.1))
				{
					if (random.Next(0, 10) == 0) { pulse1.freq(40); }
					else { pulse1.freq(20); }
					if (random.Next(0, 3) != 0) {
						pulse1Amp.reset();
					}

					tri1Amp.reset();
					
					saw1.freq(sawFreq[sawSeq] * 100);
					saw2.freq(sawFreq[sawSeq] * 100 + 10);
					saw3.freq(sawFreq[sawSeq] * 100 + 20);
					sawSeq++;
					if (sawSeq >= 16) sawSeq = 0;
					sawAmp.reset();
				}
				if (trigger(1.6*4))
				{
					for (var i = 0; i < 16; i++)
					{
						sawFreq[i] = random.Next(12, 36);
					}
				}

				if (trigger(0.4))
				{
					kickAmp.reset();
					kickEnv.reset();
				}

				if (trigger(0.8, 0.4))
				{
					snareAmp.reset();
				}

				L = pulse1.val() * pulse1Amp.val() * 0.15;
				L += tri1.val() * tri1Amp.val() * 0.1;
				L += kick.freq(kickEnv.val()).val() * kickAmp.val() * 0.3;
				L += snare.val() * snareAmp.val() * 0.1;
				L += sawDelay.io((saw1.val() + saw2.val() + saw3.val()) * sawAmp.val()) * 0.02;

				R = L;
	
				buffer[n++ + offset] = (float)L;
				buffer[n++ + offset] = (float)R;

				Program.waveFileWriter.WriteSample((float)L);
				Program.waveFileWriter.WriteSample((float)R);

				count++;
			}
			return sampleCount;
		}

		bool trigger(double sec, double offset = 0)
		{
			return count % (int)(Program.SampleRate * sec) == (int)(Program.SampleRate * offset);
		}
	}

	public class Sine
	{
		double _freq;
		double _phase;

		public Sine(float f)
		{
			_freq = f;
		}

		public double val()
		{
			_phase += _freq / Program.SampleRate;
			return Math.Sin(2 * Math.PI * _phase);
		}

		public double freq() { return _freq; }
		public Sine freq(double f)
		{
			_freq = f;
			return this;
		}
	}

	public class Triangle
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
			_value += (_freq / Program.SampleRate) * 4 * direction;
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

	public class Pulse
	{
		int _range;
		int _sample;
		double _value;

		public Pulse(double freq)
		{
			_range = (int)(Program.SampleRate / 2 / freq);
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
			_range = (int)(Program.SampleRate / 2 / freq);
			return this;
		}
	}

	public class Saw
	{
		double _freq;
		double _phase;
		public Saw(double freqency)
		{
			_freq = freqency;
		}

		public double val()
		{
			_phase += _freq / Program.SampleRate;
			_phase = (_phase > 1) ? -1 : _phase;
			return _phase;
		}

		public Saw freq(double frequency)
		{
			_freq = frequency;
			return this;
		}
	}

	public class Noise
	{
		Random _random = new Random();
		public double val()
		{
			return _random.NextDouble() * 2 - 1;
		}
	}

	public class Line
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
			_length = _dur * Program.SampleRate;
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
			_length = _dur * Program.SampleRate;
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

	public class AR
	{
		Line a = new Line();
		Line r = new Line();

		public AR(double from, double to1, double dur1, double to2, double dur2)
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

	public class Delay
	{
		int inputIndex, m;
		double delayTime, outputIndex, delta, feedback, output;
		double[] buffer = new double[Program.SampleRate];

		public Delay(double dt, double fb)
		{
			delayTime = dt;
			feedback = fb;
			inputIndex = 0;
		}

		public double io(double input)
		{
			if (inputIndex >= Program.SampleRate) inputIndex = 0;
			outputIndex = inputIndex - (Program.SampleRate * delayTime);
			if (outputIndex < 0) outputIndex = Program.SampleRate - 1 + outputIndex;

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
}
