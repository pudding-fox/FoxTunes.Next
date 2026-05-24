using FoxTunes.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes
{
    public static class MoodBarHost
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
                return typeof(MoodBarHost).Assembly.Location;
            }
        }

        public static readonly object ReadSyncRoot = new object();

        public static readonly object WriteSyncRoot = new object();

        public static void Init()
        {
            LogManager.FileName = Path.Combine(
                Publication.StoragePath,
                string.Format(
                    "Log_MoodBar_{0}.txt",
                    DateTime.UtcNow.ToFileTime()
                )
            );
            AssemblyResolver.Instance.Enable();
        }

        public static async Task<int> Create()
        {
            using (Stream input = Console.OpenStandardInput(), output = Console.OpenStandardOutput(), error = Console.OpenStandardError())
            {
                try
                {
                    await Create(input, output, error).ConfigureAwait(false);
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

        private static async Task Create(Stream input, Stream output, Stream error)
        {
            var setup = new CoreSetup();
            setup.Disable(ComponentSlots.All);
            setup.Enable(ComponentSlots.Configuration);
            setup.Enable(ComponentSlots.Logger);
            using (var core = new Core(setup))
            {
                try
                {
                    core.Load();
                    core.Initialize();
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(MoodBarHost), LogLevel.Error, "Failed to initialize core: {0}", e.Message);
                    throw;
                }
                try
                {
                    Logger.Write(typeof(MoodBarHost), LogLevel.Debug, "Begin reading items.");
                    var moodBarItems = ReadInput<MoodBarItem[]>(input);
                    Logger.Write(typeof(MoodBarHost), LogLevel.Debug, "Read {0} items.", moodBarItems.Length);
                    using (var moodBar = new MoodBar(moodBarItems))
                    {
                        moodBar.InitializeComponent(core);
                        await Create(moodBar, input, output, error).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(MoodBarHost), LogLevel.Error, "Failed to create items: {0}", e.Message);
                    throw;
                }
            }
        }

        private static async Task Create(IMoodBar moodBar, Stream input, Stream output, Stream error)
        {
#if NET40
            var task1 = TaskEx.Run(async () =>
#else
            var task1 = Task.Run(async () =>
#endif
            {
                try
                {
                    Logger.Write(typeof(MoodBarHost), LogLevel.Debug, "Starting moodbar main thread.");
                    await moodBar.Create().ConfigureAwait(false);
                    Logger.Write(typeof(MoodBarHost), LogLevel.Debug, "Finished moodbar main thread.");
                    WriteOutput(output, new MoodBarStatus(MoodBarStatusType.Complete));
                    error.Flush();
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(MoodBarHost), LogLevel.Error, "Error on moodbar main thread: {0}", e.Message);
                    WriteOutput(output, new MoodBarStatus(MoodBarStatusType.Error));
                    new StreamWriter(error).Write(e.Message);
                    error.Flush();
                }
            });
#if NET40
            var task2 = TaskEx.Run(async () =>
#else
            var task2 = Task.Run(async () =>
#endif
            {
                try
                {
                    Logger.Write(typeof(MoodBarHost), LogLevel.Debug, "Starting moodbar output thread.");
                    while (await Task.WhenAny(task1, Task.Delay(100)).ConfigureAwait(false) != task1)
                    {
                        ProcessOutput(moodBar, output);
                    }
                    ProcessOutput(moodBar, output);
                    Logger.Write(typeof(MoodBarHost), LogLevel.Debug, "Finished moodbar output thread.");
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(MoodBarHost), LogLevel.Error, "Error on moodbar output thread: {0}", e.Message);
                }
            });
#if NET40
            var task3 = TaskEx.Run(async () =>
#else
            var task3 = Task.Run(async () =>
#endif
            {
                var exit = default(bool);
                try
                {
                    Logger.Write(typeof(MoodBarHost), LogLevel.Debug, "Starting moodbar input thread.");
                    while (await Task.WhenAny(task1, Task.Delay(100)).ConfigureAwait(false) != task1)
                    {
                        ProcessInput(moodBar, input, output, out exit);
                        if (exit)
                        {
                            break;
                        }
                    }
                    if (!exit)
                    {
                        ProcessInput(moodBar, input, output, out exit);
                    }
                    Logger.Write(typeof(MoodBarHost), LogLevel.Debug, "Finished moodbar input thread.");
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(MoodBarHost), LogLevel.Error, "Error on moodbar input thread: {0}", e.Message);
                }
            });
            await Task.WhenAll(task1, task2, task3).ConfigureAwait(false);
        }

        private static void ProcessInput(IMoodBar moodBar, Stream input, Stream output, out bool exit)
        {
            Logger.Write(typeof(MoodBarHost), LogLevel.Debug, "Begin reading command.");
            var command = ReadInput<MoodBarCommand>(input);
            Logger.Write(typeof(MoodBarHost), LogLevel.Debug, "Read command: {0}", Enum.GetName(typeof(MoodBarCommandType), command.Type));
            switch (command.Type)
            {
                case MoodBarCommandType.Cancel:
                    Logger.Write(typeof(MoodBarHost), LogLevel.Debug, "Sending cancellation signal to MoodBar.");
                    moodBar.Cancel();
                    Logger.Write(typeof(MoodBarHost), LogLevel.Debug, "Closing stdin.");
                    input.Close();
                    exit = true;
                    break;
                case MoodBarCommandType.Quit:
                    Logger.Write(typeof(MoodBarHost), LogLevel.Debug, "Closing stdin/stdout.");
                    input.Close();
                    output.Close();
                    exit = true;
                    break;
                default:
                    exit = false;
                    break;
            }
        }

        private static void ProcessOutput(IMoodBar moodBar, Stream output)
        {
            WriteOutput(output, moodBar.MoodBarItems);
            moodBar.Prune();
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
    }
}
