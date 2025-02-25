using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace FoxTunes
{
    public static class BindingHelper
    {
        public static void Create(DependencyObject target, DependencyProperty property, Type targetType, object source, string path, EventHandler handler)
        {
            Create(target, property, targetType, source, path, null, handler);
        }

        public static void Create(DependencyObject target, DependencyProperty property, Type targetType, object source, string path, IValueConverter converter, EventHandler handler)
        {
            BindingOperations.SetBinding(target, property, new Binding(path)
            {
                Source = source,
                Converter = converter,
                Mode = BindingMode.TwoWay
            });
            AddHandler(target, property, targetType, handler);
        }

        public static void AddHandler(DependencyObject target, DependencyProperty property, Type targetType, EventHandler handler)
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(property, targetType);
            descriptor.AddValueChanged(target, handler);
        }

        public static void RemoveHandler(DependencyObject target, DependencyProperty property, Type targetType, EventHandler handler)
        {
            var descriptor = DependencyPropertyDescriptor.FromProperty(property, targetType);
            descriptor.RemoveValueChanged(target, handler);
        }
    }
}
