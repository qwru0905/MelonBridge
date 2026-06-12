namespace MelonLoader.Preferences
{
    public abstract class ValueValidator
    {
        public abstract bool IsValid(object value);
        public abstract string EnsureValid(object value);
    }
}
