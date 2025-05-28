void return_from_try()
{
    try
    {
        moo1();
        return;
        moo2();
    }
    catch
    {
        bar();
    }

    baz();
}

void return_from_catch()
{
    try
    {
        moo();
    }
    catch (Ex1)
    {
        bar1();
        return;
        bar2();
    }
    catch (Ex2)
    {
        asdf();
    }

    baz();
}

void break_from_try()
{
    while(true)
    {
        try
        {
            moo1();
            break;
            moo2();
        }
        catch (Ex1)
        {
            bar();
        }
    }
}

void continue_from_try()
{
    while(true)
    {
        try
        {
            moo1();
            continue;
            moo2();
        }
        catch (Ex1)
        {
            bar();
        }
    }
}

void goto_from_try()
{
    while(true)
    {
        try
        {
            moo1();
            goto l2;
            moo2();
        }
        catch (Ex1)
        {
            bar();
        }
    }
    return;

    l2:
        baz();
}

void goto_from_catch()
{
    while(true)
    {
        try
        {
            moo();
        }
        catch (Ex1)
        {
            goto l2;
        }
    }
    return;

    l2:
        baz();
}

void set_outer_var() {
    int x = 1;

    try
    {
        x = 2;
        moo();
    }
    catch
    {
    }

    if (x == 2)
        foo();
}

void _1_or_2() {
    int x = 1;

    try
    {
        moo();
    }
    catch
    {
        x = 2;
    }

    if (x == 2)
        foo();
}

void _2_or_3() {
    int x = 1;

    try
    {
        x = 2;
        moo();
    }
    catch
    {
        x = 3;
    }

    if (x == 2)
        foo();

    if (x == 3)
        bar();

    if (x == 4)
        never_called();
}

void _finally() {
    int x = 1;

    try
    {
        x = 2;
        moo();
    }
    catch
    {
        x = 3;
    }
    finally
    {
        x = 4;
    }

    if (x == 2)
        foo();

    if (x == 3)
        bar();

    if (x == 4)
        always_called();
}
