public unsafe static List<string> a9eaae98b4cd4ecdb9a67adb0b812ecf()
{
    List<string> list = new List<string>();
    List<string> list2 = list;
    int num8 = default(int);
    object obj = default(object);
    int num10 = default(int);
    object obj2 = default(object);
    obj2 = mkdir_abd3();
    obj = IO.Directory.GetFiles((string)obj2, Strings_a392.a8c2ee1d336b4bee9ea084c338609bd1);
    num8 = ((Array)obj).Length;
    if (num8 > 0)
    {
        num10 = 0;
        l101:
            if (num10 >= num8)
            {
                return list2;
            }
            else
            {
                list2.Add(IO.Path.GetFileNameWithoutExtension((string)((object[])obj)[num10]));
                num10++;
                goto l101;
            }
    }
    else
    {
        return list2;
    }
}
