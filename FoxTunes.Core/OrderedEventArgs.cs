using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class OrderedEventArgs : EventArgs, IDisposable
    {
        public const byte PRIORITY_HIGH = 0;

        public const byte PRIORITY_NORMAL = 100;

        public const byte PRIORITY_LOW = 255;

        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        protected OrderedEventArgs()
        {
            this.Actions = new List<KeyValuePair<Action, byte>>();
        }

        public IList<KeyValuePair<Action, byte>> Actions { get; private set; }

        public void Add(Action action, byte priority)
        {
            this.Actions.Add(new KeyValuePair<Action, byte>(action, priority));
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            var pairs = this.Actions.OrderBy(
                pair => pair.Value
            );
            foreach (var pair in pairs)
            {
                pair.Key();
            }
        }

        ~OrderedEventArgs()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }

        public static OrderedEventArgs Begin()
        {
            return new OrderedEventArgs();
        }
    }

    public delegate void OrderedEventHandler(object sender, OrderedEventArgs e);
}
