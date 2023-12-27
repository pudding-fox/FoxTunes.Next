using System;

namespace FoxTunes
{
    public class ActivationProxy : MarshalByRefObject
    {
        public T CreateInstance<T>(string fileName, params object[] args)
        {
            var instance = (T)Activator.CreateInstance(typeof(T), args);
            return instance;
        }

        public static ActivationProxy CreateProxy(AppDomain domain)
        {
            var proxy = (ActivationProxy)domain.CreateInstanceAndUnwrap(
                typeof(ActivationProxy).Assembly.FullName,
                typeof(ActivationProxy).FullName
            );
            return proxy;
        }
    }
}
