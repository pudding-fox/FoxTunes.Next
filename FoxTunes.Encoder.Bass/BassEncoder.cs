using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class BassEncoder : BassTool, IBassEncoder
    {
        public BassEncoder(IEnumerable<EncoderItem> encoderItems)
        {
            this.EncoderItems = encoderItems;
        }

        public IEnumerable<EncoderItem> EncoderItems { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            ComponentRegistry.Instance.GetComponent<IConfiguration>().GetElement<IntegerConfigurationElement>(
                BassEncoderBehaviourConfiguration.SECTION,
                BassEncoderBehaviourConfiguration.THREADS_ELEMENT
            ).ConnectValue(value => this.Threads = value);
            base.InitializeComponent(core);
        }

        public void Encode()
        {
            if (this.Threads > 1)
            {
                Logger.Write(this, LogLevel.Debug, "Beginning parallel encoding with {0} threads.", this.Threads);
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Beginning single threaded encoding.");
            }
            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = this.Threads
            };
            Parallel.ForEach(this.EncoderItems, parallelOptions, encoderItem =>
            {
                try
                {
                    var settings = ComponentRegistry.Instance.GetComponent<BassEncoderSettingsFactory>().CreateSettings(encoderItem.Profile);
                    if (this.CancellationToken.IsCancellationRequested)
                    {
                        Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\" due to cancellation.", encoderItem.InputFileName);
                        encoderItem.Status = EncoderItemStatus.Cancelled;
                        return;
                    }
                    if (!this.CheckInput(encoderItem.InputFileName))
                    {
                        Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\" due to output file \"{1}\" does not exist.", encoderItem.InputFileName, encoderItem.OutputFileName);
                        encoderItem.Status = EncoderItemStatus.Failed;
                        encoderItem.AddError(string.Format("Input file \"{0}\" does not exist.", encoderItem.OutputFileName));
                        return;
                    }
                    if (!this.CheckOutput(encoderItem.OutputFileName))
                    {
                        Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\": Output file \"{1}\" cannot be written.", encoderItem.InputFileName, encoderItem.OutputFileName);
                        encoderItem.Status = EncoderItemStatus.Failed;
                        encoderItem.AddError(string.Format("Output file \"{0}\" cannot be written.", encoderItem.OutputFileName));
                        return;
                    }
                    if (File.Exists(encoderItem.OutputFileName))
                    {
                        Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\": Output file \"{1}\" already exists.", encoderItem.InputFileName, encoderItem.OutputFileName);
                        encoderItem.Status = EncoderItemStatus.Failed;
                        encoderItem.AddError(string.Format("Output file \"{0}\" already exists.", encoderItem.OutputFileName));
                        return;
                    }
                    Logger.Write(this, LogLevel.Debug, "Beginning encoding file \"{0}\" to output file \"{1}\".", encoderItem.InputFileName, encoderItem.OutputFileName);
                    encoderItem.Progress = EncoderItem.PROGRESS_NONE;
                    encoderItem.Status = EncoderItemStatus.Processing;
                    this.Encode(encoderItem, settings);
                    if (encoderItem.Status == EncoderItemStatus.Complete)
                    {
                        Logger.Write(this, LogLevel.Debug, "Encoding file \"{0}\" to output file \"{1}\" completed successfully.", encoderItem.InputFileName, encoderItem.OutputFileName);
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Warn, "Encoding file \"{0}\" failed: Unknown error.", encoderItem.InputFileName);
                    }
                }
                catch (OperationCanceledException)
                {
                    Logger.Write(this, LogLevel.Warn, "Skipping file \"{0}\" due to cancellation.", encoderItem.InputFileName);
                    encoderItem.Status = EncoderItemStatus.Cancelled;
                    return;
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Encoding file \"{0}\" failed: {1}", encoderItem.InputFileName, e.Message);
                    encoderItem.Status = EncoderItemStatus.Failed;
                    encoderItem.AddError(e.Message);
                }
                finally
                {
                    encoderItem.Progress = EncoderItem.PROGRESS_COMPLETE;
                }
            });
            Logger.Write(this, LogLevel.Debug, "Encoding completed successfully.");
        }

        protected virtual void Encode(EncoderItem encoderItem, IBassEncoderSettings settings)
        {
            var flags = BassFlags.Decode;
            if (this.ShouldDecodeFloat(encoderItem, settings))
            {
                Logger.Write(this, LogLevel.Debug, "Decoding file \"{0}\" in high quality mode (32 bit floating point).", encoderItem.InputFileName);
                flags |= BassFlags.Float;
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Decoding file \"{0}\" in standaed quality mode (16 bit integer).", encoderItem.InputFileName);
            }
            using (var stream = this.CreateStream(encoderItem.InputFileName, flags))
            {
                if (stream.IsEmpty)
                {
                    Logger.Write(this, LogLevel.Debug, "Failed to create stream for file \"{0}\": Unknown error.", encoderItem.InputFileName);
                    return;
                }
                Logger.Write(this, LogLevel.Debug, "Created stream for file \"{0}\": {1}", encoderItem.InputFileName, stream.ChannelHandle);
                if (settings is IBassEncoderTool)
                {
                    if (this.ShouldResample(encoderItem, stream, settings))
                    {
                        this.EncodeWithResampler(encoderItem, stream, settings as IBassEncoderTool);
                    }
                    else
                    {
                        this.Encode(encoderItem, stream, settings as IBassEncoderTool);
                    }
                }
                else if (settings is IBassEncoderHandler)
                {
                    if (this.ShouldResample(encoderItem, stream, settings))
                    {
                        this.EncodeWithResampler(encoderItem, stream, settings as IBassEncoderHandler);
                    }
                    else
                    {
                        this.Encode(encoderItem, stream, settings as IBassEncoderHandler);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            if (this.CancellationToken.IsCancellationRequested)
            {
                encoderItem.Status = EncoderItemStatus.Cancelled;
                if (File.Exists(encoderItem.OutputFileName))
                {
                    Logger.Write(this, LogLevel.Debug, "Deleting incomplete output \"{0}\": Cancelled.", encoderItem.OutputFileName);
                    File.Delete(encoderItem.OutputFileName);
                }
            }
            else
            {
                encoderItem.Status = EncoderItemStatus.Complete;
            }
        }

        protected virtual void Encode(EncoderItem encoderItem, IBassStream stream, IBassEncoderTool settings)
        {
            using (var encoderProcess = this.CreateEncoderProcess(encoderItem, stream, settings))
            {
                this.Encode(encoderItem, stream, encoderProcess);
                if (this.WaitForExit(encoderProcess))
                {
                    if (encoderProcess.ExitCode != 0)
                    {
                        throw new InvalidOperationException("Process does not indicate success.");
                    }
                }
            }
        }

        protected virtual void Encode(EncoderItem encoderItem, IBassStream stream, Process encoderProcess)
        {
            var channelReader = new ChannelReader(encoderItem, stream);
            var encoderWriter = new ProcessWriter(encoderProcess);
            var thread = new Thread(() =>
            {
                this.Try(() => channelReader.CopyTo(encoderWriter, this.CancellationToken), this.GetErrorHandler(encoderItem));
            })
            {
                Name = string.Format("ChannelReader(\"{0}\", {1})", encoderItem.InputFileName, stream.ChannelHandle),
                IsBackground = true
            };
            Logger.Write(this, LogLevel.Debug, "Starting background thread for file \"{0}\".", encoderItem.InputFileName);
            thread.Start();
            Logger.Write(this, LogLevel.Debug, "Completing background thread for file \"{0}\".", encoderItem.InputFileName);
            this.Join(thread);
        }

        protected virtual void EncodeWithResampler(EncoderItem encoderItem, IBassStream stream, IBassEncoderTool settings)
        {
            if (!Resampler.IsWindowsVista)
            {
                throw new InvalidOperationException(Strings.BassEncoder_UnsupportedResampling);
            }
            using (var resampler = ResamplerFactory.Create(encoderItem, stream, settings))
            {
                using (var encoderProcess = this.CreateEncoderProcess(encoderItem, stream, settings))
                {
                    var success = true;
                    this.EncodeWithResampler(encoderItem, stream, resampler.Process, encoderProcess);
                    if (this.WaitForExit(resampler.Process))
                    {
                        if (resampler.Process.ExitCode != 0)
                        {
                            success = false;
                        }
                    }
                    if (this.WaitForExit(encoderProcess))
                    {
                        if (encoderProcess.ExitCode != 0)
                        {
                            success = false;
                        }
                    }
                    if (!success)
                    {
                        throw new InvalidOperationException("Process does not indicate success.");
                    }
                }
            }
        }

        protected virtual void EncodeWithResampler(EncoderItem encoderItem, IBassStream stream, Process resamplerProcess, Process encoderProcess)
        {
            var channelReader = new ChannelReader(encoderItem, stream);
            var resamplerReader = new ProcessReader(resamplerProcess);
            var resamplerWriter = new ProcessWriter(resamplerProcess);
            var encoderWriter = new ProcessWriter(encoderProcess);
            var threads = new[]
            {
                new Thread(() =>
                {
                    this.Try(() => channelReader.CopyTo(resamplerWriter,this.CancellationToken), this.GetErrorHandler(encoderItem));
                })
                {
                    Name = string.Format("ChannelReader(\"{0}\", {1})", encoderItem.InputFileName, stream.ChannelHandle),
                    IsBackground = true
                },
                new Thread(() =>
                {
                    this.Try(() => resamplerReader.CopyTo(encoderWriter,this.CancellationToken), this.GetErrorHandler(encoderItem));
                })
                {
                    Name = string.Format("ProcessReader(\"{0}\", {1})", encoderItem.InputFileName, resamplerProcess.Id),
                    IsBackground = true
                }
            };
            Logger.Write(this, LogLevel.Debug, "Starting background threads for file \"{0}\".", encoderItem.InputFileName);
            foreach (var thread in threads)
            {
                thread.Start();
            }
            Logger.Write(this, LogLevel.Debug, "Completing background threads for file \"{0}\".", encoderItem.InputFileName);
            foreach (var thread in threads)
            {
                this.Join(thread);
            }
        }

        protected virtual void Encode(EncoderItem encoderItem, IBassStream stream, IBassEncoderHandler settings)
        {
            var channelReader = new ChannelReader(encoderItem, stream);
            var encoderWriter = settings.GetWriter(encoderItem, stream);
            var thread = new Thread(() =>
            {
                this.Try(() => channelReader.CopyTo(encoderWriter, this.CancellationToken), this.GetErrorHandler(encoderItem));
            })
            {
                Name = string.Format("ChannelReader(\"{0}\", {1})", encoderItem.InputFileName, stream.ChannelHandle),
                IsBackground = true
            };
            Logger.Write(this, LogLevel.Debug, "Starting background thread for file \"{0}\".", encoderItem.InputFileName);
            thread.Start();
            Logger.Write(this, LogLevel.Debug, "Completing background thread for file \"{0}\".", encoderItem.InputFileName);
            this.Join(thread);
        }

        protected virtual void EncodeWithResampler(EncoderItem encoderItem, IBassStream stream, IBassEncoderHandler settings)
        {
            if (!Resampler.IsWindowsVista)
            {
                throw new InvalidOperationException(Strings.BassEncoder_UnsupportedResampling);
            }
            using (var resampler = ResamplerFactory.Create(encoderItem, stream, settings))
            {
                var channelReader = new ChannelReader(encoderItem, stream);
                var resamplerReader = new ProcessReader(resampler.Process);
                var resamplerWriter = new ProcessWriter(resampler.Process);
                var encoderWriter = settings.GetWriter(encoderItem, stream);
                var threads = new[]
                {
                    new Thread(() =>
                    {
                        this.Try(() => channelReader.CopyTo(resamplerWriter,this.CancellationToken), this.GetErrorHandler(encoderItem));
                    })
                    {
                        Name = string.Format("ChannelReader(\"{0}\", {1})", encoderItem.InputFileName, stream.ChannelHandle),
                        IsBackground = true
                    },
                    new Thread(() =>
                    {
                        this.Try(() => resamplerReader.CopyTo(encoderWriter,this.CancellationToken), this.GetErrorHandler(encoderItem));
                    })
                    {
                        Name = string.Format("ProcessReader(\"{0}\", {1})", encoderItem.InputFileName, resampler.Process.Id),
                        IsBackground = true
                    }
                };
                Logger.Write(this, LogLevel.Debug, "Starting background threads for file \"{0}\".", encoderItem.InputFileName);
                foreach (var thread in threads)
                {
                    thread.Start();
                }
                Logger.Write(this, LogLevel.Debug, "Completing background threads for file \"{0}\".", encoderItem.InputFileName);
                foreach (var thread in threads)
                {
                    this.Join(thread);
                }
            }
        }

        protected virtual Process CreateEncoderProcess(EncoderItem encoderItem, IBassStream stream, IBassEncoderTool settings)
        {
            Logger.Write(this, LogLevel.Debug, "Creating encoder process for file \"{0}\".", encoderItem.InputFileName);
            var arguments = settings.GetArguments(encoderItem, stream);
            var process = this.CreateProcess(
                encoderItem,
                stream,
                settings.Executable,
                settings.Directory,
                arguments,
                true,
                false,
                true
            );
            Logger.Write(this, LogLevel.Debug, "Created encoder process for file \"{0}\": \"{1}\" {2}", encoderItem.InputFileName, settings.Executable, arguments);
            return process;
        }

        protected virtual Process CreateProcess(EncoderItem encoderItem, IBassStream stream, string executable, string directory, string arguments, bool redirectStandardInput, bool redirectStandardOutput, bool redirectStandardError)
        {
            if (!File.Exists(executable))
            {
                throw new InvalidOperationException(string.Format("A required utility was not found: {0}", executable));
            }
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = executable,
                WorkingDirectory = directory,
                Arguments = arguments,
                RedirectStandardInput = redirectStandardInput,
                RedirectStandardOutput = redirectStandardOutput,
                RedirectStandardError = redirectStandardError,
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
            if (redirectStandardError)
            {
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data))
                    {
                        return;
                    }
                    Logger.Write(this, LogLevel.Trace, "{0}: {1}", executable, e.Data);
                    encoderItem.AddError(e.Data);
                };
                process.BeginErrorReadLine();
            }
            return process;
        }

        protected virtual bool ShouldDecodeFloat(EncoderItem encoderItem, IBassEncoderSettings settings)
        {
            if (encoderItem.BitsPerSample == 0)
            {
                Logger.Write(this, LogLevel.Debug, "Suggesting high quality mode for file \"{0}\": Unknown bit depth.", encoderItem.InputFileName);
                return true;
            }
            else if (encoderItem.BitsPerSample == 1)
            {
                Logger.Write(this, LogLevel.Debug, "Suggesting high quality mode for file \"{0}\": dsd.", encoderItem.InputFileName);
                return true;
            }
            if (encoderItem.BitsPerSample > 16 || settings.Format.Depth > 16)
            {
                Logger.Write(this, LogLevel.Debug, "Suggesting high quality mode for file \"{0}\": >16 bit.", encoderItem.InputFileName);
                return true;
            }
            Logger.Write(this, LogLevel.Debug, "Suggesting standard quality mode for file \"{0}\": <=16 bit.", encoderItem.InputFileName);
            return false;
        }

        protected virtual bool ShouldResample(EncoderItem encoderItem, IBassStream stream, IBassEncoderSettings settings)
        {
            var channelInfo = default(ChannelInfo);
            if (!Bass.ChannelGetInfo(stream.ChannelHandle, out channelInfo))
            {
                throw new NotImplementedException();
            }
            var binaryFormat = default(BassEncoderBinaryFormat);
            var binaryEndian = BassEncoderBinaryEndian.Little;
            var depth = default(int);
            var rate = channelInfo.Frequency;
            var channels = channelInfo.Channels;
            if (channelInfo.Flags.HasFlag(BassFlags.Float))
            {
                binaryFormat = BassEncoderBinaryFormat.FloatingPoint;
                depth = BassEncoderSettings.DEPTH_32;
            }
            else
            {
                binaryFormat = BassEncoderBinaryFormat.SignedInteger;
                depth = BassEncoderSettings.DEPTH_16;
            }
            var result = default(bool);
            if (settings.Format.BinaryFormat != binaryFormat)
            {
                Logger.Write(this, LogLevel.Debug, "Resampling required for binary format: {0} => {1}", Enum.GetName(typeof(BassEncoderBinaryFormat), binaryFormat), Enum.GetName(typeof(BassEncoderBinaryFormat), settings.Format.BinaryFormat));
                result = true;
            }
            if (settings.Format.BinaryEndian != binaryEndian)
            {
                Logger.Write(this, LogLevel.Debug, "Resampling required for binary endian: {0} => {1}", Enum.GetName(typeof(BassEncoderBinaryEndian), binaryEndian), Enum.GetName(typeof(BassEncoderBinaryEndian), settings.Format.BinaryEndian));
                result = true;
            }
            if (settings.GetDepth(encoderItem, stream) != depth)
            {
                Logger.Write(this, LogLevel.Debug, "Resampling required for depth: {0} => {1}", depth, settings.GetDepth(encoderItem, stream));
                result = true;
            }
            if (settings.GetRate(encoderItem, stream) != rate)
            {
                Logger.Write(this, LogLevel.Debug, "Resampling required for rate: {0} => {1}", rate, settings.GetRate(encoderItem, stream));
                result = true;
            }
            if (settings.GetChannels(encoderItem, stream) != channels)
            {
                Logger.Write(this, LogLevel.Debug, "Resampling required for channels: {0} => {1}", channels, settings.GetChannels(encoderItem, stream));
                result = true;
            }
            return result;
        }

        protected virtual Action<Exception> GetErrorHandler(EncoderItem encoderItem)
        {
            return e =>
            {
                Logger.Write(this, LogLevel.Warn, "Encoder background thread for file \"{0}\" error: {1}", encoderItem.InputFileName, e.Message);
                encoderItem.AddError(e.Message);
            };
        }
    }
}
