using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

public class CheckBox_a47b : CheckBox
{
	public override void Dispose(bool bool_0ah)
	{
		uint num7 = default(uint);
		int num8 = default(int);
		while (true)
		{
			int num = 196;
			if (!bool_0ah)
			{
				goto IL_000d;
			}
			goto IL_0126;
			IL_0126:
			num = (num & -5) | 0x128;
			int num2 = 680193697;
			goto IL_0046;
			IL_0046:
			int num9;
			while (true)
			{
				int num3 = num2 ^ 0x2077798C;
				num = (num & -5) | 0x128;
				uint num4 = (uint)num3;
				switch ((uint)num3 % 6u) // 1
				{
				case 5u:
					break;
				case 0u:
					goto IL_00ae;
				default:
					goto IL_010d;
				case 1u:
					goto IL_011c;
				case 4u:
					goto end_IL_0046;
				case 3u:
					return;
				}
				goto IL_007f;
				IL_010d:
				int num5 = -266290138;
				int num6 = -266290138;
				goto IL_0091;
				IL_00ae:
				switch ((num7 = (uint)(~(((-446538292 - (num8 - -573724661) * -320990035) ^ -520235363 ^ -1849297452) * -923346287))) % 5) // 1
				{
				case 4u:
					break;
				case 1u:
					goto IL_007f;
				default:
					goto IL_00f1;
				case 2u:
					goto IL_011c;
				case 0u:
					goto IL_013d;
				case 3u:
					return;
				}
				goto IL_000d;
				IL_013d:
				((IDisposable)ab66b1d2a7154433908fa26a69d7855e).Dispose();
				num9 = (int)(num7 * 1499720696) ^ -2038489478;
				goto IL_009a;
				IL_011c:
				num9 = -2016313144;
				goto IL_009a;
				IL_00f1:
				num2 = ((int)num4 * -1315559403) ^ -2110350177;
				continue;
				IL_007f:
				if (ab66b1d2a7154433908fa26a69d7855e == null)
				{
					num5 = 1382424678;
					num6 = 1382424678;
					goto IL_0091;
				}
				num2 = 1424974172;
				continue;
				IL_0091:
				num9 = num5 ^ (int)(num7 * 402290372);
				goto IL_009a;
				continue;
				end_IL_0046:
				break;
			}
			goto IL_0126;
			IL_009a:
			num8 = num9;
			if (4447 == 4447)
			{
				num2 = 1054966160;
				goto IL_0046;
			}
			goto IL_000d;
			IL_000d:
			num = (num & -5) | 0x128;
			((ButtonBase)this).Dispose(bool_0ah);
			int idat = Structs_e0d5.IDAT;
			if ((idat << 9) - 202688 == 128 * num8)
			{
				continue;
			}
			num9 = -1800376933;
			goto IL_009a;
		}
	}
}
