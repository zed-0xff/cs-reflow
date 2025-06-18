void case1()
{
    init();
    do
    {
        if (y)
        {
            foo();
        }
        else
        {
            bar();
        }
    } while (x);
}

void case2()
{
    bool y = true;
    do
    {
        if (y)
        {
            foo();
        }
        else
        {
            bar();
        }
    } while (x);
}

void case3()
{
    bool x = false;
    do
    {
        if (y)
        {
            foo();
        }
        else
        {
            bar();
        }
    } while (x);
}

void case4()
{
    int i = 0;
    do
    {
        switch(i)
        {
            case 0:
                foo();
                break;
            case 1:
                bar();
                break;
            case 2:
                return;
        }

        i++;
    } while (true);
}

void case5()
{
    int i = 0;
    while (i<2)
    {
        do
        {
            if (i==0)
            {
                foo();
            }
            else
            {
                bar();
            }
        }
        while (false);

        i++;
    }
}
