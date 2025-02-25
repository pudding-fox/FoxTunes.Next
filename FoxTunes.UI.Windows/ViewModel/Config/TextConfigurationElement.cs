using FoxTunes.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel.Config
{
    public class TextConfigurationElement : ViewModelBase
    {
        public static readonly DependencyProperty ElementProperty = DependencyProperty.Register(
           "Element",
           typeof(global::FoxTunes.TextConfigurationElement),
           typeof(TextConfigurationElement),
           new PropertyMetadata(new PropertyChangedCallback(OnElementChanged))
       );

        public static global::FoxTunes.TextConfigurationElement GetElement(ViewModelBase source)
        {
            return (global::FoxTunes.TextConfigurationElement)source.GetValue(ElementProperty);
        }

        public static void SetElement(ViewModelBase source, global::FoxTunes.TextConfigurationElement value)
        {
            source.SetValue(ElementProperty, value);
        }

        public static void OnElementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = sender as TextConfigurationElement;
            if (viewModel == null)
            {
                return;
            }
            viewModel.OnElementChanged();
        }

        public global::FoxTunes.TextConfigurationElement Element
        {
            get
            {
                return this.GetValue(ElementProperty) as global::FoxTunes.TextConfigurationElement;
            }
            set
            {
                this.SetValue(ElementProperty, value);
            }
        }

        protected virtual void OnElementChanged()
        {
            this.OnIsMultilineChanged();
            this.OnCanBrowseChanged();
            this.OnCanHelpChanged();
            if (this.ElementChanged != null)
            {
                this.ElementChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Element");
        }

        public event EventHandler ElementChanged;

        public bool IsMultiline
        {
            get
            {
                return this.Element != null && this.Element.Flags.HasFlag(ConfigurationElementFlags.MultiLine);
            }
        }

        protected virtual void OnIsMultilineChanged()
        {
            if (this.IsMultilineChanged != null)
            {
                this.IsMultilineChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsMultiline");
        }

        public event EventHandler IsMultilineChanged;

        public bool IsSecret
        {
            get
            {
                return this.Element != null && this.Element.Flags.HasFlag(ConfigurationElementFlags.Secret);
            }
        }

        protected virtual void OnIsSecretChanged()
        {
            if (this.IsSecretChanged != null)
            {
                this.IsSecretChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsSecret");
        }

        public event EventHandler IsSecretChanged;

        public ICommand BrowseCommand
        {
            get
            {
                return new Command(this.Browse, () => this.CanBrowse);
            }
        }

        public bool CanBrowse
        {
            get
            {
                if (this.Element == null)
                {
                    return false;
                }
                if (this.Element.Flags.HasFlag(ConfigurationElementFlags.FileName))
                {
                    return true;
                }
                if (this.Element.Flags.HasFlag(ConfigurationElementFlags.FolderName))
                {
                    return true;
                }
                if (this.Element.Flags.HasFlag(ConfigurationElementFlags.FontFamily))
                {
                    return true;
                }
                return false;
            }
        }

        protected virtual void OnCanBrowseChanged()
        {
            if (this.CanBrowseChanged != null)
            {
                this.CanBrowseChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("CanBrowse");
        }

        public event EventHandler CanBrowseChanged;

        public void Browse()
        {
            if (this.Element.Flags.HasFlag(ConfigurationElementFlags.FileName))
            {
                this.BrowseFile();
            }
            if (this.Element.Flags.HasFlag(ConfigurationElementFlags.FolderName))
            {
                this.BrowseFolder();
            }
            if (this.Element.Flags.HasFlag(ConfigurationElementFlags.FontFamily))
            {
                this.BrowseFont();
            }
        }

        protected virtual void BrowseFile()
        {
            var browser = ComponentRegistry.Instance.GetComponent<IFileSystemBrowser>();
            if (browser == null)
            {
                return;
            }
            var path = default(string);
            if (!string.IsNullOrEmpty(this.Element.Value) && FileSystemHelper.IsLocalPath(this.Element.Value))
            {
                path = this.Element.Value;
            }
            var options = new BrowseOptions(this.Element.Name, path, Enumerable.Empty<BrowseFilter>(), BrowseFlags.File);
            var result = browser.Browse(options);
            if (!result.Success)
            {
                return;
            }
            this.Element.Value = result.Paths.FirstOrDefault();
        }

        protected virtual void BrowseFolder()
        {
            var browser = ComponentRegistry.Instance.GetComponent<IFileSystemBrowser>();
            if (browser == null)
            {
                return;
            }
            var path = default(string);
            if (!string.IsNullOrEmpty(this.Element.Value) && FileSystemHelper.IsLocalPath(this.Element.Value))
            {
                path = this.Element.Value;
            }
            var options = new BrowseOptions(this.Element.Name, path, Enumerable.Empty<BrowseFilter>(), BrowseFlags.Folder);
            var result = browser.Browse(options);
            if (!result.Success)
            {
                return;
            }
            this.Element.Value = result.Paths.FirstOrDefault();
        }

        protected virtual void BrowseFont()
        {
            var fontFamily = default(string);
            if (!string.IsNullOrEmpty(this.Element.Value))
            {
                fontFamily = this.Element.Value;
            }
            else
            {
                fontFamily = SystemFonts.MessageFontFamily.FamilyNames.Select(familyName => familyName.Value).FirstOrDefault();
            }
            //TODO: Use only WPF frameworks.
            var fontDialog = new global::System.Windows.Forms.FontDialog()
            {
                ShowEffects = false
            };
            fontDialog.Font = new global::System.Drawing.Font(fontFamily, 12);
            if (fontDialog.ShowDialog() != global::System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            this.Element.Value = fontDialog.Font.Name;
        }

        public ICommand HelpCommand
        {
            get
            {
                return new Command(this.Help, () => this.CanHelp);
            }
        }

        public bool CanHelp
        {
            get
            {
                if (this.Element == null)
                {
                    return false;
                }
                if (this.Element.Flags.HasFlag(ConfigurationElementFlags.Script))
                {
                    return true;
                }
                return false;
            }
        }

        protected virtual void OnCanHelpChanged()
        {
            if (this.CanHelpChanged != null)
            {
                this.CanHelpChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("CanHelp");
        }

        public event EventHandler CanHelpChanged;

        public void Help()
        {
            var fileName = Path.Combine(
                Path.GetTempPath(),
                "Scripting.txt"
            );
            File.WriteAllText(fileName, Resources.Scripting);
            Process.Start(fileName);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new TextConfigurationElement();
        }
    }
}
