using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class UserInterface : StandardComponent, IUserInterface
    {
        public abstract IEnumerable<IUserInterfaceWindow> Windows { get; }

        public abstract Task Show();

        public abstract void Activate();

        public abstract void Warn(string message);

        public abstract void Fatal(Exception exception);

        public abstract bool Confirm(string message);

        public abstract string Prompt(string message, UserInterfacePromptFlags flags = UserInterfacePromptFlags.None);

        public abstract string Prompt(string message, string value, UserInterfacePromptFlags flags = UserInterfacePromptFlags.None);

        public abstract void SelectInShell(string fileName);

        public abstract void OpenInShell(string fileName);

        public abstract Task<bool> ShowSettings(string title, IEnumerable<string> sections);

        public abstract Task<bool> ShowSettings(string title, IConfiguration configuration, IEnumerable<string> sections);

        public abstract void Restart();

        protected virtual void OnWindowCreated(IUserInterfaceWindow window)
        {
            if (this.WindowCreated == null)
            {
                return;
            }
            this.WindowCreated(this, new UserInterfaceWindowEventArgs(window));
        }

        public event UserInterfaceWindowEventHandler WindowCreated;

        protected virtual void OnWindowDestroyed(IUserInterfaceWindow window)
        {
            if (this.WindowDestroyed == null)
            {
                return;
            }
            this.WindowDestroyed(this, new UserInterfaceWindowEventArgs(window));
        }

        public event UserInterfaceWindowEventHandler WindowDestroyed;

        protected virtual void OnShuttingDown()
        {
            if (this.ShuttingDown == null)
            {
                return;
            }
            this.ShuttingDown(this, EventArgs.Empty);
        }

        public event EventHandler ShuttingDown;
    }
}
