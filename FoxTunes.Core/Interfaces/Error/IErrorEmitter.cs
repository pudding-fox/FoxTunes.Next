using System;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IErrorEmitter : IErrorSource
    {
        Task Send(IBaseComponent source, string message);

        Task Send(IBaseComponent source, Exception exception);

        Task Send(IBaseComponent source, string message, Exception exception);
    }
}
