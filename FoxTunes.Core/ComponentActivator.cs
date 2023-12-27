using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class ComponentActivator : IComponentActivator
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        private ComponentActivator()
        {

        }

        public T Activate<T>(Type type) where T : IBaseComponent
        {
            try
            {
                if (type.Assembly.ReflectionOnly)
                {
                    type = AssemblyRegistry.Instance.GetExecutableType(type);
                }
                if (type.GetConstructor(new Type[] { }) == null)
                {
                    Logger.Write(typeof(ComponentActivator), LogLevel.Warn, "Failed to locate constructor for component {0}.", type.Name);
                    return default(T);
                }
                if (FastActivator.Instance.Activate(type) is T component)
                {
                    return component;
                }
                else
                {
                    Logger.Write(typeof(ComponentActivator), LogLevel.Warn, "Component {0} is not of the expected type {1}.", type.Name, typeof(T).Name);
                    return default(T);
                }
            }
            catch (Exception e)
            {
                Logger.Write(typeof(ComponentActivator), LogLevel.Warn, "Failed to activate component {0}: {1}.", type.Name, e.Message);
                return default(T);
            }
        }

        public IEnumerable<IBaseComponent> Activate(IEnumerable<Type> components)
        {
            return components.Select(this.Activate<IBaseComponent>);
        }

        public static readonly IComponentActivator Instance = new ComponentActivator();
    }

    public class ComponentActivatorException : Exception
    {
        public ComponentActivatorException(string message)
            : base(message)
        {

        }
    }
}
