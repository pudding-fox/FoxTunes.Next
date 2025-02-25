using System;
using System.Reflection;
using System.Windows;

namespace FoxTunes
{
    public static class InheritanceContextHelper
    {
        static InheritanceContextHelper()
        {
            Property = typeof(DependencyObject).GetProperty("InheritanceContext", BindingFlags.Instance | BindingFlags.NonPublic);
            if (Property == null)
            {
                throw new NotImplementedException();
            }
            Event = typeof(DependencyObject).GetEvent("InheritanceContextChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            if (Event == null)
            {
                throw new NotImplementedException();
            }
            AddMethod = Event.GetAddMethod(true);
            RemoveMethod = Event.GetRemoveMethod(true);
        }

        public static PropertyInfo Property { get; private set; }

        public static EventInfo Event { get; private set; }

        public static MethodInfo AddMethod { get; private set; }

        public static MethodInfo RemoveMethod { get; private set; }

        public static DependencyObject Get(DependencyObject source)
        {
            return (DependencyObject)Property.GetValue(source, null);
        }

        public static void AddEventHandler(DependencyObject source, EventHandler handler)
        {
            AddMethod.Invoke(source, new object[] { handler });
        }

        public static void RemoveEventHandler(DependencyObject source, EventHandler handler)
        {
            RemoveMethod.Invoke(source, new object[] { handler });
        }
    }
}
