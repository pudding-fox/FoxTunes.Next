using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FoxTunes
{
    public static class BassEncoderHost
    {
        private static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static string Location
        {
            get
            {
                return typeof(BassEncoderHost).Assembly.Location;
            }
        }

        const int INTERVAL = 1000;

        const int TIMEOUT = 10000;

        public static readonly object ReadSyncRoot = new object();

        public static readonly object WriteSyncRoot = new object();

        public static void Init()
        {
            LogManager.FileName = Path.Combine(
                Publication.StoragePath,
                string.Format(
                    "Log_Converter_{0}.txt",
                    DateTime.UtcNow.ToFileTime()
                )
            );
            AssemblyResolver.Instance.EnableExecution();
            AssemblyResolver.Instance.EnableReflectionOnly();
        }

        public static int Encode()
        {
            using (Stream input = Console.OpenStandardInput(), output = Console.OpenStandardOutput(), error = Console.OpenStandardError())
            {
                try
                {
                    Encode(input, output, error);
                    return 0;
                }
                catch (Exception e)
                {
                    new StreamWriter(error).Write(e.Message);
                    error.Flush();
                    return -1;
                }
            }
        }

        private static void Encode(Stream input, Stream output, Stream error)
        {
            var setup = new CoreSetup();
            setup.Disable(ComponentSlots.All);
            setup.Enable(ComponentSlots.Configuration);
            setup.Enable(ComponentSlots.Logger);
            using (var core = new Core(setup))
            {
                try
                {
                    core.Load(new[]
                    {
                        //Stub component used to provide the configuration.
                        new BassEncoderBehaviourStub()
                    });
                    core.Initialize();
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Error, "Failed to initialize core: {0}", e.Message);
                    throw;
                }
                try
                {
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Debug, "Begin reading items.");
                    var encoderItems = ReadInput<EncoderItem[]>(input);
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Debug, "Read {0} items.", encoderItems.Length);
                    using (var encoder = new BassEncoder(encoderItems))
                    {
                        encoder.InitializeComponent(core);
                        Encode(encoder, input, output, error);
                    }
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Error, "Failed to encode items: {0}", e.Message);
                    throw;
                }
            }
        }

        private static void Encode(IBassEncoder encoder, Stream input, Stream output, Stream error)
        {
            var thread1 = new Thread(() =>
            {
                try
                {
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Debug, "Starting encoder main thread.");
                    encoder.Encode();
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Debug, "Finished encoder main thread.");
                    WriteOutput(output, new EncoderStatus(EncoderStatusType.Complete));
                    error.Flush();
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Error, "Error on encoder main thread: {0}", e.Message);
                    WriteOutput(output, new EncoderStatus(EncoderStatusType.Error));
                    new StreamWriter(error).Write(e.Message);
                    error.Flush();
                }
            })
            {
                IsBackground = true
            };
            var thread2 = new Thread(() =>
            {
                try
                {
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Debug, "Starting encoder output thread.");
                    while (thread1.IsAlive)
                    {
                        ProcessOutput(encoder, output);
                        Thread.Sleep(INTERVAL);
                    }
                    ProcessOutput(encoder, output);
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Debug, "Finished encoder output thread.");
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Error, "Error on encoder output thread: {0}", e.Message);
                }
            })
            {
                IsBackground = true
            };
            var thread3 = new Thread(() =>
            {
                var exit = default(bool);
                try
                {
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Debug, "Starting encoder input thread.");
                    while (thread1.IsAlive)
                    {
                        ProcessInput(encoder, input, output, out exit);
                        if (exit)
                        {
                            break;
                        }
                        Thread.Sleep(INTERVAL);
                    }
                    if (!exit)
                    {
                        ProcessInput(encoder, input, output, out exit);
                    }
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Debug, "Finished encoder input thread.");
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Error, "Error on encoder input thread: {0}", e.Message);
                }
            })
            {
                IsBackground = true
            };
            thread1.Start();
            thread2.Start();
            thread3.Start();
            thread1.Join();
            if (!thread2.Join(TIMEOUT))
            {
                Logger.Write(typeof(BassEncoderHost), LogLevel.Warn, "Encoder output thread did not complete gracefully, aborting.");
                thread2.Abort();
            }
            if (!thread3.Join(TIMEOUT))
            {
                Logger.Write(typeof(BassEncoderHost), LogLevel.Warn, "Encoder input thread did not complete gracefully, aborting.");
                thread3.Abort();
            }
        }

        private static void ProcessInput(IBassEncoder encoder, Stream input, Stream output, out bool exit)
        {
            Logger.Write(typeof(BassEncoderHost), LogLevel.Debug, "Begin reading command.");
            var command = ReadInput<EncoderCommand>(input);
            Logger.Write(typeof(BassEncoderHost), LogLevel.Debug, "Read command: {0}", Enum.GetName(typeof(EncoderCommandType), command.Type));
            switch (command.Type)
            {
                case EncoderCommandType.Cancel:
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Debug, "Sending cancellation signal to encoder.");
                    encoder.Cancel();
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Debug, "Closing stdin.");
                    input.Close();
                    exit = true;
                    break;
                case EncoderCommandType.Quit:
                    Logger.Write(typeof(BassEncoderHost), LogLevel.Debug, "Closing stdin/stdout.");
                    input.Close();
                    output.Close();
                    exit = true;
                    break;
                default:
                    exit = false;
                    break;
            }
        }

        private static void ProcessOutput(IBassEncoder encoder, Stream output)
        {
            WriteOutput(output, encoder.EncoderItems);
        }

        private static T ReadInput<T>(Stream stream)
        {
            lock (ReadSyncRoot)
            {
                var input = Serializer.Instance.Read(stream);
                return (T)input;
            }
        }

        private static void WriteOutput(Stream stream, object value)
        {
            lock (WriteSyncRoot)
            {
                Serializer.Instance.Write(stream, value);
                stream.Flush();
            }
        }

        private class BassEncoderBehaviourStub : BaseComponent, IConfigurableComponent
        {
            public IEnumerable<ConfigurationSection> GetConfigurationSections()
            {
                return BassEncoderBehaviourConfiguration.GetConfigurationSections();
            }
        }
    }
}
