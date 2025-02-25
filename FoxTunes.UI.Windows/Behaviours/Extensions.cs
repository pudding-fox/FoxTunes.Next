using FoxTunes.Interfaces;
using System;
using System.Linq;
using System.Windows.Input;

namespace FoxTunes
{
    public static partial class Extensions
    {
        public static bool AddInputHook(this IInputManager inputManager, string phrase, Action action)
        {
            var modifiers = default(int);
            var keys = default(int);
            if (!TryGetKeys(phrase, out modifiers, out keys))
            {
                return false;
            }
            inputManager.AddInputHook(modifiers, keys, action);
            return true;
        }

        public static void RemoveInputHook(this IInputManager inputManager, string phrase)
        {
            var modifiers = default(int);
            var keys = default(int);
            if (!TryGetKeys(phrase, out modifiers, out keys))
            {
                throw new NotImplementedException();
            }
            inputManager.RemoveInputHook(modifiers, keys);
        }

        public static bool TryGetKeys(this string phrase, out ModifierKeys modifiers, out Key keys)
        {
            modifiers = ModifierKeys.None;
            if (string.IsNullOrEmpty(phrase))
            {
                keys = default(int);
                return false;
            }
            var key = Key.None;
            var sequence = phrase.Split(new[] { "+" }, StringSplitOptions.RemoveEmptyEntries).Select(element => element.Trim());
            foreach (var element in sequence)
            {
                var modifier = default(ModifierKeys);
                if (Enum.TryParse<ModifierKeys>(element, true, out modifier))
                {
                    modifiers |= modifier;
                }
                else if (Enum.TryParse<Key>(element, true, out key))
                {
                    //Nothing to do.
                }
                else
                {
                    keys = default(int);
                    return false;
                }
            }
            keys = key;
            return true;
        }

        public static bool TryGetKeys(this string phrase, out int modifiers, out int keys)
        {
            var _keys = default(Key);
            var _modifiers = default(ModifierKeys);
            if (!phrase.TryGetKeys(out _modifiers, out _keys))
            {
                modifiers = 0;
                keys = 0;
                return false;
            }
            modifiers = (int)_modifiers;
            keys = KeyInterop.VirtualKeyFromKey(_keys);
            return true;
        }
    }
}
