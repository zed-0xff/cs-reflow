void case1()
{
    bool a = true;
    if ( a || b ) // b is unknown but it should not be evaluated
    {
        should_run1();
    }
}

void case2()
{
    bool a = false;
    if ( a && b ) // b is unknown but it should not be evaluated
    {
        should_not_run2();
    }
    else
    {
        should_run2();
    }
}

void case3()
{
    bool a = true;
    var c = a ? 123 : (b ? 456 : 789); // b is unknown but it should not be evaluated
    stub(c); // prevent 'c' to be dropped by post-processor
}

void case4()
{
    var b = true;
    if (b)
    {
        call_me();
    }
}

void unk_and_unk() {
    int foo, bar;

    if (foo < 0 && bar < 0)
        maybe_called_lt();
    if (foo <= 0 && bar <= 0)
        maybe_called_lte();
    if (foo == 0 && bar == 0)
        maybe_called_eq();
    if (foo != 0 && bar != 0)
        maybe_called_ne();
    if (foo > 0 && bar > 0)
        maybe_called_gt();
    if (foo >= 0 && bar >= 0)
        maybe_called_gte();
}

void unk_or_unk() {
    int foo, bar;

    if (foo < 0 || bar < 0)
        maybe_called_lt();
    if (foo <= 0 || bar <= 0)
        maybe_called_lte();
    if (foo == 0 || bar == 0)
        maybe_called_eq();
    if (foo != 0 || bar != 0)
        maybe_called_ne();
    if (foo > 0 || bar > 0)
        maybe_called_gt();
    if (foo >= 0 || bar >= 0)
        maybe_called_gte();
}
