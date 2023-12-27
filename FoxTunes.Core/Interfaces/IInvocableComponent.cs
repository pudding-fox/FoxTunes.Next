using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IInvocableComponent : IBaseComponent
    {
        IEnumerable<string> InvocationCategories { get; }

        IEnumerable<IInvocationComponent> Invocations { get; }

        Task InvokeAsync(IInvocationComponent component);
    }
}
