#pragma warning disable format
public partial class ExpressionTests {
    [Fact] void check0_byte_()         { check0("int",    127, "byte",   123, "+", "4"        ); }
    [Fact] void check0_byte_byte()     { check0("int",    127, "byte",   123, "+", "(byte)4"  ); }
    [Fact] void check0_byte_sbyte()    { check0("int",    127, "byte",   123, "+", "(sbyte)4" ); }
    [Fact] void check0_byte_short()    { check0("int",    127, "byte",   123, "+", "(short)4" ); }
    [Fact] void check0_byte_ushort()   { check0("int",    127, "byte",   123, "+", "(ushort)4"); }
    [Fact] void check0_byte_int()      { check0("int",    127, "byte",   123, "+", "(int)4"   ); }
    [Fact] void check0_byte_uint()     { check0("uint",   127, "byte",   123, "+", "(uint)4"  ); }
    [Fact] void check0_byte_long()     { check0("long",   127, "byte",   123, "+", "(long)4"  ); }
    [Fact] void check0_byte_ulong()    { check0("ulong",  127, "byte",   123, "+", "(ulong)4" ); }
    [Fact] void check0_sbyte_()        { check0("int",    127, "sbyte",  123, "+", "4"        ); }
    [Fact] void check0_sbyte_byte()    { check0("int",    127, "sbyte",  123, "+", "(byte)4"  ); }
    [Fact] void check0_sbyte_sbyte()   { check0("int",    127, "sbyte",  123, "+", "(sbyte)4" ); }
    [Fact] void check0_sbyte_short()   { check0("int",    127, "sbyte",  123, "+", "(short)4" ); }
    [Fact] void check0_sbyte_ushort()  { check0("int",    127, "sbyte",  123, "+", "(ushort)4"); }
    [Fact] void check0_sbyte_int()     { check0("int",    127, "sbyte",  123, "+", "(int)4"   ); }
    [Fact] void check0_sbyte_uint()    { check0("long",   127, "sbyte",  123, "+", "(uint)4"  ); }
    [Fact] void check0_sbyte_long()    { check0("long",   127, "sbyte",  123, "+", "(long)4"  ); }
    [Fact] void check0_sbyte_ulong()   { check0_err("sbyte", 123, "+", "(ulong)4"); }
    [Fact] void check0_short_()        { check0("int",    127, "short",  123, "+", "4"        ); }
    [Fact] void check0_short_byte()    { check0("int",    127, "short",  123, "+", "(byte)4"  ); }
    [Fact] void check0_short_sbyte()   { check0("int",    127, "short",  123, "+", "(sbyte)4" ); }
    [Fact] void check0_short_short()   { check0("int",    127, "short",  123, "+", "(short)4" ); }
    [Fact] void check0_short_ushort()  { check0("int",    127, "short",  123, "+", "(ushort)4"); }
    [Fact] void check0_short_int()     { check0("int",    127, "short",  123, "+", "(int)4"   ); }
    [Fact] void check0_short_uint()    { check0("long",   127, "short",  123, "+", "(uint)4"  ); }
    [Fact] void check0_short_long()    { check0("long",   127, "short",  123, "+", "(long)4"  ); }
    [Fact] void check0_short_ulong()   { check0_err("short", 123, "+", "(ulong)4"); }
    [Fact] void check0_ushort_()       { check0("int",    127, "ushort", 123, "+", "4"        ); }
    [Fact] void check0_ushort_byte()   { check0("int",    127, "ushort", 123, "+", "(byte)4"  ); }
    [Fact] void check0_ushort_sbyte()  { check0("int",    127, "ushort", 123, "+", "(sbyte)4" ); }
    [Fact] void check0_ushort_short()  { check0("int",    127, "ushort", 123, "+", "(short)4" ); }
    [Fact] void check0_ushort_ushort() { check0("int",    127, "ushort", 123, "+", "(ushort)4"); }
    [Fact] void check0_ushort_int()    { check0("int",    127, "ushort", 123, "+", "(int)4"   ); }
    [Fact] void check0_ushort_uint()   { check0("uint",   127, "ushort", 123, "+", "(uint)4"  ); }
    [Fact] void check0_ushort_long()   { check0("long",   127, "ushort", 123, "+", "(long)4"  ); }
    [Fact] void check0_ushort_ulong()  { check0("ulong",  127, "ushort", 123, "+", "(ulong)4" ); }
    [Fact] void check0_int_()          { check0("int",    127, "int",    123, "+", "4"        ); }
    [Fact] void check0_int_byte()      { check0("int",    127, "int",    123, "+", "(byte)4"  ); }
    [Fact] void check0_int_sbyte()     { check0("int",    127, "int",    123, "+", "(sbyte)4" ); }
    [Fact] void check0_int_short()     { check0("int",    127, "int",    123, "+", "(short)4" ); }
    [Fact] void check0_int_ushort()    { check0("int",    127, "int",    123, "+", "(ushort)4"); }
    [Fact] void check0_int_int()       { check0("int",    127, "int",    123, "+", "(int)4"   ); }
    [Fact] void check0_int_uint()      { check0("long",   127, "int",    123, "+", "(uint)4"  ); }
    [Fact] void check0_int_long()      { check0("long",   127, "int",    123, "+", "(long)4"  ); }
    [Fact] void check0_int_ulong()     { check0_err("int", 123, "+", "(ulong)4"); }
    [Fact] void check0_uint_()         { check0("uint",   127, "uint",   123, "+", "4"        ); }
    [Fact] void check0_uint_byte()     { check0("uint",   127, "uint",   123, "+", "(byte)4"  ); }
    [Fact] void check0_uint_sbyte()    { check0("long",   127, "uint",   123, "+", "(sbyte)4" ); }
    [Fact] void check0_uint_short()    { check0("long",   127, "uint",   123, "+", "(short)4" ); }
    [Fact] void check0_uint_ushort()   { check0("uint",   127, "uint",   123, "+", "(ushort)4"); }
    [Fact] void check0_uint_int()      { check0("uint",   127, "uint",   123, "+", "(int)4"   ); }
    [Fact] void check0_uint_uint()     { check0("uint",   127, "uint",   123, "+", "(uint)4"  ); }
    [Fact] void check0_uint_long()     { check0("long",   127, "uint",   123, "+", "(long)4"  ); }
    [Fact] void check0_uint_ulong()    { check0("ulong",  127, "uint",   123, "+", "(ulong)4" ); }
    [Fact] void check0_long_()         { check0("long",   127, "long",   123, "+", "4"        ); }
    [Fact] void check0_long_byte()     { check0("long",   127, "long",   123, "+", "(byte)4"  ); }
    [Fact] void check0_long_sbyte()    { check0("long",   127, "long",   123, "+", "(sbyte)4" ); }
    [Fact] void check0_long_short()    { check0("long",   127, "long",   123, "+", "(short)4" ); }
    [Fact] void check0_long_ushort()   { check0("long",   127, "long",   123, "+", "(ushort)4"); }
    [Fact] void check0_long_int()      { check0("long",   127, "long",   123, "+", "(int)4"   ); }
    [Fact] void check0_long_uint()     { check0("long",   127, "long",   123, "+", "(uint)4"  ); }
    [Fact] void check0_long_long()     { check0("long",   127, "long",   123, "+", "(long)4"  ); }
    [Fact] void check0_long_ulong()    { check0_err("long", 123, "+", "(ulong)4"); }
    [Fact] void check0_ulong_()        { check0("ulong",  127, "ulong",  123, "+", "4"        ); }
    [Fact] void check0_ulong_byte()    { check0("ulong",  127, "ulong",  123, "+", "(byte)4"  ); }
    [Fact] void check0_ulong_sbyte()   { check0_err("ulong", 123, "+", "(sbyte)4"); }
    [Fact] void check0_ulong_short()   { check0_err("ulong", 123, "+", "(short)4"); }
    [Fact] void check0_ulong_ushort()  { check0("ulong",  127, "ulong",  123, "+", "(ushort)4"); }
    [Fact] void check0_ulong_int()     { check0("ulong",  127, "ulong",  123, "+", "(int)4"   ); }
    [Fact] void check0_ulong_uint()    { check0("ulong",  127, "ulong",  123, "+", "(uint)4"  ); }
    [Fact] void check0_ulong_long()    { check0("ulong",  127, "ulong",  123, "+", "(long)4"  ); }
    [Fact] void check0_ulong_ulong()   { check0("ulong",  127, "ulong",  123, "+", "(ulong)4" ); }
    [Fact] void check1_byte_()         { check1("int",    127, "byte",   123, "+", "4"        ); }
    [Fact] void check1_byte_byte()     { check1("int",    127, "byte",   123, "+", "(byte)4"  ); }
    [Fact] void check1_byte_sbyte()    { check1("int",    127, "byte",   123, "+", "(sbyte)4" ); }
    [Fact] void check1_byte_short()    { check1("int",    127, "byte",   123, "+", "(short)4" ); }
    [Fact] void check1_byte_ushort()   { check1("int",    127, "byte",   123, "+", "(ushort)4"); }
    [Fact] void check1_byte_int()      { check1("int",    127, "byte",   123, "+", "(int)4"   ); }
    [Fact] void check1_byte_uint()     { check1("uint",   127, "byte",   123, "+", "(uint)4"  ); }
    [Fact] void check1_byte_long()     { check1("long",   127, "byte",   123, "+", "(long)4"  ); }
    [Fact] void check1_byte_ulong()    { check1("ulong",  127, "byte",   123, "+", "(ulong)4" ); }
    [Fact] void check1_sbyte_()        { check1("int",    127, "sbyte",  123, "+", "4"        ); }
    [Fact] void check1_sbyte_byte()    { check1("int",    127, "sbyte",  123, "+", "(byte)4"  ); }
    [Fact] void check1_sbyte_sbyte()   { check1("int",    127, "sbyte",  123, "+", "(sbyte)4" ); }
    [Fact] void check1_sbyte_short()   { check1("int",    127, "sbyte",  123, "+", "(short)4" ); }
    [Fact] void check1_sbyte_ushort()  { check1("int",    127, "sbyte",  123, "+", "(ushort)4"); }
    [Fact] void check1_sbyte_int()     { check1("int",    127, "sbyte",  123, "+", "(int)4"   ); }
    [Fact] void check1_sbyte_uint()    { check1("long",   127, "sbyte",  123, "+", "(uint)4"  ); }
    [Fact] void check1_sbyte_long()    { check1("long",   127, "sbyte",  123, "+", "(long)4"  ); }
    [Fact] void check1_sbyte_ulong()   { check1_err("sbyte", 123, "+", "(ulong)4"); }
    [Fact] void check1_short_()        { check1("int",    127, "short",  123, "+", "4"        ); }
    [Fact] void check1_short_byte()    { check1("int",    127, "short",  123, "+", "(byte)4"  ); }
    [Fact] void check1_short_sbyte()   { check1("int",    127, "short",  123, "+", "(sbyte)4" ); }
    [Fact] void check1_short_short()   { check1("int",    127, "short",  123, "+", "(short)4" ); }
    [Fact] void check1_short_ushort()  { check1("int",    127, "short",  123, "+", "(ushort)4"); }
    [Fact] void check1_short_int()     { check1("int",    127, "short",  123, "+", "(int)4"   ); }
    [Fact] void check1_short_uint()    { check1("long",   127, "short",  123, "+", "(uint)4"  ); }
    [Fact] void check1_short_long()    { check1("long",   127, "short",  123, "+", "(long)4"  ); }
    [Fact] void check1_short_ulong()   { check1_err("short", 123, "+", "(ulong)4"); }
    [Fact] void check1_ushort_()       { check1("int",    127, "ushort", 123, "+", "4"        ); }
    [Fact] void check1_ushort_byte()   { check1("int",    127, "ushort", 123, "+", "(byte)4"  ); }
    [Fact] void check1_ushort_sbyte()  { check1("int",    127, "ushort", 123, "+", "(sbyte)4" ); }
    [Fact] void check1_ushort_short()  { check1("int",    127, "ushort", 123, "+", "(short)4" ); }
    [Fact] void check1_ushort_ushort() { check1("int",    127, "ushort", 123, "+", "(ushort)4"); }
    [Fact] void check1_ushort_int()    { check1("int",    127, "ushort", 123, "+", "(int)4"   ); }
    [Fact] void check1_ushort_uint()   { check1("uint",   127, "ushort", 123, "+", "(uint)4"  ); }
    [Fact] void check1_ushort_long()   { check1("long",   127, "ushort", 123, "+", "(long)4"  ); }
    [Fact] void check1_ushort_ulong()  { check1("ulong",  127, "ushort", 123, "+", "(ulong)4" ); }
    [Fact] void check1_int_()          { check1("int",    127, "int",    123, "+", "4"        ); }
    [Fact] void check1_int_byte()      { check1("int",    127, "int",    123, "+", "(byte)4"  ); }
    [Fact] void check1_int_sbyte()     { check1("int",    127, "int",    123, "+", "(sbyte)4" ); }
    [Fact] void check1_int_short()     { check1("int",    127, "int",    123, "+", "(short)4" ); }
    [Fact] void check1_int_ushort()    { check1("int",    127, "int",    123, "+", "(ushort)4"); }
    [Fact] void check1_int_int()       { check1("int",    127, "int",    123, "+", "(int)4"   ); }
    [Fact] void check1_int_uint()      { check1("long",   127, "int",    123, "+", "(uint)4"  ); }
    [Fact] void check1_int_long()      { check1("long",   127, "int",    123, "+", "(long)4"  ); }
    [Fact] void check1_int_ulong()     { check1_err("int", 123, "+", "(ulong)4"); }
    [Fact] void check1_uint_()         { check1("uint",   127, "uint",   123, "+", "4"        ); }
    [Fact] void check1_uint_byte()     { check1("uint",   127, "uint",   123, "+", "(byte)4"  ); }
    [Fact] void check1_uint_sbyte()    { check1("long",   127, "uint",   123, "+", "(sbyte)4" ); }
    [Fact] void check1_uint_short()    { check1("long",   127, "uint",   123, "+", "(short)4" ); }
    [Fact] void check1_uint_ushort()   { check1("uint",   127, "uint",   123, "+", "(ushort)4"); }
    [Fact] void check1_uint_int()      { check1("uint",   127, "uint",   123, "+", "(int)4"   ); }
    [Fact] void check1_uint_uint()     { check1("uint",   127, "uint",   123, "+", "(uint)4"  ); }
    [Fact] void check1_uint_long()     { check1("long",   127, "uint",   123, "+", "(long)4"  ); }
    [Fact] void check1_uint_ulong()    { check1("ulong",  127, "uint",   123, "+", "(ulong)4" ); }
    [Fact] void check1_long_()         { check1("long",   127, "long",   123, "+", "4"        ); }
    [Fact] void check1_long_byte()     { check1("long",   127, "long",   123, "+", "(byte)4"  ); }
    [Fact] void check1_long_sbyte()    { check1("long",   127, "long",   123, "+", "(sbyte)4" ); }
    [Fact] void check1_long_short()    { check1("long",   127, "long",   123, "+", "(short)4" ); }
    [Fact] void check1_long_ushort()   { check1("long",   127, "long",   123, "+", "(ushort)4"); }
    [Fact] void check1_long_int()      { check1("long",   127, "long",   123, "+", "(int)4"   ); }
    [Fact] void check1_long_uint()     { check1("long",   127, "long",   123, "+", "(uint)4"  ); }
    [Fact] void check1_long_long()     { check1("long",   127, "long",   123, "+", "(long)4"  ); }
    [Fact] void check1_long_ulong()    { check1_err("long", 123, "+", "(ulong)4"); }
    [Fact] void check1_ulong_()        { check1("ulong",  127, "ulong",  123, "+", "4"        ); }
    [Fact] void check1_ulong_byte()    { check1("ulong",  127, "ulong",  123, "+", "(byte)4"  ); }
    [Fact] void check1_ulong_sbyte()   { check1_err("ulong", 123, "+", "(sbyte)4"); }
    [Fact] void check1_ulong_short()   { check1_err("ulong", 123, "+", "(short)4"); }
    [Fact] void check1_ulong_ushort()  { check1("ulong",  127, "ulong",  123, "+", "(ushort)4"); }
    [Fact] void check1_ulong_int()     { check1("ulong",  127, "ulong",  123, "+", "(int)4"   ); }
    [Fact] void check1_ulong_uint()    { check1("ulong",  127, "ulong",  123, "+", "(uint)4"  ); }
    [Fact] void check1_ulong_long()    { check1("ulong",  127, "ulong",  123, "+", "(long)4"  ); }
    [Fact] void check1_ulong_ulong()   { check1("ulong",  127, "ulong",  123, "+", "(ulong)4" ); }
}
#pragma warning restore format
