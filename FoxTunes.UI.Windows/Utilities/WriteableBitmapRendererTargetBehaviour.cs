using System.Collections.Generic;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class WriteableBitmapRendererTargetBehaviour : RendererTargetBehaviour
    {
        public const string ID = "AAAA7DA7-6D97-40A1-B1C6-ADCD8A8EF4E7";

        public override string Id
        {
            get
            {
                return ID;
            }
        }

        public override RendererTarget Create(int width, int height)
        {
            return new WriteableBitmapRendererTarget(width, height);
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return WriteableBitmapRendererTargetBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
