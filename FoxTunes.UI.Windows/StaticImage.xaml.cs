using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for StaticImage.xaml
    /// </summary>
    [UIComponent("8A4E9DDB-2390-455D-9BA9-57C9A441CD75", role: UIComponentRole.System)]
    public partial class StaticImage : ConfigurableUIComponentBase, IInvocableComponent
    {
        public const string CATEGORY = "B11C3491-8765-4B6F-8AB8-D20E90A38400";

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source",
            typeof(Brush),
            typeof(StaticImage),
            new PropertyMetadata(new PropertyChangedCallback(OnSourceChanged))
        );

        public static Brush GetSource(StaticImage source)
        {
            return (Brush)source.GetValue(SourceProperty);
        }

        public static void SetSource(StaticImage source, Brush value)
        {
            source.SetValue(SourceProperty, value);
        }

        public static void OnSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var asyncImage = sender as StaticImage;
            if (asyncImage == null)
            {
                return;
            }
            asyncImage.OnSourceChanged();
        }

        public StaticImage()
        {
            this.InitializeComponent();
        }

        public Brush Source
        {
            get
            {
                return GetSource(this);
            }
            set
            {
                SetSource(this, value);
            }
        }

        protected virtual void OnSourceChanged()
        {
            if (this.SourceChanged != null)
            {
                this.SourceChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler SourceChanged;

        public DispatcherTimer Timer { get; private set; }

        public string Path { get; private set; }

        public string FileName { get; private set; }

        protected virtual void OnTick(object sender, EventArgs e)
        {
            try
            {
                this.Refresh();
            }
            catch
            {
                //Never throw on Dispatcher thread.
            }
        }

        public override IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return CATEGORY;
            }
        }

        protected override void InitializeComponent(ICore core)
        {
            this.Timer = new DispatcherTimer(DispatcherPriority.Background);
            this.Timer.Tick += this.OnTick;
            base.InitializeComponent(core);
        }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Configuration.GetElement<TextConfigurationElement>(
                    StaticImageConfiguration.SECTION,
                    StaticImageConfiguration.PATH
                ).ConnectValue(value =>
                {
                    this.Path = value;
                    this.Refresh();
                });
                this.Configuration.GetElement<IntegerConfigurationElement>(
                    StaticImageConfiguration.SECTION,
                    StaticImageConfiguration.INTERVAL
                ).ConnectValue(value =>
                {
                    this.Timer.Interval = TimeSpan.FromSeconds(value);
                    this.Timer.Start();
                });
                this.Configuration.GetElement<IntegerConfigurationElement>(
                    StaticImageConfiguration.SECTION,
                    StaticImageConfiguration.OPACITY
                ).ConnectValue(value =>
                {
                    var opacity = (float)value / 100;
                    this.Opacity = opacity;
                });
            }
            base.OnConfigurationChanged();
        }

        public void Refresh()
        {
            var fileName = default(string);
            var fileNames = this.GetFileNames().ToArray();
            var index = fileNames.IndexOf(this.FileName);
            if (index < 0 || index >= fileNames.Length - 1)
            {
                fileName = fileNames.FirstOrDefault();
            }
            else
            {
                fileName = fileNames[index + 1];
            }
            if (string.Equals(this.FileName, fileName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            this.FileName = fileName;
            if (!string.IsNullOrEmpty(this.FileName))
            {
                this.LoadImage();
            }
        }

        protected IEnumerable<string> GetFileNames()
        {
            if (string.IsNullOrEmpty(this.Path))
            {
                yield break;
            }
            var paths = this.Path.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Select(
                element => element.Trim()
            ).ToArray();
            foreach (var path in paths)
            {
                if (!string.IsNullOrEmpty(global::System.IO.Path.GetPathRoot(path)))
                {
                    if (Directory.Exists(path))
                    {
                        var fileNames = FileSystemHelper.EnumerateFiles(
                            this.Path,
                            "*.*",
                            FileSystemHelper.SearchOption.Recursive | FileSystemHelper.SearchOption.Sort
                        );
                        foreach (var fileName in fileNames)
                        {
                            yield return fileName;
                        }
                    }
                    else if (File.Exists(path))
                    {
                        yield return path;
                    }
                    continue;
                }
                //Who knows, might work.
                yield return path;
            }
        }

        protected virtual void LoadImage()
        {
            if (string.IsNullOrEmpty(this.FileName))
            {
                return;
            }
            this.Source = new ImageBrush(new BitmapImage(new Uri(this.FileName)));
        }

        protected override Task<bool> ShowSettings()
        {
            return this.ShowSettings(
                Strings.StaticImage_Name,
                StaticImageConfiguration.SECTION
            );
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return StaticImageConfiguration.GetConfigurationSections();
        }

        protected override void OnDisposing()
        {
            if (this.Timer != null)
            {
                this.Timer.Tick -= this.OnTick;
            }
            base.OnDisposing();
        }
    }
}
