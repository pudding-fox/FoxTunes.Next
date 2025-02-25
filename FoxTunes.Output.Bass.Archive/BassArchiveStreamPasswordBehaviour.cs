using FoxTunes.Interfaces;
using ManagedBass.ZipStream;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    //TODO: bass_zipstream.dll does not work on XP.
    //TODO: Unable to avoid linking to CxxFrameHandler3 which does not exist until a later version of msvcrt.dll
    [PlatformDependency(Major = 6, Minor = 0)]
    public class BassArchiveStreamPasswordBehaviour : StandardBehaviour, IDisposable
    {
        public static readonly object SyncRoot = new object();

        public Archive.GetPasswordHandler Handler;

        public BassArchiveStreamPasswordBehaviour()
        {
            this.Passwords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.Handler = new Archive.GetPasswordHandler(this.GetPassword);
        }

        public Dictionary<string, string> Passwords { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.UserInterface = core.Components.UserInterface;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassArchiveStreamProviderBehaviourConfiguration.SECTION,
                BassArchiveStreamProviderBehaviourConfiguration.ENABLED_ELEMENT
            ).ConnectValue(value =>
            {
                if (value)
                {
                    this.Enable();
                    Logger.Write(this, LogLevel.Debug, "Archive password handler enabled.");
                }
                else
                {
                    this.Disable();
                    Logger.Write(this, LogLevel.Debug, "Archive password handler disabled.");
                }
            });
            base.InitializeComponent(core);
        }

        public bool Enabled { get; private set; }

        public void Enable()
        {
            if (this.Enabled)
            {
                return;
            }
            Archive.GetPassword(this.Handler);
            this.Enabled = true;
        }

        public void Disable()
        {
            if (!this.Enabled)
            {
                return;
            }
            Archive.GetPassword(null);
            this.Enabled = false;
        }

        protected virtual bool GetPassword(ref Archive.ArchivePassword password)
        {
            password.password = string.Empty;
            lock (SyncRoot)
            {
                if (!this.Passwords.TryGetValue(password.path, out password.password))
                {
                    password.password = this.UserInterface.Prompt(
                        string.Format("Please enter the password for \"{0}\":", password.path.GetName()),
                        UserInterfacePromptFlags.Password
                    );
                    this.Passwords[password.path] = password.password;
                }
            }
            return !string.IsNullOrEmpty(password.password);
        }

        public bool WasCancelled(string fileName)
        {
            lock (SyncRoot)
            {
                var password = default(string);
                if (this.Passwords.TryGetValue(fileName, out password))
                {
                    if (string.IsNullOrEmpty(password))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void Reset()
        {
            this.Passwords.Clear();
        }

        public bool Reset(string fileName)
        {
            lock (SyncRoot)
            {
                return this.Passwords.Remove(fileName);
            }
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
            this.Disable();
        }

        ~BassArchiveStreamPasswordBehaviour()
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
