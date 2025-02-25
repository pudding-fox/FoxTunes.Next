using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    public class FFTDataTransformer : BaseComponent, IFFTDataTransformer
    {
        public FFTDataTransformer(int[] bands)
        {
            this.Bands = bands;
            this.MinBand = bands[0];
            this.MaxBand = bands[bands.Length - 1];
        }

        public int[] Bands;

        public int MinBand;

        public int MaxBand;

        public void Transform(FFTVisualizationData source, float[] values, float[] peakValues, float[] rmsValues)
        {
            var position = default(int);

            for (int a = 0, b = 0; a < source.FFTSize; a++)
            {
                var frequency = IndexToFrequency(a, source.FFTSize, source.Rate);
                while (frequency > this.Bands[position])
                {
                    this.UpdateValue(source, values, peakValues, rmsValues, position, b, a);
                    if (position < (this.Bands.Length - 1))
                    {
                        b = a;
                        position++;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            var doPeaks = peakValues != null;
            var doRms = rmsValues != null;
            while (position < this.Bands.Length)
            {
                values[position] = 0f;
                if (doPeaks)
                {
                    peakValues[position] = 0f;
                }
                if (doRms)
                {
                    rmsValues[position] = 0f;
                }
                position++;
            }
        }

        protected virtual void UpdateValue(FFTVisualizationData source, float[] values, float[] peakValues, float[] rmsValues, int band, int start, int end)
        {
            var value = default(float);
            var peak = default(float);
            var rms = default(float);
            var doPeaks = peakValues != null;
            var doRms = rmsValues != null;
            var count = end - start;

            if (count > 0)
            {
                for (var a = start; a < end; a++)
                {
                    value = Math.Max(source.Data[0, a], value);
                    if (doPeaks)
                    {
                        peak = Math.Max(source.History.Peak[0, a], peak);
                    }
                    if (doRms)
                    {
                        rms = Math.Max(source.History.Rms[0, a], rms);
                    }
                }
            }
            else
            {
                //If we don't have data then average the closest available bins.
                start = Math.Max(start - 1, 0);
                end = Math.Min(end + 1, source.FFTSize);
                count = end - start;
                if (count == 0)
                {
                    //Sorry.
                    return;
                }
                for (var a = start; a < end; a++)
                {
                    value += source.Data[0, a];
                    if (doPeaks)
                    {
                        peak += source.History.Peak[0, a];
                    }
                    if (doRms)
                    {
                        rms += source.History.Rms[0, a];
                    }
                }
                value /= count;
                if (doPeaks)
                {
                    peak /= count;
                }
                if (doRms)
                {
                    rms /= count;
                }
            }

            values[band] = value;
            if (doPeaks)
            {
                peakValues[band] = peak;
            }
            if (doRms)
            {
                rmsValues[band] = rms;
            }
        }

        public static int IndexToFrequency(int index, int fftSize, int rate)
        {
            var frequency = (int)Math.Floor((double)index * (double)rate / (double)fftSize);
            if (frequency > rate / 2)
            {
                frequency = rate / 2;
            }
            return frequency;
        }

        public static int FrequencyToIndex(int frequency, int fftSize, int rate)
        {
            var index = (int)Math.Floor((double)fftSize * (double)frequency / (double)rate);
            if (index > fftSize / 2 - 1)
            {
                index = fftSize / 2 - 1;
            }
            return index;
        }
    }
}
