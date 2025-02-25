using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public class BassOutputStreamDataSource : BaseComponent, IOutputStreamDataSource
    {
        public BassOutputStreamDataSource(IOutputStream outputStream)
        {
            this.OutputStream = (IBassOutputStream)outputStream;
        }

        public IBassOutputStream OutputStream { get; private set; }

        IOutputStream IOutputStreamDataSource.Stream
        {
            get
            {
                return this.OutputStream;
            }
        }

        public bool GetFormat(out int rate, out int channels, out OutputStreamFormat format)
        {
            rate = this.OutputStream.Rate;
            channels = this.OutputStream.Channels;
            format = this.OutputStream.Format;
            return true;
        }

        public bool GetChannelMap(out IDictionary<int, OutputChannel> channels)
        {
            channels = BassChannelMap.GetChannelMap(this.OutputStream.Channels);
            return true;
        }

        public T[] GetBuffer<T>(TimeSpan duration) where T : struct
        {
            return this.OutputStream.GetBuffer<T>(duration);
        }

        public int GetData(short[] buffer)
        {
            return this.OutputStream.GetData(buffer);
        }

        public int GetData(float[] buffer)
        {
            return this.OutputStream.GetData(buffer);
        }

        public float[] GetBuffer(int fftSize, bool individual = false)
        {
            var length = default(int);
            switch (fftSize)
            {
                case BassFFT.FFT256:
                    length = BassFFT.FFT256_SIZE;
                    break;
                case BassFFT.FFT512:
                    length = BassFFT.FFT512_SIZE;
                    break;
                case BassFFT.FFT1024:
                    length = BassFFT.FFT1024_SIZE;
                    break;
                case BassFFT.FFT2048:
                    length = BassFFT.FFT2048_SIZE;
                    break;
                case BassFFT.FFT4096:
                    length = BassFFT.FFT4096_SIZE;
                    break;
                case BassFFT.FFT8192:
                    length = BassFFT.FFT8192_SIZE;
                    break;
                case BassFFT.FFT16384:
                    length = BassFFT.FFT16384_SIZE;
                    break;
                case BassFFT.FFT32768:
                    length = BassFFT.FFT32768_SIZE;
                    break;
                default:
                    throw new NotImplementedException();
            }
            if (individual)
            {
                length *= this.OutputStream.Channels;
            }
            return new float[length];
        }

        public int GetData(float[] buffer, int fftSize, bool individual = false)
        {
            var channelHandle = this.OutputStream.Stream.ChannelHandle;
            var length = BassStreamOutput.GetFFTLength(fftSize, individual);
            return Bass.ChannelGetData(channelHandle, buffer, unchecked((int)length));
        }

        public int GetData(float[] buffer, int fftSize, out TimeSpan duration, bool individual = false)
        {
            var channelHandle = this.OutputStream.Stream.ChannelHandle;
            var length = BassStreamOutput.GetFFTLength(fftSize, individual);
            var bytes = Bass.ChannelGetData(channelHandle, buffer, unchecked((int)length));
            duration = TimeSpan.FromSeconds(Bass.ChannelBytes2Seconds(channelHandle, bytes));
            return bytes;
        }
    }
}
