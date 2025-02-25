using FoxDb;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace FoxTunes.Proxies
{
    public static class JavascriptContextFactory
    {
        public static readonly Lazy<Assembly> Assembly = new Lazy<Assembly>(() =>
        {
            var fileName = Path.Combine(
                Path.GetDirectoryName(typeof(JavascriptContextFactory).Assembly.Location),
                Environment.Is64BitProcess ? "x64" : "x86",
                "Noesis.Javascript.dll"
            );
            return global::System.Reflection.Assembly.LoadFrom(fileName);
        });

        public static readonly Lazy<Type> Type = new Lazy<Type>(() =>
        {
            return Assembly.Value.GetType("Noesis.Javascript.JavascriptContext");
        });

        public static readonly Lazy<JavascriptContextHandlers> Handlers = new Lazy<JavascriptContextHandlers>(() =>
        {
            return new JavascriptContextHandlers(GetParameter(), SetParameter(), Run());
        });

        private static Func<object, string, object> GetParameter()
        {
            var type = Type.Value;
            var context = Expression.Parameter(typeof(object));
            var name = Expression.Parameter(typeof(string));
            var method = type.GetMethod("GetParameter", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            return Expression.Lambda<Func<object, string, object>>(
                Expression.Call(Expression.Convert(context, type), method, name),
                context,
                name
            ).Compile();
        }

        private static Action<object, string, object> SetParameter()
        {
            var type = Type.Value;
            var context = Expression.Parameter(typeof(object));
            var name = Expression.Parameter(typeof(string));
            var value = Expression.Parameter(typeof(object));
            var method = type.GetMethod("SetParameter", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(object) }, null);
            return Expression.Lambda<Action<object, string, object>>(
                Expression.Call(Expression.Convert(context, type), method, name, value),
                context,
                name,
                value
            ).Compile();
        }

        private static Func<object, string, object> Run()
        {
            var type = Type.Value;
            var context = Expression.Parameter(typeof(object));
            var script = Expression.Parameter(typeof(string));
            var method = type.GetMethod("Run", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            return Expression.Lambda<Func<object, string, object>>(
                Expression.Call(Expression.Convert(context, type), method, script),
                context,
                script
            ).Compile();
        }

        public static JavascriptContext Create()
        {
            var context = FastActivator.Instance.Activate(Type.Value);
            return new JavascriptContext(context, Handlers.Value);
        }
    }
}
