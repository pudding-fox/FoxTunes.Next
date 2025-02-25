using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public static partial class PasswordBoxExtensions
    {
        private static readonly ConditionalWeakTable<PasswordBox, TextBehaviour> TextBehaviours = new ConditionalWeakTable<PasswordBox, TextBehaviour>();

        public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
            "Text",
            typeof(string),
            typeof(PasswordBoxExtensions),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnTextPropertyChanged))
        );

        public static string GetText(PasswordBox source)
        {
            return (string)source.GetValue(TextProperty);
        }

        public static void SetText(PasswordBox source, string value)
        {
            source.SetValue(TextProperty, value);
        }

        private static void OnTextPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox == null)
            {
                return;
            }
            var behaviour = default(TextBehaviour);
            if (!TextBehaviours.TryGetValue(passwordBox, out behaviour))
            {
                TextBehaviours.Add(passwordBox, new TextBehaviour(passwordBox));
            }
        }

        private class TextBehaviour : UIBehaviour<PasswordBox>
        {
            public TextBehaviour(PasswordBox passwordBox) : base(passwordBox)
            {
                this.PasswordBox = passwordBox;
                this.PasswordBox.PasswordChanged += this.OnPasswordChanged;
            }

            public PasswordBox PasswordBox { get; private set; }

            protected override void OnDisposing()
            {
                if (this.PasswordBox != null)
                {
                    this.PasswordBox.PasswordChanged -= this.OnPasswordChanged;
                }
                base.OnDisposing();
            }

            protected virtual void OnPasswordChanged(object sender, RoutedEventArgs e)
            {
                SetText(this.PasswordBox, this.PasswordBox.Password);
            }
        }
    }
}
