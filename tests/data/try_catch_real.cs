	public unsafe override void OnPaint(PaintEventArgs P_0)
	{
		uint num3 = default(uint);
		int num4 = default(int);
		int num = default(int);
		try
		{
			while (true)
			{
				num += 410;
				if (Control.GetStyle(this, 8192))
				{
					goto IL_0025;
				}
				goto IL_016a;
				IL_016a:
				ab5dbb20393b4538b9a05d9405e644d5(P_0);
				if (((1207959552 * (num * 97 + num * 159)) | 0x24DA) != 9434)
				{
					continue;
				}
				int num2 = 1750757892;
				goto IL_0053;
				IL_013d:
				((Control)this).OnPaintBackground(P_0);
				if ((((num3 & 0x1939) - num3) | 0xFFFFFFFEu) != 4294967294u)
				{
					continue;
				}
				num2 = (int)(num3 * 1903923989) ^ -8789405;
				goto IL_0053;
				IL_0053:
				num4 = num2;
				if ((num | -5189 | 0x14E6) != -1)
				{
					continue;
				}
				switch ((num3 = (uint)(-(~num4 ^ (~(~2107002447) + (971432967 * (~(991337816 - 653080029) - 1063704549 * (-950694200 - ~-172386562)) + ~(-1791726087 * -(-1432762050 + 478948210) - (2135180344 - -297177851 + -1994082233) * -1130517793)))) - (626446491 - (~(-376501758 ^ 0x46F55627) - -(0x1C084188 ^ -1195698877))))) % 4)
				{
				case 3u:
					break;
				default:
					goto IL_010a;
				case 1u:
					goto IL_013d;
				case 0u:
					goto IL_016a;
				case 2u:
					return;
				}
				goto IL_004e;
				IL_004e:
				num2 = -1182964399;
				goto IL_0053;
				IL_010a:
				int num5;
				if ((int)num3 * -1929379840 >>> 21 != -1897800320)
				{
					num5 = 700455875;
					goto IL_002a;
				}
				break;
				IL_0025:
				num5 = 1777788882;
				goto IL_002a;
				IL_002a:
				uint num6;
				switch ((num6 = (uint)(num5 ^ 0x2672A3A7)) % 4)
				{
				case 3u:
					break;
				case 1u:
					goto IL_004e;
				case 0u:
					return;
				default:
					goto IL_013d;
				}
				goto IL_0025;
			}
		}
		catch
		{
			num -= 228;
			while (true)
			{
				num = (num & -55) | 1;
				int num7 = 1258653693;
				while (true)
				{
					uint num6;
					uint num8 = (num6 = (uint)(num7 ^ 0x2672A3A7));
					num = (num & -55) | 1;
					int num9;
					switch (num8 % 5)
					{
					case 0u:
						break;
					case 2u:
						switch ((num3 = (uint)(-(~num4 ^ (~(~2107002447) + (971432967 * (~(991337816 - 653080029) - 1063704549 * (-950694200 - ~-172386562)) + ~(-1791726087 * -(-1432762050 + 478948210) - (2135180344 - -297177851 + -1994082233) * -1130517793)))) - (626446491 - (~(-376501758 ^ 0x46F55627) - -(0x1C084188 ^ -1195698877))))) % 3)
						{
						case 2u:
							goto IL_02ae;
						case 1u:
							goto IL_02be;
						case 0u:
							goto IL_02dd;
						}
						num7 = (int)(num6 * 81662989) ^ -1922669427;
						continue;
					case 4u:
						goto IL_02ae;
					default:
						goto IL_02be;
					case 1u:
						goto IL_02dd;
						IL_02dd:
						num += 281;
						return;
						IL_02be:
						Control.Invalidate(this);
						num9 = ((int)num3 * -1460942011) ^ -1470939749;
						goto IL_02b3;
						IL_02ae:
						num9 = 1389954359;
						goto IL_02b3;
						IL_02b3:
						num4 = num9;
						num7 = 1854990594;
						continue;
					}
					break;
				}
			}
		}
	}
