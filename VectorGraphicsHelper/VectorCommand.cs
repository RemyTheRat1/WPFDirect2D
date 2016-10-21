namespace VectorGraphicsHelper
{
    public class VectorCommand
    {
        public VectorCommand(CommandType type, float[] arguments) : this(type, arguments, false) {}

        public VectorCommand(CommandType type, float[] arguments, bool isRelative)
        {
            Type = type;
            Arguments = arguments;
            IsRelative = isRelative;
        }

        public float[] Arguments { get; private set; }

        public CommandType Type { get; private set; }

        public bool IsRelative { get; private set; }
    }
}