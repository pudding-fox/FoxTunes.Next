using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class DefaultLayoutProvider : UILayoutProviderBase
    {
        public const string ID = "BBBB4ED2-782D-4622-ADF4-AAE2B543E0F3";

        public override string Id
        {
            get
            {
                return ID;
            }
        }

        public override string Name
        {
            get
            {
                return Strings.DefaultLayoutProvider_Name;
            }
        }

        public override string Description
        {
            get
            {
                return Strings.DefaultLayoutProvider_Description;
            }
        }

        public override UIComponentBase Load(UILayoutTemplate template)
        {
            switch (template)
            {
                case UILayoutTemplate.Main:
                    return new DefaultLayout();
            }
            throw new NotImplementedException();
        }
    }
}
