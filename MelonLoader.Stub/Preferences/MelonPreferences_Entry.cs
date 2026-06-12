using System;

namespace MelonLoader
{
    public class MelonPreferences_Entry<T>
    {
        public string Identifier { get; internal set; }
        public string DisplayName { get; internal set; }
        public string Description { get; internal set; }
        public T DefaultValue { get; internal set; }

        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                OnEntryValueChangedUntyped?.Invoke();
            }
        }

        public event Action OnEntryValueChangedUntyped;

        internal MelonPreferences_Entry(string identifier, string displayName, T defaultValue)
        {
            Identifier = identifier;
            DisplayName = displayName;
            DefaultValue = defaultValue;
            _value = defaultValue;
        }
    }
}
