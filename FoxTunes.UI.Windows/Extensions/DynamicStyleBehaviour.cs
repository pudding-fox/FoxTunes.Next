using System;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Xml;

namespace FoxTunes
{
    public abstract class DynamicStyleBehaviour<T> : UIBehaviour<T>
    {
        private static readonly ThemeLoader ThemeLoader = ComponentRegistry.Instance.GetComponent<ThemeLoader>();

        protected DynamicStyleBehaviour(T subject) : base(subject)
        {
            if (ThemeLoader != null)
            {
                ThemeLoader.ThemeChanged += this.OnThemeChanged;
            }
        }

        protected virtual Style CreateStyle(Style style, Style basedOn)
        {
            if (basedOn != null)
            {
                if (style.IsSealed)
                {
                    style = this.Clone(style);
                }
                style.BasedOn = basedOn;
            }
            return style;
        }

        protected abstract void Apply();

        protected virtual Style Clone(Style style)
        {
            var builder = new StringBuilder();
            var settings = new XmlWriterSettings()
            {
                Indent = true,
                ConformanceLevel = ConformanceLevel.Fragment,
                OmitXmlDeclaration = true,
                NamespaceHandling = NamespaceHandling.OmitDuplicates,
            };
            using (var writer = XmlWriter.Create(builder, settings))
            {
                var manager = new XamlDesignerSerializationManager(writer)
                {
                    XamlWriterMode = XamlWriterMode.Expression
                };
                XamlWriter.Save(style, manager);
            }
            return (Style)XamlReader.Parse(builder.ToString());
        }

        protected virtual void OnThemeChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(() =>
            {
                //Ensure resources are loaded.
                ThemeLoader.EnsureTheme();
                this.Apply();
            });
        }

        protected override void OnDisposing()
        {
            if (ThemeLoader != null)
            {
                ThemeLoader.ThemeChanged -= this.OnThemeChanged;
            }
            base.OnDisposing();
        }
    }
}
