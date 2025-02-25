using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class Core : BaseComponent, ICore
    {
        public static ICore Instance { get; private set; }

        public static bool IsShuttingDown { get; private set; }

        private Core()
        {
            Instance = this;
            ComponentRegistry.Instance.Clear();
            IsShuttingDown = false;
        }

        public Core(ICoreSetup setup) : this()
        {
            this.Setup = setup;
            foreach (var slot in ComponentSlots.All)
            {
                if (!this.Setup.HasSlot(slot))
                {
                    ComponentResolver.Instance.Add(slot, ComponentSlots.Blocked);
                }
            }
        }

        public ICoreSetup Setup { get; private set; }

        public IStandardComponents Components
        {
            get
            {
                return StandardComponents.Instance;
            }
        }

        public IStandardManagers Managers
        {
            get
            {
                return StandardManagers.Instance;
            }
        }

        public IStandardFactories Factories
        {
            get
            {
                return StandardFactories.Instance;
            }
        }

        public void Load()
        {
            this.Load(Enumerable.Empty<IBaseComponent>());
        }

        public void Load(IEnumerable<IBaseComponent> components)
        {
            try
            {
                this.LoadComponents();
                ComponentRegistry.Instance.AddComponents(components);
                this.LoadConfiguration();
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Debug, "Failed to load the core, we will crash soon: {0}", e.Message);
                throw;
            }
        }

        public void Initialize()
        {
            try
            {
                this.InitializeComponents();
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Debug, "Failed to initialize the core, we will crash soon: {0}", e.Message);
                throw;
            }
        }

        protected virtual void LoadComponents()
        {
#if DEBUG
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            ComponentRegistry.Instance.AddComponents(
                ComponentActivator.Instance.Activate(
                    ComponentScanner.Instance.GetComponents()
                )
            );
#if DEBUG
            stopwatch.Stop();
            Logger.Write(this, LogLevel.Trace, "Components loaded in {0}ms.", stopwatch.ElapsedMilliseconds);
#endif
        }

        protected virtual void LoadConfiguration()
        {
#if DEBUG
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            var components = ComponentRegistry.Instance.GetComponents<IConfigurableComponent>();
            var sections = new ConcurrentBag<ConfigurationSection>();
            Parallel.ForEach(components, component =>
            {
                Logger.Write(this, LogLevel.Debug, "Getting configuration for component {0}.", component.GetType().Name);
                try
                {
                    sections.AddRange(component.GetConfigurationSections());
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to get configuration for component {0}: {1}", component.GetType().Name, e.Message);
                }
            });
            foreach (var section in sections)
            {
                this.Components.Configuration.WithSection(section);
            }
            this.Components.Configuration.Load();
            this.Components.Configuration.ConnectDependencies();
#if DEBUG
            stopwatch.Stop();
            Logger.Write(this, LogLevel.Trace, "Configuration loaded in {0}ms.", stopwatch.ElapsedMilliseconds);
#endif
        }

        protected virtual void InitializeComponents()
        {
            ComponentRegistry.Instance.ForEach<IInitializable>(component =>
            {
                try
                {
                    component.InitializeComponent(this);
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to initialize component {0}: {1}", component.GetType().Name, e.Message);
                }
            });
        }

        public void InitializeDatabase(IDatabaseComponent database, DatabaseInitializeType type)
        {
            ComponentRegistry.Instance.ForEach<IDatabaseInitializer>(component =>
            {
                try
                {
                    component.InitializeDatabase(database, type);
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to initialize database {0}: {1}", component.GetType().Name, e.Message);
                }
            });
        }

        public void Unload()
        {
            IsShuttingDown = true;
            this.SaveConfiguration();
        }

        protected virtual void SaveConfiguration()
        {
            this.Components.Configuration.Save();
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
            ComponentRegistry.Instance.ForEach<IDisposable>(component =>
            {
                if (object.ReferenceEquals(this, component))
                {
                    return;
                }
                try
                {
                    component.Dispose();
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to dispose component {0}: {1}", component.GetType().Name, e.Message);
                }
            });
            Instance = null;
        }

        ~Core()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}
