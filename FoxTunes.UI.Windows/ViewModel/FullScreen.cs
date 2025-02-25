using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class FullScreen : ViewModelBase
    {
        public ConditionalWeakTable<Window, PreviousWindowState> PreviousStates = new ConditionalWeakTable<Window, PreviousWindowState>();

        public ICommand ToggleCommand
        {
            get
            {
                return new Command(this.Toggle);
            }
        }

        public void Toggle()
        {
            var window = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
            if (window == null)
            {
                return;
            }
            this.Toggle(window);
        }

        protected virtual void Toggle(Window window)
        {
            var previousState = default(PreviousWindowState);
            if (this.PreviousStates.TryGetValue(window, out previousState))
            {
                this.Restore(window, previousState);
            }
            else
            {
                this.Maximize(window);
            }
        }

        protected virtual void Maximize(Window window)
        {
            this.PreviousStates.Add(window, new PreviousWindowState()
            {
                WindowStyle = window.WindowStyle,
                WindowState = window.WindowState
            });
            window.WindowStyle = global::System.Windows.WindowStyle.None;
            window.WindowState = global::System.Windows.WindowState.Maximized;
            if (window is WindowBase windowBase)
            {
                windowBase.HideTemplate();
                windowBase.HideWindowChrome();
            }
            window.StateChanged += this.OnStateChanged;
        }

        protected virtual void Restore(Window window, PreviousWindowState previousState)
        {
            window.StateChanged -= this.OnStateChanged;
            this.PreviousStates.Remove(window);
            window.WindowStyle = previousState.WindowStyle;
            window.WindowState = previousState.WindowState;
            if (window is WindowBase windowBase)
            {
                windowBase.ShowTemplate();
                windowBase.ShowWindowChrome();
            }
        }


        protected virtual void OnStateChanged(object sender, EventArgs e)
        {
            if (sender is Window window)
            {
                this.Toggle(window);
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new FullScreen();
        }

        public class PreviousWindowState
        {
            public global::System.Windows.WindowStyle WindowStyle { get; set; }

            public global::System.Windows.WindowState WindowState { get; set; }
        }
    }
}
