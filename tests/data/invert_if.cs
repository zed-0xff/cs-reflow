	protected override void invert_if(bool bool_0aw)
	{
		int num = 435;
		if (!bool_0aw)
		{
			goto IL_000c;
		}
		num = (num & -131) | 0x48;
		num = (num & -131) | 0x48;
		goto IL_009a;

		IL_009a:
		int num2 = -430721886;
		goto IL_003e;

		IL_000c:
		num = (num & -131) | 0x48;
		((Form)this).Dispose(bool_0aw);
		num2 = -481026694;
		goto IL_003e;

		IL_003e:
		while (true)
		{
			int num3 = num2;
			uint num4;
			switch ((num4 = (uint)((-503610766 - (-2087385481 - num3) - -1454308309 + 224738694 - 1823481881) * -923629479)) % 5) // 1
			{
			case 3u:
				break;
			case 4u:
				((IDisposable)b96b84d42d854f1f93d6e45a6dc465f7).Dispose();
				num2 = (int)(num4 * 1409572088) ^ -136683018;
				continue;
			case 0u:
				goto IL_009a;
			default:
				num = (num & -131) | 0x48;
				return;
			case 1u:
			{
				int num5;
				int num6;
				if (b96b84d42d854f1f93d6e45a6dc465f7 == null)
				{
					num5 = -767908022;
					num6 = -767908022;
				}
				else
				{
					num = (num & -131) | 0x48;
					num5 = 1654875414;
					num6 = 1654875414;
				}
				num2 = num5 ^ (int)(num4 * 821534444);
				continue;
			}
			case 2u:
				return;
			}
			break;
		}
		goto IL_000c;
	}
