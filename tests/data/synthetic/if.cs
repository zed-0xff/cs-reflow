void as_is()
{
    if (a)
    {
        foo();
    }
    else
    {
        bar();
    }
}

void keep_true()
{
    var a = true;
    if (a)
    {
        foo();
    }
    else
    {
        bar();
    }
}

void keep_false()
{
    var a = false;
    if (a)
    {
        foo();
    }
    else
    {
        bar();
    }
}

void as_is_indirect()
{
    bool b;
    if (a)
    {
        b = true;
    }
    else
    {
        b = false;
    }

    if (b)
    {
        foo();
    }
    else
    {
        bar();
    }
}

void keep_true_indirect()
{
    var a = true;
    var b;
    
    if (a)
    {
        b = true;
    }
    else
    {
        b = false;
    }

    if (b)
    {
        foo();
    }
    else
    {
        bar();
    }
}

void keep_false_indirect()
{
    var a = false;
    var b;

    if (a)
    {
        b = true;
    }
    else
    {
        b = false;
    }

    if (b)
    {
        foo();
    }
    else
    {
        bar();
    }
}
