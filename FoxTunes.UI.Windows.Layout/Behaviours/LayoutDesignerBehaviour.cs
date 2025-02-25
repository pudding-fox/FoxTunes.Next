using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class LayoutDesignerBehaviour : StandardBehaviour, IInvocableComponent, IDisposable
    {
        public const string DESIGN = "ZZZZ";

        static LayoutDesignerBehaviour()
        {
            global::FoxTunes.Windows.Registrations.Add(new global::FoxTunes.Windows.WindowRegistration(LayoutTreeWindow.ID, UserInterfaceWindowRole.None, () => new LayoutTreeWindow()));
        }

        public LayoutDesignerBehaviour()
        {
            this.Overlays = new List<UIComponentDesignerOverlay>();
            Instance = this;
        }

        public IList<UIComponentDesignerOverlay> Overlays { get; private set; }

        public LayoutDesignerBehaviourTask DesignerTask { get; private set; }

        public bool Enabled
        {
            get
            {
                return UIComponentRoot.Active.Any();
            }
        }

        private bool _IsDesigning { get; set; }

        public bool IsDesigning
        {
            get
            {
                return this._IsDesigning;
            }
            set
            {
                if (this._IsDesigning == value)
                {
                    return;
                }
                this._IsDesigning = value;
                this.OnIsDesigningChanged();
            }
        }

        protected virtual void OnIsDesigningChanged()
        {
            if (this.IsDesigning)
            {
                this.ShowDesignerOverlay();
            }
            else
            {
                this.HideDesignerOverlay();
            }
            if (this.IsDesigningChanged != null)
            {
                this.IsDesigningChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsDesigning");
        }

        public event EventHandler IsDesigningChanged;

        public ICore Core { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            Windows.ShuttingDown += this.OnShuttingDown;
            UIComponentRoot.ActiveChanged += this.OnActiveChanged;
            Windows.Registrations.AddIsVisibleChanged(LayoutTreeWindow.ID, this.OnWindowIsVisibleChanged);
            this.Core = core;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            base.InitializeComponent(core);
        }

        protected virtual void OnShuttingDown(object sender, EventArgs e)
        {
            this.IsDesigning = false;
        }

        protected virtual void OnActiveChanged(object sender, EventArgs e)
        {
            if (!this.IsDesigning)
            {
                return;
            }
            var root = sender as UIComponentRoot;
            if (root == null || !UIComponentRoot.Active.Contains(root))
            {
                return;
            }
            this.ShowDesignerOverlay(root);
        }

        protected virtual void OnWindowIsVisibleChanged(object sender, EventArgs e)
        {
            this.IsDesigning = Windows.Registrations.IsVisible(LayoutTreeWindow.ID);
        }

        protected virtual void ShowDesignerOverlay()
        {
            this.DesignerTask = new LayoutDesignerBehaviourTask();
            this.DesignerTask.InitializeComponent(this.Core);
            var task = this.BackgroundTaskEmitter.Send(this.DesignerTask);
            this.Dispatch(this.DesignerTask.Run);
            foreach (var root in UIComponentRoot.Active)
            {
                this.ShowDesignerOverlay(root);
            }

            Windows.Registrations.Show(LayoutTreeWindow.ID);
        }

        protected virtual void ShowDesignerOverlay(UIComponentRoot root)
        {
            var designerOverlay = new UIComponentDesignerOverlay(root);
            designerOverlay.InitializeComponent(this.Core);
            this.Overlays.Add(designerOverlay);
        }

        public void ShowDesignerOverlay(UIComponentConfiguration configuration)
        {
            var container = default(UIComponentContainer);
            var designerOverlay = this.GetDesignerOverlay(configuration, out container);
            if (designerOverlay == null)
            {
                return;
            }
            designerOverlay.ShowDesignerOverlay(container);
        }

        protected virtual UIComponentDesignerOverlay GetDesignerOverlay(UIComponentConfiguration configuration, out UIComponentContainer container)
        {
            foreach (var designerOverlay in this.Overlays)
            {
                if (!designerOverlay.Root.Contains(configuration, out container))
                {
                    continue;
                }
                return designerOverlay;
            }
            container = null;
            return null;
        }

        protected virtual void HideDesignerOverlay()
        {
            if (this.DesignerTask != null)
            {
                this.DesignerTask.Dispose();
                this.DesignerTask = null;
            }

            foreach (var designerOverlay in this.Overlays)
            {
                designerOverlay.Dispose();
            }
            this.Overlays.Clear();

            Windows.Registrations.Hide(LayoutTreeWindow.ID);
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_SETTINGS;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.Enabled)
                {
                    yield return new InvocationComponent(
                        InvocationComponent.CATEGORY_SETTINGS,
                        DESIGN,
                        Strings.LayoutDesignerBehaviour_Design,
                        path: Strings.LayoutDesignerBehaviour_Path,
                        attributes: (byte)((this.IsDesigning ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE) | InvocationComponent.ATTRIBUTE_SEPARATOR)
                    );
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case DESIGN:
                    return this.Design();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Task Design()
        {
            return Windows.Invoke(() => this.IsDesigning = !this.IsDesigning);
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
            this.HideDesignerOverlay();
            Windows.ShuttingDown -= this.OnShuttingDown;
            UIComponentRoot.ActiveChanged -= this.OnActiveChanged;
            Windows.Registrations.RemoveIsVisibleChanged(LayoutTreeWindow.ID, this.OnWindowIsVisibleChanged);
        }

        ~LayoutDesignerBehaviour()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }

        public static LayoutDesignerBehaviour Instance { get; private set; }

        public class LayoutDesignerBehaviourTask : BackgroundTask
        {
            public const string ID = "A8C4F704-F688-4CD2-8CF8-BAF8C7815759";

            public LayoutDesignerBehaviourTask() : base(ID)
            {
                this.Name = Strings.LayoutDesignerBehaviourTask_Name;
                this.Handle = new AutoResetEvent(false);
            }

            public AutoResetEvent Handle { get; private set; }

            public override bool Visible
            {
                get
                {
                    return true;
                }
            }

            public override bool Cancellable
            {
                get
                {
                    return true;
                }
            }

            protected override Task OnRun()
            {
                this.Handle.WaitOne();
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }

            protected override void OnCancellationRequested()
            {
                var task = Windows.Invoke(() => LayoutDesignerBehaviour.Instance.IsDesigning = false);
                base.OnCancellationRequested();
            }

            protected override void OnDisposing()
            {
                if (this.Handle != null)
                {
                    this.Handle.Set();
                    this.Handle.Dispose();
                }
                base.OnDisposing();
            }
        }
    }
}
