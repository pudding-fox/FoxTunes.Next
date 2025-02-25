using System;

namespace FoxTunes
{
    public class UIComponent
    {
        public const string PLACEHOLDER = "00000000-0000-0000-0000-000000000000";

        public UIComponent(string id)
        {
            this.Id = id;
        }

        public UIComponent(UIComponentAttribute attribute, Type type)
        {
            this.Id = attribute.Id;
            this.Name = StringResourceReader.GetString(type, nameof(this.Name)) ?? type.Name;
            this.Description = StringResourceReader.GetString(type, nameof(this.Description)) ?? string.Empty;
            this.Children = attribute.Children;
            this.Role = attribute.Role;
            this.Type = type;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public int Children { get; private set; }

        public UIComponentRole Role { get; private set; }

        public Type Type { get; private set; }

        public bool IsEmpty
        {
            get
            {
                return this.Type == null || this.Type == LayoutManager.PLACEHOLDER;
            }
        }

        public static UIComponent None
        {
            get
            {
                return new UIComponent(PLACEHOLDER);
            }
        }

        public const int NO_CHILDREN = 0;

        public const int UNLIMITED_CHILDREN = -1;
    }
}
