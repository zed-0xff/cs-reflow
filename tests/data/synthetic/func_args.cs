void case1(int x) {
    if (x >= 0)
        maybe_called();
}

void case2(uint x) {
    if (x >= 0)
        always_called();
}
