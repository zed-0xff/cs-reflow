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
            bft1();
            break;
            never_called();
        }
        catch (Ex1)
        {
            bar();
        }
    }

    baz();
}

void continue_from_try()
{
    while(true)
    {
        try
        {
            cft1();
            continue;
            never_called();
        }
        catch (Ex1)
        {
            bar();
        }
    }

    baz();
}

void goto_from_try()
{
    while(true)
    {
        try
        {
            gft1();
            goto l2;
            never_called();
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

void goto_from_catch1()
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
            never_called();
        }
    }
    return;

    l2:
        baz();
}

void goto_from_catch2()
{
    while(true)
    {
        bbb();
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

void goto_from_catch3()
{
    while(true)
    {
        bbb();
        try
        {
            moo();
        }
        catch (Ex1)
        {
            goto l2;
            never_called();
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
