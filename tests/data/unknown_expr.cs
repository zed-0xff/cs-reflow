void case1()
{
    int num16 = afd1f0100a9e48eb9a6c8975d80c2689;
    if (-67108864 + (int)((uint)num16 / 1024u) == (int)(2048 * (((uint)num16 % 1949u) ^ 0x70EF1C76)))
    {
        should_not_run();
    }
    else
    {
        should_run();
    }
}
