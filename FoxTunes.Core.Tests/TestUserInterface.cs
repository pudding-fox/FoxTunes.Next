using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentPriority(ComponentPriorityAttribute.LOW)]
    [Component(ID, ComponentSlots.UserInterface)]
    public class TestUserInterface : StandardComponent, IUserInterface
    {
        public const string ID = "12A99332-F264-4D5E-824B-EA812B4DCB7A";

        public IEnumerable<IUserInterfaceWindow> Windows
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Activate()
        {
            throw new NotImplementedException();
        }

        public bool Confirm(string message)
        {
            throw new NotImplementedException();
        }

        public void Fatal(Exception exception)
        {
            throw new NotImplementedException();
        }

        public void OpenInShell(string fileName)
        {
            throw new NotImplementedException();
        }

        public string Prompt(string message, UserInterfacePromptFlags flags = UserInterfacePromptFlags.None)
        {
            throw new NotImplementedException();
        }

        public string Prompt(string message, string value, UserInterfacePromptFlags flags = UserInterfacePromptFlags.None)
        {
            throw new NotImplementedException();
        }

        public void Restart()
        {
            throw new NotImplementedException();
        }

        public void SelectInShell(string fileName)
        {
            throw new NotImplementedException();
        }

        public Task Show()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ShowSettings(string title, IEnumerable<string> sections)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ShowSettings(string title, IConfiguration configuration, IEnumerable<string> sections)
        {
            throw new NotImplementedException();
        }

        public void Warn(string message)
        {
            throw new NotImplementedException();
        }

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
