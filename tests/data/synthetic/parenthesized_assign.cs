void case1() {
    int foo;
    ((foo)) = ((((((1))))));
    if( foo > 0 )
        always_called();
}

void case2a() {
    int foo, bar;
    (foo, bar) = (1, 2);
    if( foo > 0 && bar > 0 )
        always_called();
}

void case2b() {
    int foo, bar;
    (((foo, bar))) = (1, 2);
    if( foo > 0 && bar > 0 )
        always_called();
}

void case2c() {
    int foo, bar;
    (foo, bar) = (((1, 2)));
    if( foo > 0 && bar > 0 )
        always_called();
}

void case2d() {
    int foo, bar;
    (((foo, bar))) = (((1, 2)));
    if( foo > 0 && bar > 0 )
        always_called();
}

void case3() {
    int foo, bar;
    (((foo), bar)) = (1, (2));
    if( foo > 0 && bar > 0 )
        always_called();
}

void case4() {
    int foo=1, bar=2;
    (foo, bar) = fun();
    if( foo > 0 && bar > 0 )
        maybe_called();
}

void case5() {
    int foo=1, bar=2;
    (foo, bar) = external_var;
    if( foo > 0 && bar > 0 )
        maybe_called();
}
