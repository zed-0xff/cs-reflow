	public unsafe override void goto_default_case(EventArgs eventArgs_0b)
	{
		int num = 179;
		flag_acf3 = false;
		uint num4 = default(uint);
		int num5 = default(int);
		while (true)
		{
			int num2 = -439629514;
			while (true)
			{
				uint num3;
				int num6;
				switch ((num3 = (uint)(num2 ^ -234246726)) % 5) // 2
				{
				case 0u:
					break;
				default:
					flag_a60c = false;
					num6 = (int)((num4 * 352270299) ^ 0x62DBB8E3);
					goto IL_004a;
				case 2u:
					goto IL_008c;
				case 3u:
					switch ((num4 = (uint)((-999233115 - (1322037553 - -((~num5 ^ -1621966082 ^ 0x4C893E7A) - -711194964)) * 2027507243) * 967908351)) % 6) // 5
					{
					case 2u:
						break;
					case 3u:
						goto IL_0052;
					case 1u:
						goto IL_0069;
					case 5u:
						goto IL_008c;
					case 4u:
						goto IL_00e4;
					default:
						goto IL_00fe;
					case 0u:
						return;
					}
					goto default;
				case 1u:
					return;
					IL_00fe:
					num2 = ((int)num3 * -954060534) ^ 0x40156E68;
					continue;
					IL_00e4:
					flag_a913 = false;
					num6 = (int)((num4 * 1663077128) ^ 0x318C5C86);
					goto IL_004a;
					IL_0069:
					Control.Invalidate(this);
					num6 = (int)((num4 * 1488197031) ^ 0x6A7F6EE5);
					goto IL_004a;
					IL_0052:
					((ButtonBase)this).OnLostFocus(eventArgs_0b);
					num6 = ((int)num4 * -1527328290) ^ -1667105523;
					goto IL_004a;
					IL_008c:
					num6 = -2045260617;
					goto IL_004a;
					IL_004a:
					num5 = num6;
					num2 = -655925034;
					continue;
				}
				break;
			}
		}
	}
