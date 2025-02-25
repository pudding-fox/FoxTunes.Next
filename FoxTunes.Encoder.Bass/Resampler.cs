using FoxTunes.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FoxTunes
{
    public class Resampler : BaseComponent, IDisposable
    {
        public static readonly bool IsWindowsVista = Environment.OSVersion.Version.Major >= 6;

        public static string FileName
        {
            get
            {
                var directory = Path.GetDirectoryName(
                    typeof(Resampler).Assembly.Location
                );
                return Path.Combine(directory, "Sox\\sox.exe");
            }
        }

        public static string WorkingDirectory
        {
            get
            {
                return Path.GetDirectoryName(FileName);
            }
        }

        public Resampler(ResamplerFormat inputFormat, ResamplerFormat outputFormat)
        {
            this.Process = CreateProcess(inputFormat, outputFormat);
            this.Process.ErrorDataReceived += this.OnErrorDataReceived;
            this.Process.BeginErrorReadLine();
        }

        public Process Process { get; private set; }

        protected virtual void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }
            Logger.Write(typeof(Resampler), LogLevel.Trace, "{0}: {1}", FileName, e.Data);
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.Process != null)
            {
                this.Process.ErrorDataReceived -= this.OnErrorDataReceived;
                this.Process.Dispose();
            }
        }

        ~Resampler()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }

        private static Process CreateProcess(ResamplerFormat inputFormat, ResamplerFormat outputFormat)
        {
            if (!File.Exists(FileName))
            {
                throw new InvalidOperationException(string.Format("A required utility was not found: {0}", FileName));
            }
            Logger.Write(typeof(Resampler), LogLevel.Debug, "Creating resampler process: {0} => {1}", inputFormat, outputFormat);
            var arguments = GetArguments(inputFormat, outputFormat);
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = FileName,
                WorkingDirectory = WorkingDirectory,
                Arguments = arguments,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(processStartInfo);
            try
            {
                process.PriorityClass = ProcessPriorityClass.BelowNormal;
            }
            catch
            {
                //Nothing can be done, probably access denied.
            }
            Logger.Write(typeof(Resampler), LogLevel.Debug, "Created resampler process: \"{0}\" {1}", FileName, arguments);
            return process;
        }

        private static string GetArguments(ResamplerFormat inputFormat, ResamplerFormat outputFormat)
        {
            var builder = new StringBuilder();
            builder.Append(GetArguments(inputFormat));
            builder.Append(" ");
            builder.Append(GetArguments(outputFormat));
            return builder.ToString();
        }

        private static string GetArguments(ResamplerFormat format)
        {
            var encoding = GetBinaryFormat(format.Format);
            var endian = GetBinaryEndian(format.Endian);
            return string.Format(
                "--type raw --encoding {0} --endian {1} --bits {2} --rate {3} --channels {4} -",
                encoding,
                endian,
                format.Depth,
                format.Rate,
                format.Channels
            );
        }

        private static string GetBinaryFormat(BassEncoderBinaryFormat binaryFormat)
        {
            switch (binaryFormat)
            {
                case BassEncoderBinaryFormat.SignedInteger:
                    return "signed-integer";
                case BassEncoderBinaryFormat.UnsignedInteger:
                    return "unsigned-integer";
                case BassEncoderBinaryFormat.FloatingPoint:
                    return "floating-point";
                default:
                    throw new NotImplementedException();
            }
        }
        private static string GetBinaryEndian(BassEncoderBinaryEndian binaryEndian)
        {
            switch (binaryEndian)
            {
                case BassEncoderBinaryEndian.Little:
                    return "little";
                case BassEncoderBinaryEndian.Big:
                    return "big";
                default:
                    throw new NotImplementedException();
            }
        }

        public class ResamplerFormat
        {
            public ResamplerFormat(BassEncoderBinaryFormat format, BassEncoderBinaryEndian endian, int depth, int rate, int channels)
            {
                this.Format = format;
                this.Endian = endian;
                this.Depth = depth;
                this.Rate = rate;
                this.Channels = channels;
            }

            public BassEncoderBinaryFormat Format { get; private set; }

            public BassEncoderBinaryEndian Endian { get; private set; }

            public int Depth { get; private set; }

            public int Rate { get; private set; }

            public int Channels { get; private set; }

            public override string ToString()
            {
                return string.Format(
                    "Format: {0}, Endian: {1}, Depth: {2}, Rate: {3}, Channels: {4}",
                    Enum.GetName(typeof(BassEncoderBinaryFormat), this.Format),
                    Enum.GetName(typeof(BassEncoderBinaryEndian), this.Endian),
                    this.Depth,
                    this.Rate,
                    this.Channels
                );
            }
        }
    }
}
