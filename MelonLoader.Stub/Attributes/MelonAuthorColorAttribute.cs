using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonAuthorColorAttribute : Attribute
    {
        public ConsoleColor DrawingColor { get; }

        public MelonAuthorColorAttribute(ConsoleColor color = ConsoleColor.DarkGray)
        {
            DrawingColor = color;
        }
    }
}
