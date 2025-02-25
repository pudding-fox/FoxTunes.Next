namespace FoxTunes.Interfaces
{
    public interface IUIComponentPanel : IInvocableComponent, IUIComponent
    {
        bool IsInDesignMode { get; set; }

        UIComponentConfiguration Configuration { get; set; }
    }
}
