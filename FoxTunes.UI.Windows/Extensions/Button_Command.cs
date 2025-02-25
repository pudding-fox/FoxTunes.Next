using FoxTunes.Interfaces;
using FoxTunes.ViewModel;
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FoxTunes
{
    public static class ButtonExtensions
    {
        public const string COMMAND_BEHAVIOUR_DISMISS = "ButtonExtensions.Dismiss";

        public const string COMMAND_BEHAVIOUR_ACCEPT = "ButtonExtensions.Accept";

        private static readonly IErrorEmitter ErrorEmitter = ComponentRegistry.Instance.GetComponent<IErrorEmitter>();

        private static readonly ConditionalWeakTable<Button, CommandBehaviour> CommandBehaviours = new ConditionalWeakTable<Button, CommandBehaviour>();

        public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(ButtonExtensions),
            new FrameworkPropertyMetadata(default(ICommand), new PropertyChangedCallback(OnCommandPropertyChanged))
        );

        public static ICommand GetCommand(Button source)
        {
            return (ICommand)source.GetValue(CommandProperty);
        }

        public static void SetCommand(Button source, ICommand value)
        {
            source.SetValue(CommandProperty, value);
        }

        private static void OnCommandPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }
            var command = GetCommand(button);
            if (command != null)
            {
                var behaviour = default(CommandBehaviour);
                if (CommandBehaviours.TryGetValue(button, out behaviour))
                {
                    CommandBehaviours.Remove(button);
                    behaviour.Dispose();
                }
                CommandBehaviours.Add(button, new CommandBehaviour(button, command));
            }
            else
            {
                var behaviour = default(CommandBehaviour);
                if (CommandBehaviours.TryGetValue(button, out behaviour))
                {
                    CommandBehaviours.Remove(button);
                    behaviour.Dispose();
                }
            }
        }

        public static readonly DependencyProperty CommandBehaviourProperty = DependencyProperty.RegisterAttached(
            "CommandBehaviour",
            typeof(string),
            typeof(ButtonExtensions),
            new PropertyMetadata(default(string))
        );

        public static string GetCommandBehaviour(Button source)
        {
            return (string)source.GetValue(CommandBehaviourProperty);
        }

        public static void SetCommandBehaviour(Button source, string value)
        {
            source.SetValue(CommandBehaviourProperty, value);
        }

        public static readonly RoutedEvent CommandExecutedEvent = EventManager.RegisterRoutedEvent(
           "CommandExecuted",
           RoutingStrategy.Bubble,
           typeof(CommandExecutedEventHandler),
           typeof(ListBoxExtensions)
       );

        public static void AddCommandExecutedHandler(DependencyObject source, CommandExecutedEventHandler handler)
        {
            var element = source as UIElement;
            if (element != null)
            {
                element.AddHandler(CommandExecutedEvent, handler);
            }
        }

        public static void RemoveCommandExecutedHandler(DependencyObject source, CommandExecutedEventHandler handler)
        {
            var element = source as UIElement;
            if (element != null)
            {
                element.RemoveHandler(CommandExecutedEvent, handler);
            }
        }

        public delegate void CommandExecutedEventHandler(object sender, CommandExecutedEventArgs e);

        public class CommandExecutedEventArgs : RoutedEventArgs
        {
            public CommandExecutedEventArgs(ICommand command, object parameter, string behaviour)
            {
                this.Command = command;
                this.Parameter = parameter;
                this.Behaviour = behaviour;
            }

            public CommandExecutedEventArgs(RoutedEvent routedEvent, ICommand command, object parameter, string behaviour)
                : base(routedEvent)
            {
                this.Command = command;
                this.Parameter = parameter;
                this.Behaviour = behaviour;
            }

            public CommandExecutedEventArgs(RoutedEvent routedEvent, object source, ICommand command, object parameter, string behaviour)
                : base(routedEvent, source)
            {
                this.Command = command;
                this.Parameter = parameter;
                this.Behaviour = behaviour;
            }

            public ICommand Command { get; private set; }

            public object Parameter { get; private set; }

            public string Behaviour { get; private set; }
        }

        private class CommandBehaviour : UIBehaviour
        {
            public CommandBehaviour(Button button, ICommand command)
            {
                this.Button = button;
                this.Button.Command = this.WrapCommand(command);
            }

            public Button Button { get; private set; }

            protected virtual ICommand WrapCommand(ICommand command)
            {
                return new Command(() =>
                {
                    try
                    {
                        if (command != null)
                        {
                            var parameter = this.Button.CommandParameter;
                            if (command.CanExecute(parameter))
                            {
                                command.Execute(parameter);
                                this.Button.RaiseEvent(new CommandExecutedEventArgs(
                                    CommandExecutedEvent,
                                    command,
                                    parameter,
                                    GetCommandBehaviour(this.Button)
                                ));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Write(typeof(CommandBehaviour), LogLevel.Warn, "Failed to execute command: {0}", e.Message);
                        var task = ErrorEmitter.Send(this, string.Format("Failed to execute command: {0}", e.Message), e);
                    }
                });
            }

            protected override void OnDisposing()
            {
                this.Button.Command = null;
                base.OnDisposing();
            }
        }
    }
}