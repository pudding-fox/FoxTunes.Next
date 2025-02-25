using System;

namespace FoxTunes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class WindowsUserInterfaceDependencyAttribute : ComponentDependencyAttribute
    {
        public WindowsUserInterfaceDependencyAttribute()
        {
            this.Id = WindowsUserInterface.ID;
            this.Slot = ComponentSlots.UserInterface;
        }
    }
}
