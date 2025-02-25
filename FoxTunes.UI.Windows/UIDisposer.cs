using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace FoxTunes
{
    public static class UIDisposer
    {
        public static void Dispose(FrameworkElement element, UIDisposerFlags flags = UIDisposerFlags.Default)
        {
            var history = new HashSet<FrameworkElement>();
            history.Add(element);
            var stack = new Stack<FrameworkElement>();
            stack.Push(element);
            while (stack.Count > 0)
            {
                element = stack.Pop();
                if (element is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch
                    {
                        //Nothing can be done.
                    }
                }
                if (element.ContextMenu is IDisposable contextMenu)
                {
                    contextMenu.Dispose();
                }
                foreach (var behaviour in element.GetActive())
                {
                    behaviour.Dispose();
                }
                Dispose(element.Resources);
                if (flags.HasFlag(UIDisposerFlags.VisualTree))
                {
                    for (int a = 0, b = VisualTreeHelper.GetChildrenCount(element); a < b; a++)
                    {
                        var child = VisualTreeHelper.GetChild(element, a) as FrameworkElement;
                        if (child != null)
                        {
                            if (!history.Add(child))
                            {
                                continue;
                            }
                            stack.Push(child);
                        }
                    }
                }
                if (flags.HasFlag(UIDisposerFlags.LogicalTree))
                {
                    foreach (var child in LogicalTreeHelper.GetChildren(element).OfType<FrameworkElement>())
                    {
                        if (!history.Add(child))
                        {
                            continue;
                        }
                        stack.Push(child);
                    }
                }
            }
        }

        private static void Dispose(ResourceDictionary resources)
        {
            foreach (var disposable in resources.Values.OfType<IDisposable>())
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    //Nothing can be done.
                }
            }
        }
    }

    [Flags]
    public enum UIDisposerFlags : byte
    {
        None,
        VisualTree,
        LogicalTree,
        Default = VisualTree,
        All = VisualTree | LogicalTree
    }
}
