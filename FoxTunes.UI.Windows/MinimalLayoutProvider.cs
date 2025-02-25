using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    [ComponentPreference(ReleaseType.Minimal)]
    public class MinimalLayoutProvider : UILayoutProviderBase
    {
        public const string ID = "AAAAA18B-86F0-45A6-988D-B15A56128429";

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
                return Strings.MinimalLayoutProvider_Name;
            }
        }

        public override string Description
        {
            get
            {
                return Strings.MinimalLayoutProvider_Description;
            }
        }

        public override UIComponentBase Load(UILayoutTemplate template)
        {
            switch (template)
            {
                case UILayoutTemplate.Main:
                    return new MinimalLayout();
            }
            throw new NotImplementedException();
        }
    }
}
