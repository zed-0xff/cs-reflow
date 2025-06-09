void keep_a1()
{
    int b;
    int a = b = 111;
    foo(a);
}

void keep_a2()
{
    int a;
    int b;
    a = b = 111;
    foo(a);
}

void keep_a3()
{
    int a, b;
    a = b = 111;
    foo(a);
}

void keep_a4()
{
    int a, b;
    a = 111;
    b = 222;
    foo(a);
}

void keep_a5()
{
    int a, b;
    a = 111;
    b = a;
    foo(a);
}

void keep_b1()
{
    int b;
    int a = b = 111;
    foo(b);
}

void keep_b2()
{
    int a;
    int b;
    a = b = 111;
    foo(b);
}

void keep_b3()
{
    int a, b;
    a = b = 111;
    foo(b);
}

void keep_333()
{
    int b;
    foo(b = 333);
    b = 444;
}
