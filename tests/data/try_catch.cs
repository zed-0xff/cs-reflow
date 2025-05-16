	public unsafe override void try_catch(PaintEventArgs paintEventArgs_0a)
	{
		int num3 = default(int);
		uint num4 = default(uint);
		int num = default(int);
		try
		{
			num = 410;
			if (!Control.GetStyle(this, 8192))
			{
				goto IL_001a;
			}
			goto IL_0058;
			IL_001a:
			ab5dbb20393b4538b9a05d9405e644d5(paintEventArgs_0a);
			int num2 = 1750757892;
			goto IL_002d;
			IL_002d:
			num3 = num2;
			switch ((num4 = (uint)(-(~num3 ^ -243804085) - 807262198)) % 4) // 2
			{
			case 0u:
				break;
			case 3u:
				goto IL_0028;
			case 1u:
				goto IL_007c;
			default:
				goto IL_0092;
			case 2u:
				return;
			}
			goto IL_001a;
			IL_0092:
			int num5 = 700455875;
			goto IL_005d;
			IL_005d:
			uint num6;
			switch ((num6 = (uint)(num5 ^ 0x2672A3A7)) % 4) // 0
			{
			case 1u:
				break;
			case 3u:
				goto IL_0058;
			default:
				goto IL_007c;
			case 0u:
				return;
			}
			goto IL_0028;
			IL_0028:
			num2 = -1182964399;
			goto IL_002d;
			IL_0058:
			num5 = 1777788882;
			goto IL_005d;
			IL_007c:
			((Control)this).OnPaintBackground(paintEventArgs_0a);
			num2 = (int)(num4 * 1903923989) ^ -8789405;
			goto IL_002d;
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
					switch (num8 % 5) // 4
					{
					case 0u:
						break;
					case 4u:
						num9 = 1389954359;
						goto IL_00ba;
					case 2u:
						switch ((num4 = (uint)(-(~num3 ^ -243804085) - 807262198)) % 3) // 0
						{
						case 2u:
							break;
						case 1u:
							goto IL_0112;
						default:
							goto IL_0131;
						case 0u:
							goto IL_0140;
						}
						goto case 4u;
					default:
						goto IL_0112;
					case 1u:
						goto IL_0140;
						IL_0140:
						num += 281;
						return;
						IL_0131:
						num7 = (int)(num6 * 81662989) ^ -1922669427;
						continue;
						IL_0112:
						Control.Invalidate(this);
						num9 = ((int)num4 * -1460942011) ^ -1470939749;
						goto IL_00ba;
						IL_00ba:
						num3 = num9;
						num7 = 1854990594;
						continue;
					}
					break;
				}
			}
		}
	}
