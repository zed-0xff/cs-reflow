	public unsafe static void labels(object object_0md, object object_1me, object object_2mf)
	{
		int num = 145;
		int num2 = 0;
		int i = 0;
		string empty = string.Empty;
		num += 167;
		object string_0a = empty;
		object string_1b = default(object);
		int bzh = default(int);
		List<string> list = default(List<string>);
		object string_2c = default(object);
		object obj = default(object);
		uint num12 = default(uint);
		object collection = default(object);
		int tRNS = default(int);
		while (true)
		{
			num = (num & -6) | 0x22;
			int num9 = 604501969;
			while (true)
			{
				num = (num & -6) | 0x22;
				int num11;
				object obj3;
				int num14;
				uint num10;
				switch ((num10 = (uint)(num9 ^ 0x46962EF8)) % 4) // 1
				{
				case 2u:
					break;
				default:
					string_1b = string.Empty;
					num11 = ((int)num12 * -65300408) ^ -40788167;
					goto IL_0317;
				case 1u:
					goto IL_0371;
				case 0u:
					goto IL_04b3;
					IL_0317:
					while (true)
					{
						int num13 = num11;
						bzh = Structs_a7cb.BZh;
						switch ((num12 = (uint)(~((((-1470310763 - (((~num13 + 1432133790) * -804575305) ^ 0x5AA86F53)) ^ 0x5F7CBD7A) - -796678405) * 1075799265))) % 6) // 2
						{
						case 3u:
							break;
						case 5u:
							list = new List<string>();
							num11 = ((int)num12 * -1693120819) ^ 0x2EDE2F4E;
							continue;
						case 4u:
							goto IL_0371;
						default:
							goto IL_0378;
						case 2u:
							string_2c = string.Empty;
							num11 = (int)((num12 * 776499231) ^ 0x42780CAE);
							continue;
						case 0u:
							goto IL_039b;
						case 1u:
							goto IL_04b3;
						}
						break;
						IL_039b:
						if (String.get_Length(((Main_a546)((Form_af54)object_0md).main_a93b).rec_afb8.str_ac86) > 2)
						{
							num11 = ((int)num12 * -1843933523) ^ -1064715550;
							continue;
						}
						goto IL_03c4;
					}
					goto default;
					IL_04b3:
					obj = string.Empty;
					try
					{
						num -= 134;
						obj = Text.Encoding.get_UTF8().GetString(Convert.FromBase64String(((Main_a546)((Form_af54)object_0md).main_a93b).rec_afb8.str_ac86));
					}
					catch
					{
					}
					obj3 = obj;
					num += 134;
					if (!String.Contains(obj3, Strings_a392.a8f2095a7d364b9bb95b7201d7e914bd))
					{
						goto IL_03c4;
					}
					goto IL_052f;
					IL_0371:
					num11 = 1643590510;
					goto IL_0317;
					IL_03c4:
					a99d58504b1948b7a3cc4dc5bd76f268(((Control)((Form_af54)object_0md).comboBox_db7d).Text, list, ((Main_a546)((Form_af54)object_0md).main_a93b).rec_afb8, ref *(string*)(&string_0a), ref *(string*)(&string_1b), ref *(string*)(&string_2c));
					num = (num & -299) | 0x54;
					num14 = -966473800;
					goto IL_0486;
					IL_0486:
					while (true)
					{
						IL_0486_2:
						int num13 = num14;
						num = (num & -299) | 0x54;
						num10 = 432931167u;
						while (true)
						{
							switch ((num12 = (uint)(~((((-1470310763 - (((~num13 + 1432133790) * -804575305) ^ 0x5AA86F53)) ^ 0x5F7CBD7A) - -796678405) * 1075799265))) % 6) // 1
							{
							case 5u:
								break;
							case 4u:
								goto IL_0453;
							case 2u:
								goto IL_0550;
							case 1u:
								goto IL_055a;
							default:
								goto IL_059f;
							case 3u:
								goto IL_05e4;
							case 0u:
								goto IL_06f6;
							}
							break;
							IL_06f6:
							list.AddRange((IEnumerable<string>)collection);
							tRNS = Structs_e0d5.tRNS;
							num14 = ((int)num12 * -2044630975) ^ -560316090;
							goto IL_0486_2;
							IL_059f:
							int num15 = ((int)num10 * -2094500760) ^ -1277570951;
							num = (num & -299) | 0x54;
							switch ((num10 = (uint)(num15 ^ 0x46962EF8)) % 6) // 1
							{
							case 3u:
								break;
							case 4u:
								goto IL_052f;
							case 2u:
								goto IL_0550;
							case 5u:
								goto IL_055a;
							default:
								goto IL_058d;
							case 1u:
								goto IL_05e4;
							}
							continue;
							IL_05e4:
							((Control)((Form_af54)object_0md).label_af37).Text = (string)string_1b;
							try
							{
								num += 13;
								((Main_a546)((Form_af54)object_0md).main_a93b).rec_afb8.str_a5df = (string)string_0a;
								while (true)
								{
									int num16 = 381844840;
									while (true)
									{
										int num17;
										switch ((num10 = (uint)(num16 ^ 0x46962EF8)) % 5) // 1
										{
										case 4u:
											break;
										default:
											((Main_a546)((Form_af54)object_0md).main_a93b).rec_afb8.str_aff0 = (string)string_1b;
											num17 = (int)(num12 * 1306662877) ^ -515468595;
											goto IL_0658;
										case 1u:
											goto IL_0661;
										case 0u:
											switch ((num12 = (uint)(~((((-1470310763 - (((~num13 + 1432133790) * -804575305) ^ 0x5AA86F53)) ^ 0x5F7CBD7A) - -796678405) * 1075799265))) % 4) // 3
											{
											case 1u:
												break;
											case 2u:
												goto IL_0661;
											case 3u:
												goto IL_06b1;
											default:
												goto IL_06de;
											case 0u:
												return;
											}
											goto default;
										case 2u:
											return;
											IL_06de:
											num16 = (int)(num10 * 1847282627) ^ -112278406;
											continue;
											IL_06b1:
											((Main_a546)((Form_af54)object_0md).main_a93b).rec_afb8.str_a9f5 = ((Control)((Form_af54)object_0md).comboBox_db7d).Text;
											num17 = ((int)num12 * -318626851) ^ 0x4780989C;
											goto IL_0658;
											IL_0661:
											num17 = -1132026034;
											goto IL_0658;
											IL_0658:
											num13 = num17;
											num16 = 463301342;
											continue;
										}
										break;
									}
								}
							}
							catch
							{
								return;
							}
							IL_055a:
							do
							{
								((Control)((Form_af54)object_0md).label_a811).Text = (string)string_0a;
							}
							while (~(4 * (tRNS << 9)) == 618807296 * bzh);
							num = (num & -299) | 0x54;
							num10 = 155403726u;
							goto IL_058d;
							IL_058d:
							num14 = ((int)num12 * -746669197) ^ -2068435597;
							goto IL_0486_2;
						}
						break;
						IL_0453:
						object obj5 = obj;
						string[] obj6 = new string[1] { Strings_a392.a8f2095a7d364b9bb95b7201d7e914bd };
						collection = String.Split(obj5, obj6, 0);
						num14 = (int)((num12 * 666558629) ^ 0x3DAC9A77);
					}
					goto IL_03c4;
					IL_052f:
					num = (num & -299) | 0x54;
					num = (num & -299) | 0x54;
					num10 = 1100818022u;
					goto IL_0550;
					IL_0550:
					num14 = 1415031209;
					goto IL_0486;
				}
				break;
				IL_0378:
				num9 = 158604456;
			}
		}
	}
