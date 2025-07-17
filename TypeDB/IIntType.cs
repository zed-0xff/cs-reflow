using ST = Microsoft.CodeAnalysis.SpecialType;

public static partial class TypeDB
{
    public interface IIntType
    {
        Microsoft.CodeAnalysis.SpecialType IntTypeID { get; }
        bool CanBeNegative { get; }

        public static IIntType From(object obj) =>
            obj switch
            {
                IIntType iit => iit,
                byte => new IntInfo(ST.System_Byte, false),
                sbyte => new IntInfo(ST.System_SByte, true),
                short => new IntInfo(ST.System_Int16, true),
                ushort => new IntInfo(ST.System_UInt16, false),
                int => new IntInfo(ST.System_Int32, true),
                uint => new IntInfo(ST.System_UInt32, false),
                long => new IntInfo(ST.System_Int64, true),
                ulong => new IntInfo(ST.System_UInt64, false),

                bool => new IntInfo(ST.System_Boolean, false),
                char => new IntInfo(ST.System_Char, false),

                _ => throw new ArgumentException($"cannot create IIntType from ({obj.GetType()}) {obj}")
            };
    }
}
