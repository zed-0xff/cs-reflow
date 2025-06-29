using ST = Microsoft.CodeAnalysis.SpecialType;

public static partial class TypeDB
{
    public class IntInfo : IIntType
    {
        public ST IntTypeID { get; }
        public bool CanBeNegative { get; }

        public IntInfo(ST type, bool isNegative)
        {
            IntTypeID = type;
            CanBeNegative = isNegative;
        }
    }
}
