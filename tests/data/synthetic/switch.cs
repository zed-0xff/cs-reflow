int case1(){
    int x;
    switch (x)
    {
        case 1:
            return fun1();

        case 2:
            x = 4;
            break;

        case 5:
            if (x%2==0)
                goto case 1;
            else
                goto case 2;

        case 11:
            goto case 1;

        case 20:
            fun20();
            goto default;

        default:
            x = 3;
            break;
    }
    return x;
}
