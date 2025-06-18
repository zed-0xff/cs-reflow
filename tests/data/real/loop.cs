	public unsafe static List<string> loop()
	{
		List<string> list = new List<string>();
		int num = 53;
		List<string> list2 = list;
		uint num6 = default(uint);
		int num7 = default(int);
		int num8 = default(int);
		object obj = default(object);
		int num10 = default(int);
		object obj2 = default(object);
		while (true)
		{
			int num2 = -1702299507;
			while (true)
			{
				uint num3;
				int num9;
				int num4;
				int num5;
				int num11;
				switch ((num3 = (uint)(num2 ^ -555322840)) % 6) // [0x980]
				{
				case 3u:
					break;
				case 0u:                                                                     // [1, 2, 3, 4, 5, 6, 7, 8, 9, ...
					switch ((num6 = (uint)(~(-(-173978484 - (773630666 - (num7 + -1178365887)))) - -1909079758)) % 11) // [7, 2, 9, 7, 2, 9, 7, 2, 9, ...
					{
					case 5u:                                                                    // [3]
						break;
					case 10u:                                                                   // [2]
                        obj = IO.Directory.GetFiles((string)obj2, Strings_a392.a8c2ee1d336b4bee9ea084c338609bd1);
                        num9 = ((int)num6 * -2090672266) ^ -1987554389;
                        goto IL_0050;
					default:
						goto IL_00d7;
					case 0u:
						goto IL_00e9;
					case 4u:                                                                    // [4]
						goto IL_00f3;
					case 3u:                                                                    // [6]
						goto IL_011b;
					case 6u:                                                                    // [1]
                        obj2 = mkdir_abd3();
                        num9 = (int)(num6 * 505398991) ^ -1059411489;
                        goto IL_0050;
					case 7u:                                                                    // [1, 4, 7, 10, 13, 16, 19, 22, 25, ...
                        list2.Add(IO.Path.GetFileNameWithoutExtension((string)((object[])obj)[num10]));
                        num9 = 658570296;
                        goto IL_0050;
					case 9u:                                                                    // [3, 6, 9, 12, 15, 18, 21, 24, 27, ...
						goto IL_0179;
					case 8u:                                                                    // [5]
						goto IL_019b;
					case 2u:                                                                    // [2, 5, 8, 11, 14, 17, 20, 23, 26, ...
						goto IL_01b1;
					case 1u:                                                                    // [1]
						goto IL_01ca;
					}
					num8 = ((Array)obj).Length;
					num9 = ((int)num6 * -1453621085) ^ 0x6A823CE6;
					goto IL_0050;
				case 1u:                                                                     // [1]
					goto IL_00e9;
				case 4u:
					goto IL_00f3;
				default:                                                                     // [1]
					num4 = 1910365010;
					num5 = 1910365010;
					goto IL_0102;
				case 5u:
					goto IL_01ca;
					IL_01ca:
					return list2;
					IL_01b1:
					num10++;
					num9 = ((int)num6 * -613512936) ^ -475491621;
					goto IL_0050;
					IL_019b:
					num10 = 0;
					num9 = (int)((num6 * 1180182368) ^ 0x105783C7);
					goto IL_0050;
					IL_0179:
					if (num10 >= num8)
					{
						num9 = 887190687;
						num11 = 887190687;
					}
					else
					{
						num9 = 1456007238;
						num11 = 1456007238;
					}
					goto IL_0050;

					IL_00d7:
					num2 = (int)((num3 * 993963302) ^ 0x4C4D2C4D);
					continue;

					IL_011b:
					num9 = (int)(num6 * 1967367450) ^ -1803474099;
					goto IL_0050;

					IL_00f3:
					if (num8 > 0)
					{
						num4 = 808681761;
						num5 = 808681761;
						goto IL_0102;
					}
					num2 = -975596328;
					continue;
					IL_00e9:
					num9 = 818880659;
					goto IL_0050;
					IL_0102:
					num9 = num4 ^ (int)(num6 * 1843075551);
					goto IL_0050;

					IL_0050:
					num7 = num9;
					num2 = -416297362;
					continue;
				}
				break;
			}
		}
	}
