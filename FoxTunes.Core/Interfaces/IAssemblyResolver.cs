namespace FoxTunes.Interfaces
{
    public interface IAssemblyResolver
    {
        void EnableExecution();

        void EnableReflectionOnly();

        void DisableExecution();

        void DisableReflectionOnly();
    }
}
