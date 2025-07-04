using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Numerics;

public static partial class TypeDB
{
    public static int Bitness = 0;

    public const SpecialType ST_SByte = SpecialType.System_SByte;
    public const SpecialType ST_Byte = SpecialType.System_Byte;
    public const SpecialType ST_Char = SpecialType.System_Char;
    public const SpecialType ST_Int16 = SpecialType.System_Int16;
    public const SpecialType ST_UInt16 = SpecialType.System_UInt16;
    public const SpecialType ST_Int32 = SpecialType.System_Int32;
    public const SpecialType ST_UInt32 = SpecialType.System_UInt32;
    public const SpecialType ST_Int64 = SpecialType.System_Int64;
    public const SpecialType ST_UInt64 = SpecialType.System_UInt64;
    public const SpecialType ST_Boolean = SpecialType.System_Boolean;
    public const SpecialType ST_IntPtr = SpecialType.System_IntPtr;
    public const SpecialType ST_UIntPtr = SpecialType.System_UIntPtr;

    public static readonly IntType Int8 = new IntType("sbyte", 8, true, ST_SByte);
    public static readonly IntType UInt8 = new IntType("byte", 8, false, ST_Byte);
    public static readonly IntType Int16 = new IntType("short", 16, true, ST_Int16);
    public static readonly IntType UInt16 = new IntType("ushort", 16, false, ST_UInt16);
    public static readonly IntType Int32 = new IntType("int", 32, true, ST_Int32);
    public static readonly IntType UInt32 = new IntType("uint", 32, false, ST_UInt32);
    public static readonly IntType Int64 = new IntType("long", 64, true, ST_Int64);
    public static readonly IntType UInt64 = new IntType("ulong", 64, false, ST_UInt64);

    public static readonly IntType Bool = new IntType("bool", 1, false, ST_Boolean);
    public static readonly IntType Char = new IntType("char", 16, false, ST_Char);

    public static readonly IntType IntPtr32 = new IntType("nint", 32, true, ST_IntPtr);
    public static readonly IntType UIntPtr32 = new IntType("nuint", 32, false, ST_UIntPtr);

    public static readonly IntType IntPtr64 = new IntType("nint", 64, true, ST_IntPtr);
    public static readonly IntType UIntPtr64 = new IntType("nuint", 64, false, ST_UIntPtr);

    // XXX: make sure it's not leaked into UnknownTypedValue
    private static readonly IntType GUID = new IntType("Guid", 128, false, SpecialType.None);

    // aliases
    public static readonly IntType Byte = UInt8;
    public static readonly IntType SByte = Int8;
    public static readonly IntType Short = Int16;
    public static readonly IntType UShort = UInt16;
    public static readonly IntType Int = Int32;
    public static readonly IntType UInt = UInt32;
    public static readonly IntType Long = Int64;
    public static readonly IntType ULong = UInt64;

    public static IntType NInt => bitness_aware(IntPtr32, IntPtr64);
    public static IntType NUInt => bitness_aware(UIntPtr32, UIntPtr64);

    public static IntType Find(string typeName) => TryFind(typeName) ?? throw new NotImplementedException($"TypeDB: {typeName} not supported.");
    public static IntType Find(System.Type type) => Find(type.ToString());
    public static IntType Find(TypeSyntax type) => Find(type.ToString());

    public static IntType? TryFind(TypeSyntax type) => TryFind(type.ToString());
    public static IntType? TryFind(System.Type type) => TryFind(type.ToString());
    public static IntType? TryFind(string typeName)
    {
        return ShortType(typeName) switch
        {
            "sbyte" => Int8,
            "byte" => UInt8,
            "short" => Int16,
            "ushort" => UInt16,
            "int" => Int32,
            "uint" => UInt32,
            "long" => Int64,
            "ulong" => UInt64,

            "bool" => Bool,
            "char" => Char,

            "nint" => NInt,
            "nuint" => NUInt,
            _ => null
        };
    }

    private static T bitness_aware<T>(T res32, T res64) =>
        Bitness switch
        {
            32 => res32,
            64 => res64,
            0 => throw new InvalidOperationException($"TypeDB.Bitness is not set."),
            _ => throw new NotSupportedException($"TypeDB.Bitness {Bitness} is not supported.")
        };

    public static string ShortType(string type)
    {
        return type switch
        {
            "System.Boolean" => "bool",
            "Boolean" => "bool",

            "System.Char" => "char",
            "Char" => "char",

            "System.Byte" => "byte",
            "System.SByte" => "sbyte",
            "System.Int16" => "short",
            "System.UInt16" => "ushort",
            "System.Int32" => "int",
            "System.UInt32" => "uint",
            "System.Int64" => "long",
            "System.UInt64" => "ulong",
            "System.IntPtr" => "nint",
            "System.UIntPtr" => "nuint",

            "Byte" => "byte",
            "SByte" => "sbyte",
            "Int16" => "short",
            "UInt16" => "ushort",
            "Int32" => "int",
            "UInt32" => "uint",
            "Int64" => "long",
            "UInt64" => "ulong",
            "IntPtr" => "nint",
            "UIntPtr" => "nuint",

            _ => type,
        };
    }

    public static (IntType?, IntType?) Promote(object l, object r)
    {
        var il = IIntType.From(l);
        var ir = IIntType.From(r);
        return PromoteTypes(il, ir);
    }

    // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/expressions#12473-binary-numeric-promotions
    public static (IntType?, IntType?) PromoteTypes(IIntType l, IIntType r)
    {
        var lid = l.IntTypeID;
        var rid = r.IntTypeID;
        switch (lid, rid)
        {
            case (ST_Int32, ST_Int32): // fast check for most common case
                return (null, null);
            // [floats skipped]
            // if either operand is of type ulong, the OTHER OPERAND is converted to type ulong,
            // or a binding-time error occurs if the other operand is of type sbyte, short, int, or long.
            case (ST_UInt64, _) or (_, ST_UInt64):
                if (lid != ST_UInt64) return (ULong, null);
                if (rid != ST_UInt64) return (null, ULong);
                break;
            // Otherwise, if either operand is of type long, the OTHER OPERAND is converted to type long.
            case (ST_Int64, _) or (_, ST_Int64):
                if (lid != ST_Int64) return (Long, null);
                if (rid != ST_Int64) return (null, Long);
                break;
            // Otherwise, if either operand is of type uint and the other operand is of type sbyte, short, or int, BOTH OPERANDS are converted to type long.
            // XXX not always true XXX
            case (ST_UInt32, ST_SByte or ST_Int16 or ST_Int32) or (ST_SByte or ST_Int16 or ST_Int32, ST_UInt32):
                // if left is uint and right is int, but can fit in uint => right is converted to uint
                if (lid == ST_UInt32 && rid == ST_Int32 && !r.CanBeNegative) return (null, UInt);
                if (rid == ST_UInt32 && lid == ST_Int32 && !l.CanBeNegative) return (UInt, null);
                return (Long, Long);
            // Otherwise, if either operand is of type uint, the OTHER OPERAND is converted to type uint.
            case (ST_UInt32, _) or (_, ST_UInt32):
                if (lid != ST_UInt32) return (UInt, null);
                if (rid != ST_UInt32) return (null, UInt);
                break;
            // Otherwise, BOTH OPERANDS are converted to type int.
            default:
                return (Int, Int);
        }
        ;

        return (null, null); // no promotion necessary
    }

    public static int ToInt32(object value) => Convert.ToInt32(Int32.ConvertAny(value));

    public static System.Type? ToSystemType(TypeSyntax type) =>
        type switch
        {
            PredefinedTypeSyntax pts => pts.Keyword.Kind() switch
            {
                SyntaxKind.ByteKeyword => typeof(byte),
                SyntaxKind.SByteKeyword => typeof(sbyte),
                SyntaxKind.ShortKeyword => typeof(short),
                SyntaxKind.UShortKeyword => typeof(ushort),
                SyntaxKind.IntKeyword => typeof(int),
                SyntaxKind.UIntKeyword => typeof(uint),
                SyntaxKind.LongKeyword => typeof(long),
                SyntaxKind.ULongKeyword => typeof(ulong),

                SyntaxKind.FloatKeyword => typeof(float),
                SyntaxKind.DoubleKeyword => typeof(double),

                SyntaxKind.BoolKeyword => typeof(bool),
                SyntaxKind.CharKeyword => typeof(char),

                SyntaxKind.StringKeyword => typeof(string),
                SyntaxKind.ObjectKeyword => typeof(object),

                _ => null
            },
            IdentifierNameSyntax id => id.Identifier.ValueText switch
            {
                // "nint" => typeof(nint), // TODO: host-independent NInt
                // "nuint" => typeof(nuint), // TODO: host-independent NUInt
                "Guid" => typeof(Guid),
                _ => null
            },
            _ => null
        };

    public static int SizeOf(TypeSyntax type) =>
        type switch
        {
            PredefinedTypeSyntax pts => pts.Keyword.Kind() switch
            {
                SyntaxKind.ByteKeyword => sizeof(byte),
                SyntaxKind.SByteKeyword => sizeof(sbyte),
                SyntaxKind.ShortKeyword => sizeof(short),
                SyntaxKind.UShortKeyword => sizeof(ushort),
                SyntaxKind.IntKeyword => sizeof(int),
                SyntaxKind.UIntKeyword => sizeof(uint),
                SyntaxKind.LongKeyword => sizeof(long),
                SyntaxKind.ULongKeyword => sizeof(ulong),

                SyntaxKind.FloatKeyword => sizeof(float),
                SyntaxKind.DoubleKeyword => sizeof(double),

                SyntaxKind.BoolKeyword => sizeof(bool),
                SyntaxKind.CharKeyword => sizeof(char),

                _ => throw new NotSupportedException($"TypeDB.GetSize: {pts.Keyword} is not supported.")
            },
            PointerTypeSyntax => NInt.ByteSize,
            IdentifierNameSyntax id => id.Identifier.ValueText switch
            {
                "nint" => NInt.ByteSize,
                "nuint" => NUInt.ByteSize,
                "Guid" => GUID.ByteSize,
                _ => throw new NotSupportedException($"TypeDB.SizeOf: Identifier '{id.Identifier.ValueText}' is not supported.")
            },
            _ => throw new NotSupportedException($"TypeDB.GetSize: {type} is not supported.")
        };

    public static int SizeOf(object obj) =>
        obj switch
        {
            byte => sizeof(byte),
            sbyte => sizeof(sbyte),
            short => sizeof(short),
            ushort => sizeof(ushort),
            int => sizeof(int),
            uint => sizeof(uint),
            long => sizeof(long),
            ulong => sizeof(ulong),

            nint => NInt.ByteSize,
            nuint => NUInt.ByteSize,

            float => sizeof(float),
            double => sizeof(double),

            bool => sizeof(bool),
            char => sizeof(char),
            Guid => GUID.ByteSize,

            _ => throw new NotSupportedException($"TypeDB.GetSize: {obj.GetType()} is not supported.")
        };

    public static object? Default(TypeSyntax type) =>
        type switch
        {
            PredefinedTypeSyntax pts => pts.Keyword.Kind() switch
            {
                // switch tries to find the best match for the type, so if all types are integers of different sizes => long will be used
                // but if at least one branch returns object => all branches will return object
                SyntaxKind.ByteKeyword => default(byte),
                SyntaxKind.SByteKeyword => default(sbyte),
                SyntaxKind.ShortKeyword => default(short),
                SyntaxKind.UShortKeyword => default(ushort),
                SyntaxKind.IntKeyword => default(int),
                SyntaxKind.UIntKeyword => default(uint),
                SyntaxKind.LongKeyword => default(long),
                SyntaxKind.ULongKeyword => default(ulong),

                SyntaxKind.FloatKeyword => default(float),
                SyntaxKind.DoubleKeyword => default(double),

                SyntaxKind.BoolKeyword => default(bool),
                SyntaxKind.CharKeyword => default(char),
                SyntaxKind.ObjectKeyword => default(object),
                SyntaxKind.StringKeyword => default(string),

                _ => throw new NotSupportedException($"TypeDB.Default: {pts.Keyword} is not supported.")
            },
            ArrayTypeSyntax => null,
            IdentifierNameSyntax id => id.Identifier.ValueText switch
            {
                "nint" => (object)UnknownTypedValue.Zero(NInt),
                "nuint" => (object)UnknownTypedValue.Zero(NUInt),
                "Guid" => Guid.Empty,
                _ => throw new NotSupportedException($"TypeDB.Default: Identifier '{id.Identifier.ValueText}' is not supported.")
            },
            _ => throw new NotSupportedException($"TypeDB.Default: {type} is not supported.")
        };
}

