	public static string a51ee1556c0e4b359ae4b9687427924c(int int_0nk, int int_1nl, int int_2nm, int int_3nn)
	{
		int num = 507;
		object obj = new char[int_0nk];
		object obj2 = default(object);
		object obj3 = default(object);
		int num6 = default(int);
		int num8 = default(int);
		object obj4 = default(object);
		int seed = default(int);
		object obj5 = default(object);
		int num10 = default(int);
		object obj6 = default(object);
		int num12 = default(int);
		while (true)
		{
			num &= -2;
			num &= -2;
			uint num2 = 911232952u;
			while (true)
			{
				int num3 = 1541273794;
				while (true)
				{
					int num4 = num3;
					num &= -2;
					num2 = 617255856u;
					uint num5;
					while (true)
					{
						switch ((num5 = (uint)(~(-1655092843 - ~(num4 * 1093216613 - -338995535)))) % 39)
						{
						case 35u:
							break;
						case 38u:
							goto IL_00ef;
						case 6u:
							goto IL_0119;
						case 31u:
							goto IL_012b;
						case 8u:
							goto IL_0140;
						case 25u:
							goto IL_0155;
						case 34u:
							goto IL_016a;
						case 2u:
							goto IL_017c;
						case 11u:
							goto IL_0195;
						case 36u:
							goto IL_01ae;
						case 24u:
							goto IL_01c7;
						case 23u:
							goto IL_01e1;
						case 9u:
							goto IL_01f6;
						case 21u:
							goto IL_0216;
						case 20u:
							goto IL_0237;
						case 14u:
							goto IL_0249;
						case 28u:
							goto IL_025b;
						case 13u:
							goto IL_027b;
						case 15u:
							goto IL_029c;
						case 16u:
							goto IL_02b4;
						case 33u:
							goto IL_02c6;
						case 17u:
							goto IL_02df;
						case 5u:
							goto IL_02f9;
						case 19u:
							goto IL_0321;
						default:
						{
							int num7 = ((int)num2 * -795858593) ^ -779392907;
							num &= -2;
							switch ((num2 = (uint)(num7 ^ -846984167)) % 5)
							{
							case 3u:
								break;
							case 2u:
								goto end_IL_0049;
							case 1u:
								continue;
							default:
								goto IL_02df;
							case 4u:
								goto IL_0525;
							}
							goto end_IL_0024;
						}
						case 30u:
							goto IL_0379;
						case 7u:
							goto IL_0393;
						case 37u:
							goto IL_03a8;
						case 29u:
							((Array)obj2).CopyTo((Array)obj3, int_0nk);
							if (((((uint)num6 / 1838537459u) | (num2 << 14)) ^ 0x18A4) == 0)
							{
								goto default;
							}
							goto IL_03e3;
						case 10u:
							goto IL_03f5;
						case 1u:
							goto IL_0411;
						case 27u:
							goto IL_042a;
						case 12u:
							goto IL_043c;
						case 3u:
							goto IL_0456;
						case 4u:
							goto IL_0477;
						case 0u:
							goto IL_0495;
						case 32u:
							goto IL_04ad;
						case 18u:
							goto IL_04c5;
						case 26u:
							goto IL_04df;
						case 22u:
							goto IL_0525;
							IL_0525:
							return new string((char[]?)obj3);
							end_IL_0049:
							break;
						}
						break;
					}
					break;
					IL_01ae:
					a76aec11e5f44b0192b96663807fe1e4((char[])obj3);
					num3 = (int)(num5 * 918302736) ^ -680446508;
					continue;
					IL_04df:
					num8++;
					int a05a685af4b1416382a8bb55b80a91ac = Structs_e0d5.IHDR;
					if (((((uint)(6711 + -83394560 * a05a685af4b1416382a8bb55b80a91ac) & num2) ^ 0x1D3C) & 0x2140) == 256)
					{
						num3 = ((int)num5 * -1523043794) ^ 0x21CE9AD6;
						continue;
					}
					goto IL_021b;
					IL_0195:
					obj4 = new Random(seed);
					num3 = ((int)num5 * -1892591915) ^ -615417959;
					continue;
					IL_04c5:
					obj5 = new char[int_2nm];
					num3 = (int)(num5 * 1002335565) ^ -678713635;
					continue;
					IL_04ad:
					seed = a8e9b0795da8415f8c143cfd0444d27e();
					num3 = (int)(num5 * 997424310) ^ -524343499;
					continue;
					IL_0495:
					seed = a8e9b0795da8415f8c143cfd0444d27e();
					num3 = (int)(num5 * 532717421) ^ -1640952322;
					continue;
					IL_0477:
					((Array)obj5).CopyTo((Array)obj3, int_0nk + int_1nl);
					num3 = (int)((num5 * 762959913) ^ 0x72192CBB);
					continue;
					IL_0456:
					int num9;
					if (num6 < int_0nk)
					{
						num3 = 1249171081;
						num9 = 1249171081;
					}
					else
					{
						num3 = -527586889;
						num9 = -527586889;
					}
					continue;
					IL_017c:
					obj4 = new Random(seed);
					num3 = ((int)num5 * -967002799) ^ 0xD815F3C;
					continue;
					IL_016a:
					num10 = 0;
					num3 = (int)((num5 * 214437178) ^ 0x3FC15E57);
					continue;
					IL_043c:
					((short[])obj5)[num8] = (short)(ushort)((Random)obj4).Next(48, 57);
					num3 = 357664013;
					continue;
					IL_042a:
					num3 = ((int)num5 * -1425474817) ^ 0x78235269;
					continue;
					IL_0411:
					obj4 = new Random(seed);
					num3 = ((int)num5 * -1077525267) ^ -712474598;
					continue;
					IL_03f5:
					((Array)obj).CopyTo((Array)obj3, 0);
					num3 = (int)(num5 * 638667246) ^ -1159673726;
					continue;
					IL_03e3:
					num3 = (int)((num5 * 785937745) ^ 0x16DF4F7C);
					continue;
					IL_03a8:
					int num11;
					if (num10 >= int_1nl)
					{
						num3 = -1454231484;
						num11 = -1454231484;
					}
					else
					{
						num3 = -1114046442;
						num11 = -1114046442;
					}
					continue;
					IL_0155:
					num6++;
					num3 = ((int)num5 * -750016683) ^ 0x429B5527;
					continue;
					IL_0140:
					num10++;
					num3 = (int)(num5 * 1084420177) ^ -1315758622;
					continue;
					IL_0393:
					num6 = 0;
					num3 = (int)(num5 * 1094617503) ^ -2062835671;
					continue;
					IL_0379:
					((short[])obj6)[num12] = (short)(ushort)((Random)obj4).Next(33, 47);
					num3 = -440635308;
					continue;
					IL_0321:
					((short[])obj2)[num10] = (short)(ushort)((Random)obj4).Next(97, 122);
					num3 = 1128769324;
					continue;
					IL_02f9:
					seed = a8e9b0795da8415f8c143cfd0444d27e();
					num3 = (int)((num5 * 1714646279) ^ 0x1DEA615C);
					continue;
					IL_02df:
					((short[])obj)[num6] = (short)(ushort)((Random)obj4).Next(65, 90);
					num3 = 1905497210;
					continue;
					IL_02c6:
					obj4 = new Random(seed);
					num3 = (int)(num5 * 102840135) ^ -441901536;
					continue;
					IL_02b4:
					num3 = ((int)num5 * -1581315317) ^ -7467505;
					continue;
					IL_029c:
					num12++;
					num3 = (int)((num5 * 603369298) ^ 0x3911C4C1);
					continue;
					IL_027b:
					int num13;
					if (num12 < int_3nn)
					{
						num3 = 2040417356;
						num13 = 2040417356;
					}
					else
					{
						num3 = -1274024695;
						num13 = -1274024695;
					}
					continue;
					IL_012b:
					seed = a8e9b0795da8415f8c143cfd0444d27e();
					num3 = (int)(num5 * 1326336390) ^ -875359700;
					continue;
					IL_0119:
					num12 = 0;
					num3 = (int)((num5 * 1180826493) ^ 0x435D5EAB);
					continue;
					IL_025b:
					((Array)obj6).CopyTo((Array)obj3, int_0nk + int_1nl + int_2nm);
					num3 = ((int)num5 * -1812783754) ^ -112764836;
					continue;
					IL_0249:
					num3 = (int)(num5 * 1774726016) ^ -346948211;
					continue;
					IL_0237:
					num3 = (int)(num5 * 2116593507) ^ -143627979;
					continue;
					IL_0216:
					int num14;
					if (num8 < int_2nm)
					{
						num3 = 337184056;
						num14 = 337184056;
						continue;
					}
					goto IL_021b;
					IL_021b:
					num3 = 1450355500;
					num14 = 1450355500;
					continue;
					IL_00ef:
					obj6 = new char[int_3nn];
					num3 = ((int)num5 * -568827897) ^ 0x243D13F9;
					continue;
					IL_01f6:
					obj3 = new char[int_0nk + int_1nl + int_2nm + int_3nn];
					num3 = (int)((num5 * 1686330154) ^ 0x6FBDF064);
					continue;
					IL_01e1:
					num8 = 0;
					num3 = (int)((num5 * 1754889234) ^ 0x30C4A6D5);
					continue;
					IL_01c7:
					obj2 = new char[int_1nl];
					num3 = (int)(num5 * 1220470105) ^ -1048158991;
				}
				continue;
				end_IL_0024:
				break;
			}
		}
	}
