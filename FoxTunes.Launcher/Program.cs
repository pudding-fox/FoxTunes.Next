using FoxTunes.Interfaces;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace FoxTunes.Launcher
{
    public static class Program
    {
        public static readonly TimeSpan SHUTDOWN_INTERVAL = TimeSpan.FromSeconds(1);

        public static readonly TimeSpan SHUTDOWN_TIMEOUT = TimeSpan.FromSeconds(10);

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(Program).Assembly.Location);
            }
        }

        static Program()
        {
            AssemblyResolver.Instance.EnableExecution();
            AssemblyResolver.Instance.EnableReflectionOnly();
        }

#if DEBUG

        private static void SetCulture(string name)
        {
            var culture = CultureInfo.CreateSpecificCulture(name);

            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;

#if NET40

#else
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
#endif
        }

#endif

        [STAThread]
        public static void Main(string[] args)
        {
            if (!string.Equals(Environment.CurrentDirectory, Location, StringComparison.OrdinalIgnoreCase))
            {
                //Since using relative paths for our data, we should always cwd to here.
                //This only matters when using shortcuts to portable release.
                Environment.CurrentDirectory = Location;
            }
#if DEBUG
            //SetCulture("fr");
#endif
            using (var server = new Server())
            {
                var unresponsive = default(bool);
                if (server.IsDisposed)
                {
                    ForwardCommandLine(out unresponsive);
                    if (!unresponsive)
                    {
                        //Nothing left to do, exit.
                        return;
                    }
                }
                using (var core = new Core(CoreSetup.Default))
                {
                    SetupErrorHandlers(core);
                    try
                    {
                        LoadCore(core);
                        if (unresponsive)
                        {
                            if (TerminateOtherInstances(core))
                            {
                                StartNewInstance();
                                return;
                            }
                        }
                        InitializeDatabase(core);
                        core.Initialize();
                        ProcessCommandLine(server, core);
                    }
                    catch (Exception e)
                    {
                        FatalError(core, e);
                        return;
                    }
                    try
                    {
                        ShowUI(core);
                        UnloadCore(core);
                    }
                    catch (Exception e)
                    {
                        FatalError(core, e);
                    }
                }
            }
        }

        private static void SetupErrorHandlers(ICore core)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (core.Components.UserInterface != null)
                {
                    core.Components.UserInterface.Fatal(e.ExceptionObject as Exception);
                }
            };
        }

        private static void LoadCore(ICore core)
        {
            core.Load();
        }

        private static void UnloadCore(ICore core)
        {
            core.Unload();
            if (core.Components.Output != null)
            {
                if (core.Components.Output.IsStarted)
                {
                    //TODO: Bad .Wait().
                    if (!core.Components.Output.Shutdown().Wait(SHUTDOWN_TIMEOUT))
                    {
                        //TODO: Warn.
                    }
                }
            }
            //TODO: Bad .Result
            if (!BackgroundTask.Shutdown(SHUTDOWN_INTERVAL, SHUTDOWN_TIMEOUT).Result)
            {
                //TODO: Warn.
            }
        }

        private static bool TerminateOtherInstances(ICore core)
        {
            if (!core.Components.UserInterface.Confirm(Strings.Program_Unresponsive))
            {
                return false;
            }
            var result = default(bool);
            var id = Process.GetCurrentProcess().Id;
            var name = Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location);
            foreach (var process in Process.GetProcessesByName(name))
            {
                if (process.Id == id)
                {
                    //Current process.
                    continue;
                }
                try
                {
                    process.Kill();
                    result = true;
                }
                catch
                {
                    //Nothing can be done.
                }
            }
            return result;
        }

        private static void InitializeDatabase(ICore core)
        {
            switch (core.Factories.Database.Test())
            {
                case DatabaseTestResult.OK:
                    //Nothing to do.
                    return;
                case DatabaseTestResult.Missing:
                    if (core.Factories.Database.Flags.HasFlag(DatabaseFactoryFlags.ConfirmCreate))
                    {
                        if (!core.Components.UserInterface.Confirm(Strings.Program_CreateDatabase))
                        {
                            throw new OperationCanceledException(Strings.Program_DatabaseCreationCancelled);
                        }
                    }
                    break;
                case DatabaseTestResult.Mismatch:
                    if (!core.Components.UserInterface.Confirm(Strings.Program_DatabaseMismatch))
                    {
                        throw new OperationCanceledException(Strings.Program_DatabaseCreationCancelled);
                    }
                    break;
            }
            core.Factories.Database.Initialize();
            if (core.Factories.Database.Test() != DatabaseTestResult.OK)
            {
                throw new InvalidOperationException(Strings.Program_DatabaseCreationFailed);
            }
            using (var database = core.Factories.Database.Create())
            {
                core.InitializeDatabase(database, DatabaseInitializeType.All);
            }
        }

        private static void ForwardCommandLine(out bool unresponsive)
        {
            var client = new Client();
            try
            {
                //TODO: Bad .Wait()
                client.Send(Environment.CommandLine).Wait();
                unresponsive = false;
            }
            catch
            {
                //Failed to forward command line to existing instance.
                unresponsive = true;
            }
        }

        private static void ProcessCommandLine(Server server, ICore core)
        {
            server.Message += (sender, e) =>
            {
                core.Managers.FileActionHandler.RunCommand(e.Message);
            };
            core.Managers.FileActionHandler.RunCommand(Environment.CommandLine);
        }

        private static void ShowUI(ICore core)
        {
            if (core.Components.UserInterface != null)
            {
                //TODO: Bad .Wait().
                core.Components.UserInterface.Show().Wait();
            }
        }

        private static void FatalError(ICore core, Exception e)
        {
            if (core.Components.UserInterface != null)
            {
                core.Components.UserInterface.Fatal(e);
            }
        }

        private static void StartNewInstance()
        {
            Process.Start(typeof(Program).Assembly.Location);
        }
    }
}
