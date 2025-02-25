using FoxTunes.Interfaces;
using System;

namespace FoxTunes.ViewModel
{
    public class Command : CommandBase
    {
        public Command(Action action)
            : this(action, null)
        {
        }

        public Command(Action action, Func<bool> predicate)
        {
            this.Action = action;
            this.Predicate = predicate;

        }

        public Action Action { get; private set; }

        public Func<bool> Predicate { get; private set; }

        public override bool CanExecute(object parameter)
        {
            if (this.Predicate == null)
            {
                return true;
            }
            return this.Predicate();
        }

        public override void Execute(object parameter)
        {
            if (!this.CanExecute(parameter))
            {
                throw new InvalidOperationException("Execution is not valid at this time.");
            }
            if (this.Action == null)
            {
                return;
            }
            try
            {
                this.Action();
            }
            catch (Exception e)
            {
                Logger.Write(typeof(Command), LogLevel.Warn, "Failed to execute command: {0}", e.Message);
                var task = ErrorEmitter.Send(this, string.Format("Failed to execute command: {0}", e.Message), e);
            }
            this.OnCanExecuteChanged();
        }
    }

    public class Command<T> : CommandBase
    {
        public Command(Action<T> action)
            : this(action, null)
        {
        }

        public Command(Action<T> action, Func<T, bool> predicate)
        {
            this.Action = action;
            this.Predicate = predicate;

        }

        public Action<T> Action { get; private set; }

        public Func<T, bool> Predicate { get; private set; }

        public override bool CanExecute(object parameter)
        {
            if (this.Predicate == null)
            {
                return true;
            }
            if (parameter is T)
            {
                return this.Predicate((T)parameter);
            }
            else
            {
                return this.Predicate(default(T));
            }
        }

        public override void Execute(object parameter)
        {
            if (!this.CanExecute(parameter))
            {
                throw new InvalidOperationException("Execution is not valid at this time.");
            }
            if (this.Action == null)
            {
                return;
            }
            try
            {
                if (parameter is T)
                {
                    this.Action((T)parameter);
                }
                else
                {
                    this.Action(default(T));
                }
            }
            catch (Exception e)
            {
                Logger.Write(typeof(Command), LogLevel.Warn, "Failed to execute command: {0}", e.Message);
                var task = ErrorEmitter.Send(this, string.Format("Failed to execute command: {0}", e.Message), e);
            }
            this.OnCanExecuteChanged();
        }
    }
}
