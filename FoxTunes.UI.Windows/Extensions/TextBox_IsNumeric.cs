using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FoxTunes
{
    public static partial class TextBoxExtensions
    {
        private static readonly ConditionalWeakTable<TextBox, NumericBehaviour> NumericBehaviours = new ConditionalWeakTable<TextBox, NumericBehaviour>();

        public static readonly DependencyProperty IsNumericProperty = DependencyProperty.RegisterAttached(
            "IsNumeric",
            typeof(bool),
            typeof(TextBoxExtensions),
            new PropertyMetadata(false, new PropertyChangedCallback(OnIsNumericPropertyChanged))
        );

        public static bool GetIsNumeric(TextBox source)
        {
            return (bool)source.GetValue(IsNumericProperty);
        }

        public static void SetIsNumeric(TextBox source, bool value)
        {
            source.SetValue(IsNumericProperty, value);
        }

        private static void OnIsNumericPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }
            var behaviour = default(NumericBehaviour);
            if (!NumericBehaviours.TryGetValue(textBox, out behaviour))
            {
                NumericBehaviours.Add(textBox, new NumericBehaviour(textBox));
            }
        }

        private class NumericBehaviour : UIBehaviour<TextBox>
        {
            private static readonly Regex Pattern = new Regex(@"^\d+$", RegexOptions.Compiled); //TODO: Positive (or zero) integer only.

            public NumericBehaviour(TextBox textBox) : base(textBox)
            {
                this.TextBox = textBox;
                this.TextBox.PreviewTextInput += this.OnPreviewTextInput;
            }

            public TextBox TextBox { get; private set; }

            protected override void OnDisposing()
            {
                if (this.TextBox != null)
                {
                    this.TextBox.PreviewTextInput -= this.OnPreviewTextInput;
                }
                base.OnDisposing();
            }

            protected virtual void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
            {
                //If input is not numeric then "handle" the event, this prevents the input from being registered.
                e.Handled = !Pattern.IsMatch(e.Text);
            }
        }
    }
}
