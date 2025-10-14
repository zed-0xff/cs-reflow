public class ListHelper {
    public unsafe static void MoveItemTo<T>(List<T> list, int fromIndex, int toIndex)
    {
        int num = 7;
        int num2 = 7;
        num2 = 7;
        int[] array = new int[14];
        array[7] = 1;
        array[6] = 2;
        array[8] = 3;
        array[9] = 4;
        array[11] = 5;
        array[13] = 6;
        array[10] = 7;
        while (num2 != 0)
        {
            switch (array[num + 0])
            {
                default:
                    num = ((fromIndex < 0) ? 1 : 0) * 2 + 6;
                    break;

                case 2:
                    num = ((fromIndex < list.Count) ? 1 : 0) * 1 + 8;
                    break;

                case 3:
                    throw new ArgumentOutOfRangeException("fromIndex", "FromIndex is out of range.");

                case 4:
                    num = ((toIndex < 0) ? 1 : 0) * 2 + 11;
                    break;

                case 5:
                    num = ((toIndex < list.Count) ? 1 : 0) * -3 + 13;
                    break;

                case 6:
                    throw new ArgumentOutOfRangeException("toIndex", "ToIndex is out of range.");

                case 7:
                    {
                        T item = list[fromIndex];
                        list.RemoveAt(fromIndex);
                        list.Insert(toIndex, item);
                        num2 = 0;
                        break;
                    }
                case 0:
                    return;
            }
        }
        num2 = 7;
    }
}

