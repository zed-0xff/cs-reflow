public class ListHelper {
    // got an infinite loop reflowing this
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
                    {
                        int num3 = ((fromIndex < 0) ? 1 : 0) * 2 + 6;
                        num = num3;
                        break;
                    }
                case 2:
                    {
                        int num3 = ((fromIndex < list.Count) ? 1 : 0) * 1 + 8;
                        num = num3;
                        break;
                    }
                case 3:
                    {
                        object obj;
                        object value3 = (obj = new char[9]);
                        ((short[])obj)[0] = 102;
                        ((short[])obj)[1] = (short)(((byte*)Unsafe.AsPointer(ref SeedGetCustData.getImportPathIReferenceIdentity))[1] ^ 0x4A);
                        ((short[])obj)[2] = 111;
                        ((short[])obj)[3] = 109;
                        ((short[])obj)[4] = (short)(((byte*)Unsafe.AsPointer(ref SeedGetCustData.getImportPathIReferenceIdentity))[4] ^ 0x70);
                        ((short[])obj)[5] = 110;
                        ((short[])obj)[6] = 100;
                        ((short[])obj)[7] = (short)(((byte*)Unsafe.AsPointer(ref SeedGetCustData.getImportPathIReferenceIdentity))[7] ^ 0x7D);
                        ((short[])obj)[8] = 120; // "fromIndex"
                        string paramName2 = new string((char[]?)value3);
                        object value4 = (obj = new char[26]);
                        ((short[])obj)[0] = (short)(((byte*)Unsafe.AsPointer(ref TakeOwnershipGeneric.SetMethodSourceRangeActivate))[0] ^ 0x8B);
                        ((short[])obj)[1] = 114;
                        ((short[])obj)[2] = 111;
                        ((short[])obj)[3] = 109;
                        ((short[])obj)[4] = 73;
                        ((short[])obj)[5] = (short)(((byte*)Unsafe.AsPointer(ref TakeOwnershipGeneric.SetMethodSourceRangeActivate))[5] ^ 0x47);
                        ((short[])obj)[6] = 100;
                        ((short[])obj)[7] = (short)(((byte*)Unsafe.AsPointer(ref TakeOwnershipGeneric.SetMethodSourceRangeActivate))[7] ^ 0x7D);
                        ((short[])obj)[8] = 120;
                        ((short[])obj)[9] = (short)(((byte*)Unsafe.AsPointer(ref TakeOwnershipGeneric.SetMethodSourceRangeActivate))[9] ^ 0x6A);
                        ((short[])obj)[10] = 105;
                        ((short[])obj)[11] = (short)(((byte*)Unsafe.AsPointer(ref TakeOwnershipGeneric.SetMethodSourceRangeActivate))[11] ^ 0x6D);
                        ((short[])obj)[12] = 32;
                        ((short[])obj)[13] = 111;
                        ((short[])obj)[14] = (short)(((byte*)Unsafe.AsPointer(ref TakeOwnershipGeneric.SetMethodSourceRangeActivate))[14] ^ 0x94);
                        ((short[])obj)[15] = 116;
                        ((short[])obj)[16] = 32;
                        ((short[])obj)[17] = (short)(((byte*)Unsafe.AsPointer(ref TakeOwnershipGeneric.SetMethodSourceRangeActivate))[17] ^ 0x42);
                        ((short[])obj)[18] = 102;
                        ((short[])obj)[19] = (short)(((byte*)Unsafe.AsPointer(ref TakeOwnershipGeneric.SetMethodSourceRangeActivate))[19] ^ 0x1C);
                        ((short[])obj)[20] = (short)(((byte*)Unsafe.AsPointer(ref TakeOwnershipGeneric.SetMethodSourceRangeActivate))[20] ^ 0x45);
                        ((short[])obj)[21] = 97;
                        ((short[])obj)[22] = 110;
                        ((short[])obj)[23] = 103;
                        ((short[])obj)[24] = 101;
                        ((short[])obj)[25] = 46; // "FromIndex is out of range."
                        throw new ArgumentOutOfRangeException(paramName2, new string((char[]?)value4));
                    }
                case 4:
                    {
                        int num3 = ((toIndex < 0) ? 1 : 0) * 2 + 11;
                        num = num3;
                        break;
                    }
                case 5:
                    {
                        int num3 = ((toIndex < list.Count) ? 1 : 0) * -3 + 13;
                        num = num3;
                        break;
                    }
                case 6:
                    {
                        object obj;
                        object value = (obj = new char[7]);
                        ((short[])obj)[0] = (short)(((byte*)Unsafe.AsPointer(ref FileDialogPermissionSoapDuration.getZonegetMaxSize))[0] ^ 0xA5);
                        ((short[])obj)[1] = (short)(((byte*)Unsafe.AsPointer(ref FileDialogPermissionSoapDuration.getZonegetMaxSize))[1] ^ 0x59);
                        ((short[])obj)[2] = (short)(((byte*)Unsafe.AsPointer(ref FileDialogPermissionSoapDuration.getZonegetMaxSize))[2] ^ 0xE9);
                        ((short[])obj)[3] = 110;
                        ((short[])obj)[4] = (short)(((byte*)Unsafe.AsPointer(ref FileDialogPermissionSoapDuration.getZonegetMaxSize))[4] ^ 0x14);
                        ((short[])obj)[5] = (short)(((byte*)Unsafe.AsPointer(ref FileDialogPermissionSoapDuration.getZonegetMaxSize))[5] ^ 0x56);
                        ((short[])obj)[6] = 120; // "toIndex"
                        string paramName = new string((char[]?)value);
                        object value2 = (obj = new char[24]);
                        ((short[])obj)[0] = 84;
                        ((short[])obj)[1] = 111;
                        ((short[])obj)[2] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[2] ^ 0x31);
                        ((short[])obj)[3] = 110;
                        ((short[])obj)[4] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[4] ^ 0);
                        ((short[])obj)[5] = 101;
                        ((short[])obj)[6] = 120;
                        ((short[])obj)[7] = 32;
                        ((short[])obj)[8] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[8] ^ 0x86);
                        ((short[])obj)[9] = 115;
                        ((short[])obj)[10] = 32;
                        ((short[])obj)[11] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[11] ^ 0x62);
                        ((short[])obj)[12] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[12] ^ 0x27);
                        ((short[])obj)[13] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[13] ^ 0x64);
                        ((short[])obj)[14] = 32;
                        ((short[])obj)[15] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[15] ^ 0x83);
                        ((short[])obj)[16] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[16] ^ 0xBD);
                        ((short[])obj)[17] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[17] ^ 0xAB);
                        ((short[])obj)[18] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[18] ^ 0xA3);
                        ((short[])obj)[19] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[19] ^ 0x10);
                        ((short[])obj)[20] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[20] ^ 0xE0);
                        ((short[])obj)[21] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[21] ^ 0x90);
                        ((short[])obj)[22] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[22] ^ 0x53);
                        ((short[])obj)[23] = (short)(((byte*)Unsafe.AsPointer(ref TYPEFLAGFAPPOBJECTAssemblyBuilderLock.CodeTypeMaskCreateAggregatedObject))[23] ^ 0x5F); // "ToIndex is out of range."
                        throw new ArgumentOutOfRangeException(paramName, new string((char[]?)value2));
                    }
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

