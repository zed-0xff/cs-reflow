void case1() {
    int foo;

    if (foo < 0)
        maybe_called_lt();
    if (foo <= 0)
        maybe_called_lte();
    if (foo == 0)
        maybe_called_eq();
    if (foo != 0)
        maybe_called_ne();
    if (foo > 0)
        maybe_called_gt();
    if (foo >= 0)
        maybe_called_gte();
}
