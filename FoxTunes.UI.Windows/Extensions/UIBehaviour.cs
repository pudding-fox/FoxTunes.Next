using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public abstract class UIBehaviour : BaseComponent, IDisposable
    {
        static UIBehaviour()
        {
            Instances = new List<WeakReference<UIBehaviour>>();
        }

        private static IList<WeakReference<UIBehaviour>> Instances { get; set; }

        public static IEnumerable<UIBehaviour> Active
        {
            get
            {
                lock (Instances)
                {
                    return Instances
                        .Where(instance => instance != null && instance.IsAlive)
                        .Select(instance => instance.Target)
                        .ToArray();
                }
            }
        }


        protected UIBehaviour()
        {
            lock (Instances)
            {
                Instances.Add(new WeakReference<UIBehaviour>(this));
            }
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
            lock (Instances)
            {
                for (var a = Instances.Count - 1; a >= 0; a--)
                {
                    var instance = Instances[a];
                    if (instance == null || !instance.IsAlive)
                    {
                        Instances.RemoveAt(a);
                    }
                    else if (object.ReferenceEquals(this, instance.Target))
                    {
                        Instances.RemoveAt(a);
                    }
                }
            }
        }

        ~UIBehaviour()
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

        public static void Shutdown()
        {
            Logger.Write(typeof(UIBehaviour), LogLevel.Debug, "Shutting down..");
            foreach (var instance in Active)
            {
                try
                {
                    instance.Dispose();
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(UIBehaviour), LogLevel.Warn, "Instance failed to dispose: {0}", e.Message);
                }
            }
            if (Active.Any())
            {
                Logger.Write(typeof(UIBehaviour), LogLevel.Warn, "Some instances failed to disposed.");
            }
        }
    }

    public abstract class UIBehaviour<T> : UIBehaviour, IUIBehaviour<T>
    {
        protected UIBehaviour(T subject)
        {
            this.Subject = subject;
        }

        public T Subject { get; private set; }
    }

    public interface IUIBehaviour<out T> : IDisposable
    {
        T Subject { get; }
    }

    public static partial class Extensions
    {
        public static IEnumerable<IUIBehaviour<T>> GetActive<T>(this T subject)
        {
            return UIBehaviour.Active.OfType<IUIBehaviour<T>>().Where(behaviour => object.ReferenceEquals(behaviour.Subject, subject));
        }
    }
}
