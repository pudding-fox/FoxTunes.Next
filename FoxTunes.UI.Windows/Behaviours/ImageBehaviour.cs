using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class ImageBehaviour : StandardBehaviour, IInvocableComponent
    {
        const string REFRESH_IMAGES = "ZAAA";

        public ISignalEmitter SignalEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.SignalEmitter = core.Components.SignalEmitter;
            base.InitializeComponent(core);
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_LIBRARY;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(InvocationComponent.CATEGORY_LIBRARY, REFRESH_IMAGES, Strings.ImageBehaviour_Refresh, path: Strings.ImageBehaviour_Path);
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case REFRESH_IMAGES:
                    return this.RefreshImages();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        private Task RefreshImages()
        {
            return this.SignalEmitter.Send(new Signal(this, CommonSignals.ImagesUpdated));
        }
    }
}
