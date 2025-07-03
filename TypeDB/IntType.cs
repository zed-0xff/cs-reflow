using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Numerics;

public static partial class TypeDB
{
    public class IntType : IIntType
    {
        public readonly SpecialType id;
        public bool CanBeNegative => signed;
        public SpecialType IntTypeID => id;

        public readonly string Name;
        public readonly int nbits;
        public readonly int ByteSize;
        public readonly bool signed;

        public readonly long MinValue;
        public readonly long MaxSignedValue;
        public readonly ulong MaxUnsignedValue;

        public readonly long Mask;
        public readonly long SignMask;
        public readonly LongRange Range;
        public readonly BitSpan BitSpan;

        public IntType(string name, int nbits, bool signed, SpecialType id)
        {
            this.id = id;
            Name = name;
            this.nbits = nbits;
            this.ByteSize = nbits / 8;
            this.signed = signed;
            MinValue = signed ? -(1L << (nbits - 1)) : 0;
            MaxSignedValue = (nbits == 64 || signed) ? (1L << (nbits - 1)) - 1 : (1L << nbits) - 1;
            MaxUnsignedValue = (nbits == 64 && !signed) ? unchecked((ulong)(-1L)) : (ulong)MaxSignedValue;

            Mask = (1L << nbits) - 1;
            SignMask = signed ? (1L << (nbits - 1)) : 0;
            Range = new LongRange(MinValue, MaxSignedValue);
            BitSpan = new BitSpan(0, Mask);
        }

        public override string ToString() => Name;
        public override bool Equals(object? obj) => (obj is IntType other) && nbits == other.nbits && id == other.id;
        public override int GetHashCode() => HashCode.Combine(nbits, signed);

        public bool CanFit(long value)
        {
            return value >= MinValue && value <= MaxSignedValue;
        }

        public bool CanFit(ulong value)
        {
            return value <= MaxUnsignedValue;
        }

        public object ConvertAny(object value)
        {
            if (value is IntConstExpr ice)
                return ice.Materialize(this);

            return ConvertInt(value);
        }

        // converts the argument to boxed int of the type represented by this IntType
        public object ConvertInt(object value)
        {
            long l = Convert.ToInt64(value);
            // switch tries to find the best match for the type, so if all types are integers of different sizes => long will be used
            // but if at least one branch returns object => all branches will return object
            object result = id switch
            {
                ST_SByte => (object)(sbyte)l,
                ST_Byte => (byte)l,
                ST_Int16 => (short)l,
                ST_UInt16 => (ushort)l,
                ST_Int32 => (int)l,
                ST_UInt32 => (uint)l,
                ST_UInt64 => (ulong)l,
                ST_Int64 => (long)l,
                ST_Boolean => Convert.ToBoolean(l),
                ST_IntPtr => Bitness switch
                {
                    32 => (object)(int)l,
                    64 => (object)(long)l,
                    _ => throw new NotSupportedException($"TypeDB.Bitness {Bitness} is not supported.")
                },
                ST_UIntPtr => Bitness switch
                {
                    32 => (object)(uint)l,
                    64 => (object)(ulong)l,
                    _ => throw new NotSupportedException($"TypeDB.Bitness {Bitness} is not supported.")
                },
                _ => throw new NotImplementedException($"TypeDB: unsupported cast ({value?.GetType()}) {value} to {Name}.")
            };
            Logger.debug(() => $"{value?.GetType()} {value} to {Name} -> {result?.GetType()} {result} [id: {id}, bitness: {Bitness}]", "IntType.ConvertInt");
            return result;
        }
    }

}
