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
		Random rnd = new Random();

		Sine s1 = new Sine(440);
		Sine s2 = new Sine(890);
		AR a1 = new AR(0, 1, 0.0001, 0, 0.01);
		AR a2 = new AR(0, 1, 0.0001, 0, 0.01);

		public override int Read(float[] buffer, int offset, int sampleCount)
		{
			for (int n = 0; n < sampleCount;)
			{
				if (count % (int)(Program.SampleRate/16) == 0)
				{
					a1.reset();
					a2.reset();
				}
				L = s1.val() * a1.val() * 0.1f;
				R = s2.val() * a2.val() * 0.1f;

				buffer[n++ + offset] = (float)L;
				buffer[n++ + offset] = (float)R;

				Program.waveFileWriter.WriteSample((float)L);
				Program.waveFileWriter.WriteSample((float)R);

				count++;
			}
			return sampleCount;
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
			//_value = (float)(Math.Sin(2 * Math.PI *_sample * _freq / Program.SampleRate));
			//_sample++;
			// if (_sample >= Program.SampleRate) _sample = 0;
			//return _value;

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
}
