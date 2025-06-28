using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Numerics;

public static class TypeDB
{
    public static int Bitness = 0;

    public static readonly IntInfo Int8 = new IntInfo("sbyte", typeof(sbyte), 8, true, SyntaxKind.SByteKeyword);
    public static readonly IntInfo UInt8 = new IntInfo("byte", typeof(byte), 8, false, SyntaxKind.ByteKeyword);
    public static readonly IntInfo Int16 = new IntInfo("short", typeof(short), 16, true, SyntaxKind.ShortKeyword);
    public static readonly IntInfo UInt16 = new IntInfo("ushort", typeof(ushort), 16, false, SyntaxKind.UShortKeyword);
    public static readonly IntInfo Int32 = new IntInfo("int", typeof(int), 32, true, SyntaxKind.IntKeyword);
    public static readonly IntInfo UInt32 = new IntInfo("uint", typeof(uint), 32, false, SyntaxKind.UIntKeyword);
    public static readonly IntInfo Int64 = new IntInfo("long", typeof(long), 64, true, SyntaxKind.LongKeyword);
    public static readonly IntInfo UInt64 = new IntInfo("ulong", typeof(ulong), 64, false, SyntaxKind.ULongKeyword);
    public static readonly IntInfo Bool = new IntInfo("bool", typeof(bool), 1, false, SyntaxKind.BoolKeyword);

    public static readonly IntInfo IntPtr32 = new IntInfo("nint", typeof(nint), 32, true, SyntaxKind.IntKeyword);
    public static readonly IntInfo UIntPtr32 = new IntInfo("nuint", typeof(nuint), 32, false, SyntaxKind.UIntKeyword);
    public static readonly IntInfo IntPtr64 = new IntInfo("nint", typeof(nint), 64, true, SyntaxKind.LongKeyword);
    public static readonly IntInfo UIntPtr64 = new IntInfo("nuint", typeof(nuint), 64, false, SyntaxKind.ULongKeyword);

    // aliases
    public static readonly IntInfo Byte = UInt8;
    public static readonly IntInfo SByte = Int8;
    public static readonly IntInfo Short = Int16;
    public static readonly IntInfo UShort = UInt16;
    public static readonly IntInfo Int = Int32;
    public static readonly IntInfo UInt = UInt32;
    public static readonly IntInfo Long = Int64;
    public static readonly IntInfo ULong = UInt64;

    public static IntInfo NInt => bitness_aware(IntPtr32, IntPtr64);
    public static IntInfo NUInt => bitness_aware(UIntPtr32, UIntPtr64);

    public static IntInfo Find(string typeName) => TryFind(typeName) ?? throw new NotImplementedException($"TypeDB: {typeName} not supported.");
    public static IntInfo Find(System.Type type) => Find(type.ToString());
    public static IntInfo Find(TypeSyntax type) => Find(type.ToString());

    public static IntInfo? TryFind(TypeSyntax type) => TryFind(type.ToString());
    public static IntInfo? TryFind(System.Type type) => TryFind(type.ToString());
    public static IntInfo? TryFind(string typeName)
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

            "nint" => NInt,
            "nuint" => NUInt,
            _ => null
        };
    }

    private static IntInfo bitness_aware(IntInfo int32, IntInfo int64) =>
        Bitness switch
        {
            32 => int32,
            64 => int64,
            0 => throw new NotSupportedException($"TypeDB.Bitness is not set."),
            _ => throw new NotSupportedException($"TypeDB.Bitness {Bitness} is not supported.")
        };

    public static string ShortType(string type)
    {
        return type switch
        {
            "System.Boolean" => "bool",
            "Boolean" => "bool",

            "System.Byte" => "byte",
            "System.SByte" => "sbyte",
            "System.Int16" => "short",
            "System.UInt16" => "ushort",
            "System.Int32" => "int",
            "System.UInt32" => "uint",
            "System.Int64" => "long",
            "System.UInt64" => "ulong",

            "Byte" => "byte",
            "SByte" => "sbyte",
            "Int16" => "short",
            "UInt16" => "ushort",
            "Int32" => "int",
            "UInt32" => "uint",
            "Int64" => "long",
            "UInt64" => "ulong",

            "IntPtr" => "nint",
            "System.IntPtr" => "nint",

            "UIntPtr" => "nuint",
            "System.UIntPtr" => "nuint",

            _ => type,
        };
    }

    public interface IIntInfo
    {
        IntInfo IntType { get; }
        bool CanBeNegative { get; }
    }

    public class IntInfo : IIntInfo
    {
        public bool CanBeNegative => signed;
        public IntInfo IntType => this;

        public readonly string Name;
        public readonly System.Type Type;
        public readonly int nbits;
        public readonly int ByteSize;
        public readonly bool signed;
        public readonly SyntaxKind Kind;

        public readonly long MinValue;
        public readonly long MaxSignedValue;
        public readonly ulong MaxUnsignedValue;

        public readonly long Mask;
        public readonly long SignMask;
        public readonly LongRange Range;
        public readonly BitSpan BitSpan;

        public IntInfo(string name, System.Type type, int nbits, bool signed, SyntaxKind kind)
        {
            Name = name;
            Type = type;
            this.nbits = nbits;
            this.ByteSize = nbits / 8;
            this.signed = signed;
            Kind = kind;
            MinValue = signed ? -(1L << (nbits - 1)) : 0;
            MaxSignedValue = (nbits == 64 || signed) ? (1L << (nbits - 1)) - 1 : (1L << nbits) - 1;
            MaxUnsignedValue = (nbits == 64 && !signed) ? unchecked((ulong)(-1L)) : (ulong)MaxSignedValue;

            Mask = (1L << nbits) - 1;
            SignMask = signed ? (1L << (nbits - 1)) : 0;
            Range = new LongRange(MinValue, MaxSignedValue);
            BitSpan = new BitSpan(0, Mask);
        }

        public override string ToString() => Name;
        public override bool Equals(object? obj) => (obj is IntInfo other) && nbits == other.nbits && Type == other.Type;
        public override int GetHashCode() => HashCode.Combine(nbits, signed);

        public bool CanFit(long value)
        {
            return value >= MinValue && value <= MaxSignedValue;
        }

        public bool CanFit(ulong value)
        {
            return value <= MaxUnsignedValue;
        }
    }

    public static (IntInfo?, IntInfo?) PromoteTypes(IIntInfo l, IIntInfo r)
    {
        var ltype = l.IntType;
        var rtype = r.IntType;
        // [floats skipped]
        // if either operand is of type ulong, the OTHER OPERAND is converted to type ulong,
        // or a binding-time error occurs if the other operand is of type sbyte, short, int, or long.
        if (ltype == ULong || rtype == ULong)
        {
            if (ltype != ULong) return (ULong, null);
            if (rtype != ULong) return (null, ULong);
        }

        // Otherwise, if either operand is of type long, the OTHER OPERAND is converted to type long.
        else if (ltype == Long || rtype == Long)
        {
            if (ltype != Long) return (Long, null);
            if (rtype != Long) return (null, Long);
        }

        // Otherwise, if either operand is of type uint and the other operand is of type sbyte, short, or int, BOTH OPERANDS are converted to type long.
        // XXX not always true XXX
        else if (
                (ltype == UInt && (rtype == SByte || rtype == Short || rtype == Int)) ||
                (rtype == UInt && (ltype == SByte || ltype == Short || ltype == Int))
                )
        {
            // if left is uint and right is int, but can fit in uint => right is converted to uint
            if (ltype == UInt && rtype == Int && !r.CanBeNegative)
            {
                return (null, UInt);
            }
            else if (rtype == UInt && ltype == Int && !l.CanBeNegative)
            {
                return (UInt, null);
            }
            else
            {
                return (Long, Long);
            }
        }

        // Otherwise, if either operand is of type uint, the OTHER OPERAND is converted to type uint.
        else if (ltype == UInt || rtype == UInt)
        {
            if (ltype != UInt)
                return (UInt, null);
            if (rtype != UInt)
                return (null, UInt);
        }
        else
        {
            // Otherwise, BOTH OPERANDS are converted to type int.
            return (Int, Int);
        }

        return (null, null); // no promotion necessary
    }
}

