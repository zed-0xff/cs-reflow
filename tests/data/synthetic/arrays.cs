void case1()
{
    var a = new int[] { 1, 2, 3 };
    if (a.Length == 3)
    {
        called1();
    }

    if (a[1] == 2)
    {
        called2();
    }

    a[1] = 123;
    if (a[1] == 123)
    {
        called3();
    }
}

void case2()
{
    var a = new int[3];
    if (a.Length == 3)
    {
        called1();
    }

    if (a[1] == 0)
    {
        called2();
    }

    a[1] = 123;
    if (a[1] == 123)
    {
        called3();
    }
}
