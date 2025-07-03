partial class C1 {
    public const int Z = 2+2;
}

partial class C1 {
    public readonly static int X = 1; // can still be modified via reflection
    public const int Y = 2;

    void fun() {
        if (X == 1)
        {
            maybe_called();
        }

        if (Y != 2)
        {
            never_called();
        }

        if (Z == 4)
        {
            always_called();
        }
    }
}

class C2 {
    void fun2()
    {
        if (C1.Z == 4)
        {
            always_called();
        }
    }
}
