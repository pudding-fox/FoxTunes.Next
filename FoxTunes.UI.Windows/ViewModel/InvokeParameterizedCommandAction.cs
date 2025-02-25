using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace FoxTunes.ViewModel
{
    public class InvokeParameterizedCommandAction : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command",
            typeof(ICommand),
            typeof(InvokeParameterizedCommandAction)
        );

        public static ICommand GetCommand(DependencyObject source)
        {
            return (ICommand)source.GetValue(CommandProperty);
        }

        public static void SetCommand(DependencyObject source, ICommand value)
        {
            source.SetValue(CommandProperty, value);
        }

        protected override void Invoke(object parameter)
        {
            var command = GetCommand(this);
            command.Execute(parameter);
        }
    }
}
