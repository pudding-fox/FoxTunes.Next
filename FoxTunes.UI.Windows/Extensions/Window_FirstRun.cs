using System.Runtime.CompilerServices;
using System.Windows;

namespace FoxTunes
{
    public static partial class WindowExtensions
    {
        private static readonly ConditionalWeakTable<Window, FirstRunBehaviour> NumericBehaviours = new ConditionalWeakTable<Window, FirstRunBehaviour>();

        public static readonly DependencyProperty FirstRunProperty = DependencyProperty.RegisterAttached(
            "FirstRun",
            typeof(bool),
            typeof(WindowExtensions),
            new PropertyMetadata(false, new PropertyChangedCallback(OnFirstRunPropertyChanged))
        );

        public static bool GetFirstRun(Window source)
        {
            return (bool)source.GetValue(FirstRunProperty);
        }

        public static void SetFirstRun(Window source, bool value)
        {
            source.SetValue(FirstRunProperty, value);
        }

        private static void OnFirstRunPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var window = sender as Window;
            if (window == null)
            {
                return;
            }
            var behaviour = default(FirstRunBehaviour);
            if (!NumericBehaviours.TryGetValue(window, out behaviour))
            {
                NumericBehaviours.Add(window, new FirstRunBehaviour(window));
            }
        }

        private class FirstRunBehaviour : UIBehaviour<Window>
        {
            public FirstRunBehaviour(Window window) : base(window)
            {
                this.Window = window;
                this.FirstRun = Core.Instance.Components.Configuration.GetElement<BooleanConfigurationElement>(
                    WindowsUserInterfaceConfiguration.SECTION,
                    WindowsUserInterfaceConfiguration.FIRST_RUN_ELEMENT
                );
                if (this.FirstRun.Value)
                {
                    this.Show();
                    this.FirstRun.Value = false;
                }
            }

            public Window Window { get; private set; }

            public BooleanConfigurationElement FirstRun { get; private set; }

            public void Show()
            {
                this.Dispatch(() => Windows.ShowDialog<FirstRunDialog>(Core.Instance, this.FirstRun.Name));
            }
        }
    }
}
