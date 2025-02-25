using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes.ViewModel
{
    public class CommandFactory
    {
        public Command CreateCommand(Action action)
        {
            return new Command(action);
        }

        public Command CreateCommand(Action action, Func<bool> predicate)
        {
            return new Command(action, predicate);
        }

        public Command<T> CreateCommand<T>(Action<T> action)
        {
            return new Command<T>(action);
        }

        public Command<T> CreateCommand<T>(Action<T> action, Func<T, bool> predicate)
        {
            return new Command<T>(action, predicate);
        }

        public AsyncCommand CreateCommand(Func<Task> func)
        {
            return new AsyncCommand(func);
        }

        public AsyncCommand CreateCommand(Func<Task> func, Func<bool> predicate)
        {
            return new AsyncCommand(func, predicate);
        }

        public AsyncCommand<T> CreateCommand<T>(Func<T, Task> func)
        {
            return new AsyncCommand<T>(func);
        }

        public AsyncCommand<T> CreateCommand<T>(Func<T, Task> func, Func<T, bool> predicate)
        {
            return new AsyncCommand<T>(func, predicate);
        }

        public static readonly CommandFactory Instance = new CommandFactory();
    }
}
