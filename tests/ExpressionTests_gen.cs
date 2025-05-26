#pragma warning disable format
public partial class ExpressionTests {
    [Fact] void check0_byte__4()          { check0("int",    127, "byte",   123, "+", "4"        ); }
    [Fact] void check0_byte_byte_4()      { check0("int",    127, "byte",   123, "+", "(byte)4"  ); }
    [Fact] void check0_byte_sbyte_4()     { check0("int",    127, "byte",   123, "+", "(sbyte)4" ); }
    [Fact] void check0_byte_short_4()     { check0("int",    127, "byte",   123, "+", "(short)4" ); }
    [Fact] void check0_byte_ushort_4()    { check0("int",    127, "byte",   123, "+", "(ushort)4"); }
    [Fact] void check0_byte_int_4()       { check0("int",    127, "byte",   123, "+", "(int)4"   ); }
    [Fact] void check0_byte_uint_4()      { check0("uint",   127, "byte",   123, "+", "(uint)4"  ); }
    [Fact] void check0_byte_long_4()      { check0("long",   127, "byte",   123, "+", "(long)4"  ); }
    [Fact] void check0_byte_ulong_4()     { check0("ulong",  127, "byte",   123, "+", "(ulong)4" ); }
    [Fact] void check0_sbyte__4()         { check0("int",    127, "sbyte",  123, "+", "4"        ); }
    [Fact] void check0_sbyte_byte_4()     { check0("int",    127, "sbyte",  123, "+", "(byte)4"  ); }
    [Fact] void check0_sbyte_sbyte_4()    { check0("int",    127, "sbyte",  123, "+", "(sbyte)4" ); }
    [Fact] void check0_sbyte_short_4()    { check0("int",    127, "sbyte",  123, "+", "(short)4" ); }
    [Fact] void check0_sbyte_ushort_4()   { check0("int",    127, "sbyte",  123, "+", "(ushort)4"); }
    [Fact] void check0_sbyte_int_4()      { check0("int",    127, "sbyte",  123, "+", "(int)4"   ); }
    [Fact] void check0_sbyte_uint_4()     { check0("long",   127, "sbyte",  123, "+", "(uint)4"  ); }
    [Fact] void check0_sbyte_long_4()     { check0("long",   127, "sbyte",  123, "+", "(long)4"  ); }
    [Fact] void check0_sbyte_ulong_4()    { check0_err("sbyte", 123, "+", "(ulong)4"); }
    [Fact] void check0_short__4()         { check0("int",    127, "short",  123, "+", "4"        ); }
    [Fact] void check0_short_byte_4()     { check0("int",    127, "short",  123, "+", "(byte)4"  ); }
    [Fact] void check0_short_sbyte_4()    { check0("int",    127, "short",  123, "+", "(sbyte)4" ); }
    [Fact] void check0_short_short_4()    { check0("int",    127, "short",  123, "+", "(short)4" ); }
    [Fact] void check0_short_ushort_4()   { check0("int",    127, "short",  123, "+", "(ushort)4"); }
    [Fact] void check0_short_int_4()      { check0("int",    127, "short",  123, "+", "(int)4"   ); }
    [Fact] void check0_short_uint_4()     { check0("long",   127, "short",  123, "+", "(uint)4"  ); }
    [Fact] void check0_short_long_4()     { check0("long",   127, "short",  123, "+", "(long)4"  ); }
    [Fact] void check0_short_ulong_4()    { check0_err("short", 123, "+", "(ulong)4"); }
    [Fact] void check0_ushort__4()        { check0("int",    127, "ushort", 123, "+", "4"        ); }
    [Fact] void check0_ushort_byte_4()    { check0("int",    127, "ushort", 123, "+", "(byte)4"  ); }
    [Fact] void check0_ushort_sbyte_4()   { check0("int",    127, "ushort", 123, "+", "(sbyte)4" ); }
    [Fact] void check0_ushort_short_4()   { check0("int",    127, "ushort", 123, "+", "(short)4" ); }
    [Fact] void check0_ushort_ushort_4()  { check0("int",    127, "ushort", 123, "+", "(ushort)4"); }
    [Fact] void check0_ushort_int_4()     { check0("int",    127, "ushort", 123, "+", "(int)4"   ); }
    [Fact] void check0_ushort_uint_4()    { check0("uint",   127, "ushort", 123, "+", "(uint)4"  ); }
    [Fact] void check0_ushort_long_4()    { check0("long",   127, "ushort", 123, "+", "(long)4"  ); }
    [Fact] void check0_ushort_ulong_4()   { check0("ulong",  127, "ushort", 123, "+", "(ulong)4" ); }
    [Fact] void check0_int__4()           { check0("int",    127, "int",    123, "+", "4"        ); }
    [Fact] void check0_int_byte_4()       { check0("int",    127, "int",    123, "+", "(byte)4"  ); }
    [Fact] void check0_int_sbyte_4()      { check0("int",    127, "int",    123, "+", "(sbyte)4" ); }
    [Fact] void check0_int_short_4()      { check0("int",    127, "int",    123, "+", "(short)4" ); }
    [Fact] void check0_int_ushort_4()     { check0("int",    127, "int",    123, "+", "(ushort)4"); }
    [Fact] void check0_int_int_4()        { check0("int",    127, "int",    123, "+", "(int)4"   ); }
    [Fact] void check0_int_uint_4()       { check0("long",   127, "int",    123, "+", "(uint)4"  ); }
    [Fact] void check0_int_long_4()       { check0("long",   127, "int",    123, "+", "(long)4"  ); }
    [Fact] void check0_int_ulong_4()      { check0_err("int", 123, "+", "(ulong)4"); }
    [Fact] void check0_uint__4()          { check0("uint",   127, "uint",   123, "+", "4"        ); }
    [Fact] void check0_uint_byte_4()      { check0("uint",   127, "uint",   123, "+", "(byte)4"  ); }
    [Fact] void check0_uint_sbyte_4()     { check0("long",   127, "uint",   123, "+", "(sbyte)4" ); }
    [Fact] void check0_uint_short_4()     { check0("long",   127, "uint",   123, "+", "(short)4" ); }
    [Fact] void check0_uint_ushort_4()    { check0("uint",   127, "uint",   123, "+", "(ushort)4"); }
    [Fact] void check0_uint_int_4()       { check0("uint",   127, "uint",   123, "+", "(int)4"   ); }
    [Fact] void check0_uint_uint_4()      { check0("uint",   127, "uint",   123, "+", "(uint)4"  ); }
    [Fact] void check0_uint_long_4()      { check0("long",   127, "uint",   123, "+", "(long)4"  ); }
    [Fact] void check0_uint_ulong_4()     { check0("ulong",  127, "uint",   123, "+", "(ulong)4" ); }
    [Fact] void check0_long__4()          { check0("long",   127, "long",   123, "+", "4"        ); }
    [Fact] void check0_long_byte_4()      { check0("long",   127, "long",   123, "+", "(byte)4"  ); }
    [Fact] void check0_long_sbyte_4()     { check0("long",   127, "long",   123, "+", "(sbyte)4" ); }
    [Fact] void check0_long_short_4()     { check0("long",   127, "long",   123, "+", "(short)4" ); }
    [Fact] void check0_long_ushort_4()    { check0("long",   127, "long",   123, "+", "(ushort)4"); }
    [Fact] void check0_long_int_4()       { check0("long",   127, "long",   123, "+", "(int)4"   ); }
    [Fact] void check0_long_uint_4()      { check0("long",   127, "long",   123, "+", "(uint)4"  ); }
    [Fact] void check0_long_long_4()      { check0("long",   127, "long",   123, "+", "(long)4"  ); }
    [Fact] void check0_long_ulong_4()     { check0_err("long", 123, "+", "(ulong)4"); }
    [Fact] void check0_ulong__4()         { check0("ulong",  127, "ulong",  123, "+", "4"        ); }
    [Fact] void check0_ulong_byte_4()     { check0("ulong",  127, "ulong",  123, "+", "(byte)4"  ); }
    [Fact] void check0_ulong_sbyte_4()    { check0_err("ulong", 123, "+", "(sbyte)4"); }
    [Fact] void check0_ulong_short_4()    { check0_err("ulong", 123, "+", "(short)4"); }
    [Fact] void check0_ulong_ushort_4()   { check0("ulong",  127, "ulong",  123, "+", "(ushort)4"); }
    [Fact] void check0_ulong_int_4()      { check0("ulong",  127, "ulong",  123, "+", "(int)4"   ); }
    [Fact] void check0_ulong_uint_4()     { check0("ulong",  127, "ulong",  123, "+", "(uint)4"  ); }
    [Fact] void check0_ulong_long_4()     { check0("ulong",  127, "ulong",  123, "+", "(long)4"  ); }
    [Fact] void check0_ulong_ulong_4()    { check0("ulong",  127, "ulong",  123, "+", "(ulong)4" ); }
    [Fact] void check0_byte__0()          { check0("int",    123, "byte",   123, "+", "0"        ); }
    [Fact] void check0_byte_byte_0()      { check0("int",    123, "byte",   123, "+", "(byte)0"  ); }
    [Fact] void check0_byte_sbyte_0()     { check0("int",    123, "byte",   123, "+", "(sbyte)0" ); }
    [Fact] void check0_byte_short_0()     { check0("int",    123, "byte",   123, "+", "(short)0" ); }
    [Fact] void check0_byte_ushort_0()    { check0("int",    123, "byte",   123, "+", "(ushort)0"); }
    [Fact] void check0_byte_int_0()       { check0("int",    123, "byte",   123, "+", "(int)0"   ); }
    [Fact] void check0_byte_uint_0()      { check0("uint",   123, "byte",   123, "+", "(uint)0"  ); }
    [Fact] void check0_byte_long_0()      { check0("long",   123, "byte",   123, "+", "(long)0"  ); }
    [Fact] void check0_byte_ulong_0()     { check0("ulong",  123, "byte",   123, "+", "(ulong)0" ); }
    [Fact] void check0_sbyte__0()         { check0("int",    123, "sbyte",  123, "+", "0"        ); }
    [Fact] void check0_sbyte_byte_0()     { check0("int",    123, "sbyte",  123, "+", "(byte)0"  ); }
    [Fact] void check0_sbyte_sbyte_0()    { check0("int",    123, "sbyte",  123, "+", "(sbyte)0" ); }
    [Fact] void check0_sbyte_short_0()    { check0("int",    123, "sbyte",  123, "+", "(short)0" ); }
    [Fact] void check0_sbyte_ushort_0()   { check0("int",    123, "sbyte",  123, "+", "(ushort)0"); }
    [Fact] void check0_sbyte_int_0()      { check0("int",    123, "sbyte",  123, "+", "(int)0"   ); }
    [Fact] void check0_sbyte_uint_0()     { check0("long",   123, "sbyte",  123, "+", "(uint)0"  ); }
    [Fact] void check0_sbyte_long_0()     { check0("long",   123, "sbyte",  123, "+", "(long)0"  ); }
    [Fact] void check0_sbyte_ulong_0()    { check0_err("sbyte", 123, "+", "(ulong)0"); }
    [Fact] void check0_short__0()         { check0("int",    123, "short",  123, "+", "0"        ); }
    [Fact] void check0_short_byte_0()     { check0("int",    123, "short",  123, "+", "(byte)0"  ); }
    [Fact] void check0_short_sbyte_0()    { check0("int",    123, "short",  123, "+", "(sbyte)0" ); }
    [Fact] void check0_short_short_0()    { check0("int",    123, "short",  123, "+", "(short)0" ); }
    [Fact] void check0_short_ushort_0()   { check0("int",    123, "short",  123, "+", "(ushort)0"); }
    [Fact] void check0_short_int_0()      { check0("int",    123, "short",  123, "+", "(int)0"   ); }
    [Fact] void check0_short_uint_0()     { check0("long",   123, "short",  123, "+", "(uint)0"  ); }
    [Fact] void check0_short_long_0()     { check0("long",   123, "short",  123, "+", "(long)0"  ); }
    [Fact] void check0_short_ulong_0()    { check0_err("short", 123, "+", "(ulong)0"); }
    [Fact] void check0_ushort__0()        { check0("int",    123, "ushort", 123, "+", "0"        ); }
    [Fact] void check0_ushort_byte_0()    { check0("int",    123, "ushort", 123, "+", "(byte)0"  ); }
    [Fact] void check0_ushort_sbyte_0()   { check0("int",    123, "ushort", 123, "+", "(sbyte)0" ); }
    [Fact] void check0_ushort_short_0()   { check0("int",    123, "ushort", 123, "+", "(short)0" ); }
    [Fact] void check0_ushort_ushort_0()  { check0("int",    123, "ushort", 123, "+", "(ushort)0"); }
    [Fact] void check0_ushort_int_0()     { check0("int",    123, "ushort", 123, "+", "(int)0"   ); }
    [Fact] void check0_ushort_uint_0()    { check0("uint",   123, "ushort", 123, "+", "(uint)0"  ); }
    [Fact] void check0_ushort_long_0()    { check0("long",   123, "ushort", 123, "+", "(long)0"  ); }
    [Fact] void check0_ushort_ulong_0()   { check0("ulong",  123, "ushort", 123, "+", "(ulong)0" ); }
    [Fact] void check0_int__0()           { check0("int",    123, "int",    123, "+", "0"        ); }
    [Fact] void check0_int_byte_0()       { check0("int",    123, "int",    123, "+", "(byte)0"  ); }
    [Fact] void check0_int_sbyte_0()      { check0("int",    123, "int",    123, "+", "(sbyte)0" ); }
    [Fact] void check0_int_short_0()      { check0("int",    123, "int",    123, "+", "(short)0" ); }
    [Fact] void check0_int_ushort_0()     { check0("int",    123, "int",    123, "+", "(ushort)0"); }
    [Fact] void check0_int_int_0()        { check0("int",    123, "int",    123, "+", "(int)0"   ); }
    [Fact] void check0_int_uint_0()       { check0("long",   123, "int",    123, "+", "(uint)0"  ); }
    [Fact] void check0_int_long_0()       { check0("long",   123, "int",    123, "+", "(long)0"  ); }
    [Fact] void check0_int_ulong_0()      { check0_err("int", 123, "+", "(ulong)0"); }
    [Fact] void check0_uint__0()          { check0("uint",   123, "uint",   123, "+", "0"        ); }
    [Fact] void check0_uint_byte_0()      { check0("uint",   123, "uint",   123, "+", "(byte)0"  ); }
    [Fact] void check0_uint_sbyte_0()     { check0("long",   123, "uint",   123, "+", "(sbyte)0" ); }
    [Fact] void check0_uint_short_0()     { check0("long",   123, "uint",   123, "+", "(short)0" ); }
    [Fact] void check0_uint_ushort_0()    { check0("uint",   123, "uint",   123, "+", "(ushort)0"); }
    [Fact] void check0_uint_int_0()       { check0("uint",   123, "uint",   123, "+", "(int)0"   ); }
    [Fact] void check0_uint_uint_0()      { check0("uint",   123, "uint",   123, "+", "(uint)0"  ); }
    [Fact] void check0_uint_long_0()      { check0("long",   123, "uint",   123, "+", "(long)0"  ); }
    [Fact] void check0_uint_ulong_0()     { check0("ulong",  123, "uint",   123, "+", "(ulong)0" ); }
    [Fact] void check0_long__0()          { check0("long",   123, "long",   123, "+", "0"        ); }
    [Fact] void check0_long_byte_0()      { check0("long",   123, "long",   123, "+", "(byte)0"  ); }
    [Fact] void check0_long_sbyte_0()     { check0("long",   123, "long",   123, "+", "(sbyte)0" ); }
    [Fact] void check0_long_short_0()     { check0("long",   123, "long",   123, "+", "(short)0" ); }
    [Fact] void check0_long_ushort_0()    { check0("long",   123, "long",   123, "+", "(ushort)0"); }
    [Fact] void check0_long_int_0()       { check0("long",   123, "long",   123, "+", "(int)0"   ); }
    [Fact] void check0_long_uint_0()      { check0("long",   123, "long",   123, "+", "(uint)0"  ); }
    [Fact] void check0_long_long_0()      { check0("long",   123, "long",   123, "+", "(long)0"  ); }
    [Fact] void check0_long_ulong_0()     { check0_err("long", 123, "+", "(ulong)0"); }
    [Fact] void check0_ulong__0()         { check0("ulong",  123, "ulong",  123, "+", "0"        ); }
    [Fact] void check0_ulong_byte_0()     { check0("ulong",  123, "ulong",  123, "+", "(byte)0"  ); }
    [Fact] void check0_ulong_sbyte_0()    { check0_err("ulong", 123, "+", "(sbyte)0"); }
    [Fact] void check0_ulong_short_0()    { check0_err("ulong", 123, "+", "(short)0"); }
    [Fact] void check0_ulong_ushort_0()   { check0("ulong",  123, "ulong",  123, "+", "(ushort)0"); }
    [Fact] void check0_ulong_int_0()      { check0("ulong",  123, "ulong",  123, "+", "(int)0"   ); }
    [Fact] void check0_ulong_uint_0()     { check0("ulong",  123, "ulong",  123, "+", "(uint)0"  ); }
    [Fact] void check0_ulong_long_0()     { check0("ulong",  123, "ulong",  123, "+", "(long)0"  ); }
    [Fact] void check0_ulong_ulong_0()    { check0("ulong",  123, "ulong",  123, "+", "(ulong)0" ); }
    [Fact] void check0_byte__m4()         { check0("int",    119, "byte",   123, "+", "-4"       ); }
    [Fact] void check0_byte_byte_m4()     { check0_err("byte", 123, "+", "(byte)-4"); }
    [Fact] void check0_byte_sbyte_m4()    { check0("int",    119, "byte",   123, "+", "(sbyte)-4"); }
    [Fact] void check0_byte_short_m4()    { check0("int",    119, "byte",   123, "+", "(short)-4"); }
    [Fact] void check0_byte_ushort_m4()   { check0_err("byte", 123, "+", "(ushort)-4"); }
    [Fact] void check0_byte_int_m4()      { check0("int",    119, "byte",   123, "+", "(int)-4"  ); }
    [Fact] void check0_byte_uint_m4()     { check0_err("byte", 123, "+", "(uint)-4"); }
    [Fact] void check0_byte_long_m4()     { check0("long",   119, "byte",   123, "+", "(long)-4" ); }
    [Fact] void check0_byte_ulong_m4()    { check0_err("byte", 123, "+", "(ulong)-4"); }
    [Fact] void check0_sbyte__m4()        { check0("int",    119, "sbyte",  123, "+", "-4"       ); }
    [Fact] void check0_sbyte_byte_m4()    { check0_err("sbyte", 123, "+", "(byte)-4"); }
    [Fact] void check0_sbyte_sbyte_m4()   { check0("int",    119, "sbyte",  123, "+", "(sbyte)-4"); }
    [Fact] void check0_sbyte_short_m4()   { check0("int",    119, "sbyte",  123, "+", "(short)-4"); }
    [Fact] void check0_sbyte_ushort_m4()  { check0_err("sbyte", 123, "+", "(ushort)-4"); }
    [Fact] void check0_sbyte_int_m4()     { check0("int",    119, "sbyte",  123, "+", "(int)-4"  ); }
    [Fact] void check0_sbyte_uint_m4()    { check0_err("sbyte", 123, "+", "(uint)-4"); }
    [Fact] void check0_sbyte_long_m4()    { check0("long",   119, "sbyte",  123, "+", "(long)-4" ); }
    [Fact] void check0_sbyte_ulong_m4()   { check0_err("sbyte", 123, "+", "(ulong)-4"); }
    [Fact] void check0_short__m4()        { check0("int",    119, "short",  123, "+", "-4"       ); }
    [Fact] void check0_short_byte_m4()    { check0_err("short", 123, "+", "(byte)-4"); }
    [Fact] void check0_short_sbyte_m4()   { check0("int",    119, "short",  123, "+", "(sbyte)-4"); }
    [Fact] void check0_short_short_m4()   { check0("int",    119, "short",  123, "+", "(short)-4"); }
    [Fact] void check0_short_ushort_m4()  { check0_err("short", 123, "+", "(ushort)-4"); }
    [Fact] void check0_short_int_m4()     { check0("int",    119, "short",  123, "+", "(int)-4"  ); }
    [Fact] void check0_short_uint_m4()    { check0_err("short", 123, "+", "(uint)-4"); }
    [Fact] void check0_short_long_m4()    { check0("long",   119, "short",  123, "+", "(long)-4" ); }
    [Fact] void check0_short_ulong_m4()   { check0_err("short", 123, "+", "(ulong)-4"); }
    [Fact] void check0_ushort__m4()       { check0("int",    119, "ushort", 123, "+", "-4"       ); }
    [Fact] void check0_ushort_byte_m4()   { check0_err("ushort", 123, "+", "(byte)-4"); }
    [Fact] void check0_ushort_sbyte_m4()  { check0("int",    119, "ushort", 123, "+", "(sbyte)-4"); }
    [Fact] void check0_ushort_short_m4()  { check0("int",    119, "ushort", 123, "+", "(short)-4"); }
    [Fact] void check0_ushort_ushort_m4() { check0_err("ushort", 123, "+", "(ushort)-4"); }
    [Fact] void check0_ushort_int_m4()    { check0("int",    119, "ushort", 123, "+", "(int)-4"  ); }
    [Fact] void check0_ushort_uint_m4()   { check0_err("ushort", 123, "+", "(uint)-4"); }
    [Fact] void check0_ushort_long_m4()   { check0("long",   119, "ushort", 123, "+", "(long)-4" ); }
    [Fact] void check0_ushort_ulong_m4()  { check0_err("ushort", 123, "+", "(ulong)-4"); }
    [Fact] void check0_int__m4()          { check0("int",    119, "int",    123, "+", "-4"       ); }
    [Fact] void check0_int_byte_m4()      { check0_err("int", 123, "+", "(byte)-4"); }
    [Fact] void check0_int_sbyte_m4()     { check0("int",    119, "int",    123, "+", "(sbyte)-4"); }
    [Fact] void check0_int_short_m4()     { check0("int",    119, "int",    123, "+", "(short)-4"); }
    [Fact] void check0_int_ushort_m4()    { check0_err("int", 123, "+", "(ushort)-4"); }
    [Fact] void check0_int_int_m4()       { check0("int",    119, "int",    123, "+", "(int)-4"  ); }
    [Fact] void check0_int_uint_m4()      { check0_err("int", 123, "+", "(uint)-4"); }
    [Fact] void check0_int_long_m4()      { check0("long",   119, "int",    123, "+", "(long)-4" ); }
    [Fact] void check0_int_ulong_m4()     { check0_err("int", 123, "+", "(ulong)-4"); }
    [Fact] void check0_uint__m4()         { check0("long",   119, "uint",   123, "+", "-4"       ); }
    [Fact] void check0_uint_byte_m4()     { check0_err("uint", 123, "+", "(byte)-4"); }
    [Fact] void check0_uint_sbyte_m4()    { check0("long",   119, "uint",   123, "+", "(sbyte)-4"); }
    [Fact] void check0_uint_short_m4()    { check0("long",   119, "uint",   123, "+", "(short)-4"); }
    [Fact] void check0_uint_ushort_m4()   { check0_err("uint", 123, "+", "(ushort)-4"); }
    [Fact] void check0_uint_int_m4()      { check0("long",   119, "uint",   123, "+", "(int)-4"  ); }
    [Fact] void check0_uint_uint_m4()     { check0_err("uint", 123, "+", "(uint)-4"); }
    [Fact] void check0_uint_long_m4()     { check0("long",   119, "uint",   123, "+", "(long)-4" ); }
    [Fact] void check0_uint_ulong_m4()    { check0_err("uint", 123, "+", "(ulong)-4"); }
    [Fact] void check0_long__m4()         { check0("long",   119, "long",   123, "+", "-4"       ); }
    [Fact] void check0_long_byte_m4()     { check0_err("long", 123, "+", "(byte)-4"); }
    [Fact] void check0_long_sbyte_m4()    { check0("long",   119, "long",   123, "+", "(sbyte)-4"); }
    [Fact] void check0_long_short_m4()    { check0("long",   119, "long",   123, "+", "(short)-4"); }
    [Fact] void check0_long_ushort_m4()   { check0_err("long", 123, "+", "(ushort)-4"); }
    [Fact] void check0_long_int_m4()      { check0("long",   119, "long",   123, "+", "(int)-4"  ); }
    [Fact] void check0_long_uint_m4()     { check0_err("long", 123, "+", "(uint)-4"); }
    [Fact] void check0_long_long_m4()     { check0("long",   119, "long",   123, "+", "(long)-4" ); }
    [Fact] void check0_long_ulong_m4()    { check0_err("long", 123, "+", "(ulong)-4"); }
    [Fact] void check0_ulong__m4()        { check0_err("ulong", 123, "+", "-4"); }
    [Fact] void check0_ulong_byte_m4()    { check0_err("ulong", 123, "+", "(byte)-4"); }
    [Fact] void check0_ulong_sbyte_m4()   { check0_err("ulong", 123, "+", "(sbyte)-4"); }
    [Fact] void check0_ulong_short_m4()   { check0_err("ulong", 123, "+", "(short)-4"); }
    [Fact] void check0_ulong_ushort_m4()  { check0_err("ulong", 123, "+", "(ushort)-4"); }
    [Fact] void check0_ulong_int_m4()     { check0_err("ulong", 123, "+", "(int)-4"); }
    [Fact] void check0_ulong_uint_m4()    { check0_err("ulong", 123, "+", "(uint)-4"); }
    [Fact] void check0_ulong_long_m4()    { check0_err("ulong", 123, "+", "(long)-4"); }
    [Fact] void check0_ulong_ulong_m4()   { check0_err("ulong", 123, "+", "(ulong)-4"); }
    [Fact] void check1_byte__4()          { check1("int",    127, "byte",   123, "+", "4"        ); }
    [Fact] void check1_byte_byte_4()      { check1("int",    127, "byte",   123, "+", "(byte)4"  ); }
    [Fact] void check1_byte_sbyte_4()     { check1("int",    127, "byte",   123, "+", "(sbyte)4" ); }
    [Fact] void check1_byte_short_4()     { check1("int",    127, "byte",   123, "+", "(short)4" ); }
    [Fact] void check1_byte_ushort_4()    { check1("int",    127, "byte",   123, "+", "(ushort)4"); }
    [Fact] void check1_byte_int_4()       { check1("int",    127, "byte",   123, "+", "(int)4"   ); }
    [Fact] void check1_byte_uint_4()      { check1("uint",   127, "byte",   123, "+", "(uint)4"  ); }
    [Fact] void check1_byte_long_4()      { check1("long",   127, "byte",   123, "+", "(long)4"  ); }
    [Fact] void check1_byte_ulong_4()     { check1("ulong",  127, "byte",   123, "+", "(ulong)4" ); }
    [Fact] void check1_sbyte__4()         { check1("int",    127, "sbyte",  123, "+", "4"        ); }
    [Fact] void check1_sbyte_byte_4()     { check1("int",    127, "sbyte",  123, "+", "(byte)4"  ); }
    [Fact] void check1_sbyte_sbyte_4()    { check1("int",    127, "sbyte",  123, "+", "(sbyte)4" ); }
    [Fact] void check1_sbyte_short_4()    { check1("int",    127, "sbyte",  123, "+", "(short)4" ); }
    [Fact] void check1_sbyte_ushort_4()   { check1("int",    127, "sbyte",  123, "+", "(ushort)4"); }
    [Fact] void check1_sbyte_int_4()      { check1("int",    127, "sbyte",  123, "+", "(int)4"   ); }
    [Fact] void check1_sbyte_uint_4()     { check1("long",   127, "sbyte",  123, "+", "(uint)4"  ); }
    [Fact] void check1_sbyte_long_4()     { check1("long",   127, "sbyte",  123, "+", "(long)4"  ); }
    [Fact] void check1_sbyte_ulong_4()    { check1_err("sbyte", 123, "+", "(ulong)4"); }
    [Fact] void check1_short__4()         { check1("int",    127, "short",  123, "+", "4"        ); }
    [Fact] void check1_short_byte_4()     { check1("int",    127, "short",  123, "+", "(byte)4"  ); }
    [Fact] void check1_short_sbyte_4()    { check1("int",    127, "short",  123, "+", "(sbyte)4" ); }
    [Fact] void check1_short_short_4()    { check1("int",    127, "short",  123, "+", "(short)4" ); }
    [Fact] void check1_short_ushort_4()   { check1("int",    127, "short",  123, "+", "(ushort)4"); }
    [Fact] void check1_short_int_4()      { check1("int",    127, "short",  123, "+", "(int)4"   ); }
    [Fact] void check1_short_uint_4()     { check1("long",   127, "short",  123, "+", "(uint)4"  ); }
    [Fact] void check1_short_long_4()     { check1("long",   127, "short",  123, "+", "(long)4"  ); }
    [Fact] void check1_short_ulong_4()    { check1_err("short", 123, "+", "(ulong)4"); }
    [Fact] void check1_ushort__4()        { check1("int",    127, "ushort", 123, "+", "4"        ); }
    [Fact] void check1_ushort_byte_4()    { check1("int",    127, "ushort", 123, "+", "(byte)4"  ); }
    [Fact] void check1_ushort_sbyte_4()   { check1("int",    127, "ushort", 123, "+", "(sbyte)4" ); }
    [Fact] void check1_ushort_short_4()   { check1("int",    127, "ushort", 123, "+", "(short)4" ); }
    [Fact] void check1_ushort_ushort_4()  { check1("int",    127, "ushort", 123, "+", "(ushort)4"); }
    [Fact] void check1_ushort_int_4()     { check1("int",    127, "ushort", 123, "+", "(int)4"   ); }
    [Fact] void check1_ushort_uint_4()    { check1("uint",   127, "ushort", 123, "+", "(uint)4"  ); }
    [Fact] void check1_ushort_long_4()    { check1("long",   127, "ushort", 123, "+", "(long)4"  ); }
    [Fact] void check1_ushort_ulong_4()   { check1("ulong",  127, "ushort", 123, "+", "(ulong)4" ); }
    [Fact] void check1_int__4()           { check1("int",    127, "int",    123, "+", "4"        ); }
    [Fact] void check1_int_byte_4()       { check1("int",    127, "int",    123, "+", "(byte)4"  ); }
    [Fact] void check1_int_sbyte_4()      { check1("int",    127, "int",    123, "+", "(sbyte)4" ); }
    [Fact] void check1_int_short_4()      { check1("int",    127, "int",    123, "+", "(short)4" ); }
    [Fact] void check1_int_ushort_4()     { check1("int",    127, "int",    123, "+", "(ushort)4"); }
    [Fact] void check1_int_int_4()        { check1("int",    127, "int",    123, "+", "(int)4"   ); }
    [Fact] void check1_int_uint_4()       { check1("long",   127, "int",    123, "+", "(uint)4"  ); }
    [Fact] void check1_int_long_4()       { check1("long",   127, "int",    123, "+", "(long)4"  ); }
    [Fact] void check1_int_ulong_4()      { check1_err("int", 123, "+", "(ulong)4"); }
    [Fact] void check1_uint__4()          { check1("uint",   127, "uint",   123, "+", "4"        ); }
    [Fact] void check1_uint_byte_4()      { check1("uint",   127, "uint",   123, "+", "(byte)4"  ); }
    [Fact] void check1_uint_sbyte_4()     { check1("long",   127, "uint",   123, "+", "(sbyte)4" ); }
    [Fact] void check1_uint_short_4()     { check1("long",   127, "uint",   123, "+", "(short)4" ); }
    [Fact] void check1_uint_ushort_4()    { check1("uint",   127, "uint",   123, "+", "(ushort)4"); }
    [Fact] void check1_uint_int_4()       { check1("uint",   127, "uint",   123, "+", "(int)4"   ); }
    [Fact] void check1_uint_uint_4()      { check1("uint",   127, "uint",   123, "+", "(uint)4"  ); }
    [Fact] void check1_uint_long_4()      { check1("long",   127, "uint",   123, "+", "(long)4"  ); }
    [Fact] void check1_uint_ulong_4()     { check1("ulong",  127, "uint",   123, "+", "(ulong)4" ); }
    [Fact] void check1_long__4()          { check1("long",   127, "long",   123, "+", "4"        ); }
    [Fact] void check1_long_byte_4()      { check1("long",   127, "long",   123, "+", "(byte)4"  ); }
    [Fact] void check1_long_sbyte_4()     { check1("long",   127, "long",   123, "+", "(sbyte)4" ); }
    [Fact] void check1_long_short_4()     { check1("long",   127, "long",   123, "+", "(short)4" ); }
    [Fact] void check1_long_ushort_4()    { check1("long",   127, "long",   123, "+", "(ushort)4"); }
    [Fact] void check1_long_int_4()       { check1("long",   127, "long",   123, "+", "(int)4"   ); }
    [Fact] void check1_long_uint_4()      { check1("long",   127, "long",   123, "+", "(uint)4"  ); }
    [Fact] void check1_long_long_4()      { check1("long",   127, "long",   123, "+", "(long)4"  ); }
    [Fact] void check1_long_ulong_4()     { check1_err("long", 123, "+", "(ulong)4"); }
    [Fact] void check1_ulong__4()         { check1("ulong",  127, "ulong",  123, "+", "4"        ); }
    [Fact] void check1_ulong_byte_4()     { check1("ulong",  127, "ulong",  123, "+", "(byte)4"  ); }
    [Fact] void check1_ulong_sbyte_4()    { check1_err("ulong", 123, "+", "(sbyte)4"); }
    [Fact] void check1_ulong_short_4()    { check1_err("ulong", 123, "+", "(short)4"); }
    [Fact] void check1_ulong_ushort_4()   { check1("ulong",  127, "ulong",  123, "+", "(ushort)4"); }
    [Fact] void check1_ulong_int_4()      { check1("ulong",  127, "ulong",  123, "+", "(int)4"   ); }
    [Fact] void check1_ulong_uint_4()     { check1("ulong",  127, "ulong",  123, "+", "(uint)4"  ); }
    [Fact] void check1_ulong_long_4()     { check1("ulong",  127, "ulong",  123, "+", "(long)4"  ); }
    [Fact] void check1_ulong_ulong_4()    { check1("ulong",  127, "ulong",  123, "+", "(ulong)4" ); }
    [Fact] void check1_byte__0()          { check1("int",    123, "byte",   123, "+", "0"        ); }
    [Fact] void check1_byte_byte_0()      { check1("int",    123, "byte",   123, "+", "(byte)0"  ); }
    [Fact] void check1_byte_sbyte_0()     { check1("int",    123, "byte",   123, "+", "(sbyte)0" ); }
    [Fact] void check1_byte_short_0()     { check1("int",    123, "byte",   123, "+", "(short)0" ); }
    [Fact] void check1_byte_ushort_0()    { check1("int",    123, "byte",   123, "+", "(ushort)0"); }
    [Fact] void check1_byte_int_0()       { check1("int",    123, "byte",   123, "+", "(int)0"   ); }
    [Fact] void check1_byte_uint_0()      { check1("uint",   123, "byte",   123, "+", "(uint)0"  ); }
    [Fact] void check1_byte_long_0()      { check1("long",   123, "byte",   123, "+", "(long)0"  ); }
    [Fact] void check1_byte_ulong_0()     { check1("ulong",  123, "byte",   123, "+", "(ulong)0" ); }
    [Fact] void check1_sbyte__0()         { check1("int",    123, "sbyte",  123, "+", "0"        ); }
    [Fact] void check1_sbyte_byte_0()     { check1("int",    123, "sbyte",  123, "+", "(byte)0"  ); }
    [Fact] void check1_sbyte_sbyte_0()    { check1("int",    123, "sbyte",  123, "+", "(sbyte)0" ); }
    [Fact] void check1_sbyte_short_0()    { check1("int",    123, "sbyte",  123, "+", "(short)0" ); }
    [Fact] void check1_sbyte_ushort_0()   { check1("int",    123, "sbyte",  123, "+", "(ushort)0"); }
    [Fact] void check1_sbyte_int_0()      { check1("int",    123, "sbyte",  123, "+", "(int)0"   ); }
    [Fact] void check1_sbyte_uint_0()     { check1("long",   123, "sbyte",  123, "+", "(uint)0"  ); }
    [Fact] void check1_sbyte_long_0()     { check1("long",   123, "sbyte",  123, "+", "(long)0"  ); }
    [Fact] void check1_sbyte_ulong_0()    { check1_err("sbyte", 123, "+", "(ulong)0"); }
    [Fact] void check1_short__0()         { check1("int",    123, "short",  123, "+", "0"        ); }
    [Fact] void check1_short_byte_0()     { check1("int",    123, "short",  123, "+", "(byte)0"  ); }
    [Fact] void check1_short_sbyte_0()    { check1("int",    123, "short",  123, "+", "(sbyte)0" ); }
    [Fact] void check1_short_short_0()    { check1("int",    123, "short",  123, "+", "(short)0" ); }
    [Fact] void check1_short_ushort_0()   { check1("int",    123, "short",  123, "+", "(ushort)0"); }
    [Fact] void check1_short_int_0()      { check1("int",    123, "short",  123, "+", "(int)0"   ); }
    [Fact] void check1_short_uint_0()     { check1("long",   123, "short",  123, "+", "(uint)0"  ); }
    [Fact] void check1_short_long_0()     { check1("long",   123, "short",  123, "+", "(long)0"  ); }
    [Fact] void check1_short_ulong_0()    { check1_err("short", 123, "+", "(ulong)0"); }
    [Fact] void check1_ushort__0()        { check1("int",    123, "ushort", 123, "+", "0"        ); }
    [Fact] void check1_ushort_byte_0()    { check1("int",    123, "ushort", 123, "+", "(byte)0"  ); }
    [Fact] void check1_ushort_sbyte_0()   { check1("int",    123, "ushort", 123, "+", "(sbyte)0" ); }
    [Fact] void check1_ushort_short_0()   { check1("int",    123, "ushort", 123, "+", "(short)0" ); }
    [Fact] void check1_ushort_ushort_0()  { check1("int",    123, "ushort", 123, "+", "(ushort)0"); }
    [Fact] void check1_ushort_int_0()     { check1("int",    123, "ushort", 123, "+", "(int)0"   ); }
    [Fact] void check1_ushort_uint_0()    { check1("uint",   123, "ushort", 123, "+", "(uint)0"  ); }
    [Fact] void check1_ushort_long_0()    { check1("long",   123, "ushort", 123, "+", "(long)0"  ); }
    [Fact] void check1_ushort_ulong_0()   { check1("ulong",  123, "ushort", 123, "+", "(ulong)0" ); }
    [Fact] void check1_int__0()           { check1("int",    123, "int",    123, "+", "0"        ); }
    [Fact] void check1_int_byte_0()       { check1("int",    123, "int",    123, "+", "(byte)0"  ); }
    [Fact] void check1_int_sbyte_0()      { check1("int",    123, "int",    123, "+", "(sbyte)0" ); }
    [Fact] void check1_int_short_0()      { check1("int",    123, "int",    123, "+", "(short)0" ); }
    [Fact] void check1_int_ushort_0()     { check1("int",    123, "int",    123, "+", "(ushort)0"); }
    [Fact] void check1_int_int_0()        { check1("int",    123, "int",    123, "+", "(int)0"   ); }
    [Fact] void check1_int_uint_0()       { check1("long",   123, "int",    123, "+", "(uint)0"  ); }
    [Fact] void check1_int_long_0()       { check1("long",   123, "int",    123, "+", "(long)0"  ); }
    [Fact] void check1_int_ulong_0()      { check1_err("int", 123, "+", "(ulong)0"); }
    [Fact] void check1_uint__0()          { check1("uint",   123, "uint",   123, "+", "0"        ); }
    [Fact] void check1_uint_byte_0()      { check1("uint",   123, "uint",   123, "+", "(byte)0"  ); }
    [Fact] void check1_uint_sbyte_0()     { check1("long",   123, "uint",   123, "+", "(sbyte)0" ); }
    [Fact] void check1_uint_short_0()     { check1("long",   123, "uint",   123, "+", "(short)0" ); }
    [Fact] void check1_uint_ushort_0()    { check1("uint",   123, "uint",   123, "+", "(ushort)0"); }
    [Fact] void check1_uint_int_0()       { check1("uint",   123, "uint",   123, "+", "(int)0"   ); }
    [Fact] void check1_uint_uint_0()      { check1("uint",   123, "uint",   123, "+", "(uint)0"  ); }
    [Fact] void check1_uint_long_0()      { check1("long",   123, "uint",   123, "+", "(long)0"  ); }
    [Fact] void check1_uint_ulong_0()     { check1("ulong",  123, "uint",   123, "+", "(ulong)0" ); }
    [Fact] void check1_long__0()          { check1("long",   123, "long",   123, "+", "0"        ); }
    [Fact] void check1_long_byte_0()      { check1("long",   123, "long",   123, "+", "(byte)0"  ); }
    [Fact] void check1_long_sbyte_0()     { check1("long",   123, "long",   123, "+", "(sbyte)0" ); }
    [Fact] void check1_long_short_0()     { check1("long",   123, "long",   123, "+", "(short)0" ); }
    [Fact] void check1_long_ushort_0()    { check1("long",   123, "long",   123, "+", "(ushort)0"); }
    [Fact] void check1_long_int_0()       { check1("long",   123, "long",   123, "+", "(int)0"   ); }
    [Fact] void check1_long_uint_0()      { check1("long",   123, "long",   123, "+", "(uint)0"  ); }
    [Fact] void check1_long_long_0()      { check1("long",   123, "long",   123, "+", "(long)0"  ); }
    [Fact] void check1_long_ulong_0()     { check1_err("long", 123, "+", "(ulong)0"); }
    [Fact] void check1_ulong__0()         { check1("ulong",  123, "ulong",  123, "+", "0"        ); }
    [Fact] void check1_ulong_byte_0()     { check1("ulong",  123, "ulong",  123, "+", "(byte)0"  ); }
    [Fact] void check1_ulong_sbyte_0()    { check1_err("ulong", 123, "+", "(sbyte)0"); }
    [Fact] void check1_ulong_short_0()    { check1_err("ulong", 123, "+", "(short)0"); }
    [Fact] void check1_ulong_ushort_0()   { check1("ulong",  123, "ulong",  123, "+", "(ushort)0"); }
    [Fact] void check1_ulong_int_0()      { check1("ulong",  123, "ulong",  123, "+", "(int)0"   ); }
    [Fact] void check1_ulong_uint_0()     { check1("ulong",  123, "ulong",  123, "+", "(uint)0"  ); }
    [Fact] void check1_ulong_long_0()     { check1("ulong",  123, "ulong",  123, "+", "(long)0"  ); }
    [Fact] void check1_ulong_ulong_0()    { check1("ulong",  123, "ulong",  123, "+", "(ulong)0" ); }
    [Fact] void check1_byte__m4()         { check1("int",    119, "byte",   123, "+", "-4"       ); }
    [Fact] void check1_byte_byte_m4()     { check1_err("byte", 123, "+", "(byte)-4"); }
    [Fact] void check1_byte_sbyte_m4()    { check1("int",    119, "byte",   123, "+", "(sbyte)-4"); }
    [Fact] void check1_byte_short_m4()    { check1("int",    119, "byte",   123, "+", "(short)-4"); }
    [Fact] void check1_byte_ushort_m4()   { check1_err("byte", 123, "+", "(ushort)-4"); }
    [Fact] void check1_byte_int_m4()      { check1("int",    119, "byte",   123, "+", "(int)-4"  ); }
    [Fact] void check1_byte_uint_m4()     { check1_err("byte", 123, "+", "(uint)-4"); }
    [Fact] void check1_byte_long_m4()     { check1("long",   119, "byte",   123, "+", "(long)-4" ); }
    [Fact] void check1_byte_ulong_m4()    { check1_err("byte", 123, "+", "(ulong)-4"); }
    [Fact] void check1_sbyte__m4()        { check1("int",    119, "sbyte",  123, "+", "-4"       ); }
    [Fact] void check1_sbyte_byte_m4()    { check1_err("sbyte", 123, "+", "(byte)-4"); }
    [Fact] void check1_sbyte_sbyte_m4()   { check1("int",    119, "sbyte",  123, "+", "(sbyte)-4"); }
    [Fact] void check1_sbyte_short_m4()   { check1("int",    119, "sbyte",  123, "+", "(short)-4"); }
    [Fact] void check1_sbyte_ushort_m4()  { check1_err("sbyte", 123, "+", "(ushort)-4"); }
    [Fact] void check1_sbyte_int_m4()     { check1("int",    119, "sbyte",  123, "+", "(int)-4"  ); }
    [Fact] void check1_sbyte_uint_m4()    { check1_err("sbyte", 123, "+", "(uint)-4"); }
    [Fact] void check1_sbyte_long_m4()    { check1("long",   119, "sbyte",  123, "+", "(long)-4" ); }
    [Fact] void check1_sbyte_ulong_m4()   { check1_err("sbyte", 123, "+", "(ulong)-4"); }
    [Fact] void check1_short__m4()        { check1("int",    119, "short",  123, "+", "-4"       ); }
    [Fact] void check1_short_byte_m4()    { check1_err("short", 123, "+", "(byte)-4"); }
    [Fact] void check1_short_sbyte_m4()   { check1("int",    119, "short",  123, "+", "(sbyte)-4"); }
    [Fact] void check1_short_short_m4()   { check1("int",    119, "short",  123, "+", "(short)-4"); }
    [Fact] void check1_short_ushort_m4()  { check1_err("short", 123, "+", "(ushort)-4"); }
    [Fact] void check1_short_int_m4()     { check1("int",    119, "short",  123, "+", "(int)-4"  ); }
    [Fact] void check1_short_uint_m4()    { check1_err("short", 123, "+", "(uint)-4"); }
    [Fact] void check1_short_long_m4()    { check1("long",   119, "short",  123, "+", "(long)-4" ); }
    [Fact] void check1_short_ulong_m4()   { check1_err("short", 123, "+", "(ulong)-4"); }
    [Fact] void check1_ushort__m4()       { check1("int",    119, "ushort", 123, "+", "-4"       ); }
    [Fact] void check1_ushort_byte_m4()   { check1_err("ushort", 123, "+", "(byte)-4"); }
    [Fact] void check1_ushort_sbyte_m4()  { check1("int",    119, "ushort", 123, "+", "(sbyte)-4"); }
    [Fact] void check1_ushort_short_m4()  { check1("int",    119, "ushort", 123, "+", "(short)-4"); }
    [Fact] void check1_ushort_ushort_m4() { check1_err("ushort", 123, "+", "(ushort)-4"); }
    [Fact] void check1_ushort_int_m4()    { check1("int",    119, "ushort", 123, "+", "(int)-4"  ); }
    [Fact] void check1_ushort_uint_m4()   { check1_err("ushort", 123, "+", "(uint)-4"); }
    [Fact] void check1_ushort_long_m4()   { check1("long",   119, "ushort", 123, "+", "(long)-4" ); }
    [Fact] void check1_ushort_ulong_m4()  { check1_err("ushort", 123, "+", "(ulong)-4"); }
    [Fact] void check1_int__m4()          { check1("int",    119, "int",    123, "+", "-4"       ); }
    [Fact] void check1_int_byte_m4()      { check1_err("int", 123, "+", "(byte)-4"); }
    [Fact] void check1_int_sbyte_m4()     { check1("int",    119, "int",    123, "+", "(sbyte)-4"); }
    [Fact] void check1_int_short_m4()     { check1("int",    119, "int",    123, "+", "(short)-4"); }
    [Fact] void check1_int_ushort_m4()    { check1_err("int", 123, "+", "(ushort)-4"); }
    [Fact] void check1_int_int_m4()       { check1("int",    119, "int",    123, "+", "(int)-4"  ); }
    [Fact] void check1_int_uint_m4()      { check1_err("int", 123, "+", "(uint)-4"); }
    [Fact] void check1_int_long_m4()      { check1("long",   119, "int",    123, "+", "(long)-4" ); }
    [Fact] void check1_int_ulong_m4()     { check1_err("int", 123, "+", "(ulong)-4"); }
    [Fact] void check1_uint__m4()         { check1("long",   119, "uint",   123, "+", "-4"       ); }
    [Fact] void check1_uint_byte_m4()     { check1_err("uint", 123, "+", "(byte)-4"); }
    [Fact] void check1_uint_sbyte_m4()    { check1("long",   119, "uint",   123, "+", "(sbyte)-4"); }
    [Fact] void check1_uint_short_m4()    { check1("long",   119, "uint",   123, "+", "(short)-4"); }
    [Fact] void check1_uint_ushort_m4()   { check1_err("uint", 123, "+", "(ushort)-4"); }
    [Fact] void check1_uint_int_m4()      { check1("long",   119, "uint",   123, "+", "(int)-4"  ); }
    [Fact] void check1_uint_uint_m4()     { check1_err("uint", 123, "+", "(uint)-4"); }
    [Fact] void check1_uint_long_m4()     { check1("long",   119, "uint",   123, "+", "(long)-4" ); }
    [Fact] void check1_uint_ulong_m4()    { check1_err("uint", 123, "+", "(ulong)-4"); }
    [Fact] void check1_long__m4()         { check1("long",   119, "long",   123, "+", "-4"       ); }
    [Fact] void check1_long_byte_m4()     { check1_err("long", 123, "+", "(byte)-4"); }
    [Fact] void check1_long_sbyte_m4()    { check1("long",   119, "long",   123, "+", "(sbyte)-4"); }
    [Fact] void check1_long_short_m4()    { check1("long",   119, "long",   123, "+", "(short)-4"); }
    [Fact] void check1_long_ushort_m4()   { check1_err("long", 123, "+", "(ushort)-4"); }
    [Fact] void check1_long_int_m4()      { check1("long",   119, "long",   123, "+", "(int)-4"  ); }
    [Fact] void check1_long_uint_m4()     { check1_err("long", 123, "+", "(uint)-4"); }
    [Fact] void check1_long_long_m4()     { check1("long",   119, "long",   123, "+", "(long)-4" ); }
    [Fact] void check1_long_ulong_m4()    { check1_err("long", 123, "+", "(ulong)-4"); }
    [Fact] void check1_ulong__m4()        { check1_err("ulong", 123, "+", "-4"); }
    [Fact] void check1_ulong_byte_m4()    { check1_err("ulong", 123, "+", "(byte)-4"); }
    [Fact] void check1_ulong_sbyte_m4()   { check1_err("ulong", 123, "+", "(sbyte)-4"); }
    [Fact] void check1_ulong_short_m4()   { check1_err("ulong", 123, "+", "(short)-4"); }
    [Fact] void check1_ulong_ushort_m4()  { check1_err("ulong", 123, "+", "(ushort)-4"); }
    [Fact] void check1_ulong_int_m4()     { check1_err("ulong", 123, "+", "(int)-4"); }
    [Fact] void check1_ulong_uint_m4()    { check1_err("ulong", 123, "+", "(uint)-4"); }
    [Fact] void check1_ulong_long_m4()    { check1_err("ulong", 123, "+", "(long)-4"); }
    [Fact] void check1_ulong_ulong_m4()   { check1_err("ulong", 123, "+", "(ulong)-4"); }
}
#pragma warning restore format
