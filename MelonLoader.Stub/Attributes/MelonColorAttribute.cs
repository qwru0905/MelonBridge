using System;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MelonColorAttribute : Attribute
    {
        public ConsoleColor DrawingColor { get; }

        public MelonColorAttribute(ConsoleColor color = ConsoleColor.Green)
        {
            DrawingColor = color;
        }
    }
}
