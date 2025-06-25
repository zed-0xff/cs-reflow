using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Numerics;

public static class TypeDB
{
    public static readonly IntInfo Int8 = new IntInfo("sbyte", typeof(sbyte), 8, true, SyntaxKind.SByteKeyword);
    public static readonly IntInfo UInt8 = new IntInfo("byte", typeof(byte), 8, false, SyntaxKind.ByteKeyword);
    public static readonly IntInfo Int16 = new IntInfo("short", typeof(short), 16, true, SyntaxKind.ShortKeyword);
    public static readonly IntInfo UInt16 = new IntInfo("ushort", typeof(ushort), 16, false, SyntaxKind.UShortKeyword);
    public static readonly IntInfo Int32 = new IntInfo("int", typeof(int), 32, true, SyntaxKind.IntKeyword);
    public static readonly IntInfo UInt32 = new IntInfo("uint", typeof(uint), 32, false, SyntaxKind.UIntKeyword);
    public static readonly IntInfo Int64 = new IntInfo("long", typeof(long), 64, true, SyntaxKind.LongKeyword);
    public static readonly IntInfo UInt64 = new IntInfo("ulong", typeof(ulong), 64, false, SyntaxKind.ULongKeyword);
    public static readonly IntInfo Bool = new IntInfo("bool", typeof(bool), 1, false, SyntaxKind.BoolKeyword);

    // aliases
    public static readonly IntInfo Byte = UInt8;
    public static readonly IntInfo SByte = Int8;
    public static readonly IntInfo Short = Int16;
    public static readonly IntInfo UShort = UInt16;
    public static readonly IntInfo Int = Int32;
    public static readonly IntInfo UInt = UInt32;
    public static readonly IntInfo Long = Int64;
    public static readonly IntInfo ULong = UInt64;

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

            "nint" => Int32,   // TODO: 32/64 bit switch
            "nuint" => UInt32, // TODO: 32/64 bit switch
            _ => null
        };
    }

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
            _ => type,
        };
    }

    public class IntInfo
    {
        public readonly string Name;
        public readonly System.Type Type;
        public readonly int nbits;
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
        public override bool Equals(object obj) => (obj is IntInfo other) && nbits == other.nbits && signed == other.signed;
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
}

