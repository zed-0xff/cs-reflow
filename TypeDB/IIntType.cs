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
                byte b => new IntInfo(ST.System_Byte, false),
                sbyte sb => new IntInfo(ST.System_SByte, true),
                short s => new IntInfo(ST.System_Int16, true),
                ushort us => new IntInfo(ST.System_UInt16, false),
                int i => new IntInfo(ST.System_Int32, true),
                uint ui => new IntInfo(ST.System_UInt32, false),
                long l => new IntInfo(ST.System_Int64, true),
                ulong ul => new IntInfo(ST.System_UInt64, false),
                _ => throw new ArgumentException($"cannot create IIntType from ({obj.GetType()}) {obj}")
            };
    }
}
