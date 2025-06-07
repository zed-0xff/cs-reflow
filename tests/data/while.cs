void eliminate()
{
    while (false)
        never_called();
    always_called();
}

void keep_1()
{
    while (true)
        foo();
}

void keep_block()
{
    while (true)
    {
        foo();
    }
}

void keep_call1()
{
    int i = 0;
    while (true)
    {
        if (foo(i++))
            break;
    }
}

void keep_call2()
{
    int i = 0;
    while (true)
    {
        kc1();
        if (foo(i++))
            break;
    }
}

void keep_call3()
{
    int i = 0;
    while (true)
    {
        if (foo(i++))
            break;
        kc2();
    }
}

void keep_call4()
{
    int i = 0;
    while (true)
    {
        kc1();
        if (foo(i++))
            break;
        kc2();
    }
}

void reflow()
{
    int step = 0;
    while (true)
    {
        switch (step++)
        {
            case 0:
                foo();
                break;
            case 1:
                bar();
                break;
            case 2:
                return;
            case 3:
                never_called1();
                break;
            default:
                never_called2();
                break;
        }
    }
}
