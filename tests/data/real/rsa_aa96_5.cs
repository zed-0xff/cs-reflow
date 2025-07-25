	public unsafe static RSACryptoServiceProvider rsa_aa96(object object_0bed)
	{
		int tRNS = default(int);
		object input = default(object);
		uint num11 = default(uint);
		object result = default(object);
		int num18 = default(int);
		object obj2 = default(object);
		object arr15 = default(object);
		ushort type19 = default(ushort);
		RSAParameters parameters = default(RSAParameters);
		RSAParameters rSAParameters = default(RSAParameters);
		byte b = default(byte);
		object exponent = default(object);
		int count = default(int);
		byte b2 = default(byte);
		byte b3 = default(byte);
		byte b4 = default(byte);
		object obj3 = default(object);
		ushort num26 = default(ushort);
		object modulus = default(object);
		ushort num27 = default(ushort);
		int num14 = default(int);
		while (true)
		{
			byte[] array = new byte[115];
			int num = 32;
			array[9] = 101;
			string text = get_str(49595, 35902, 137);
			int num2 = 0;
			while (true)
			{
				int num3 = num2;
				num |= 6;
				if (num3 >= String.get_Length(text))
				{
					break;
				}
				array[num2 + 12] = (byte)String.get_Chars(text, num2);
				num2++;
			}
			int num4 = a999fbf5299144caa731c75edd1c4be0;
			string text2 = get_str(27513, 9243, 76);
			int num5 = 0;
			while (true)
			{
				num = (num & -35) | 0x191;
				if (num5 >= String.get_Length(text2))
				{
					break;
				}
				array[num5 + 56] = (byte)String.get_Chars(text2, num5);
				num5++;
			}
			string text3 = get_str(38752, 55247, 202);
			int num6 = 0;
			while (true)
			{
				num = (num & -6) | 0x6A;
				if (num6 >= String.get_Length(text3))
				{
					break;
				}
				array[num6 + 63] = (byte)String.get_Chars(text3, num6);
				num6++;
			}
			num -= 280;
			array[62] = 14;
			byte[] array2 = new byte[15];
			array[11] = 29;
			array[10] = 164;
			array[55] = 213;
			Runtime.CompilerServices.RuntimeHelpers.InitializeArray(array2, (RuntimeFieldHandle)/*OpCode not supported: ldmembertoken a2327656515942c2af3673172f772396 at IL_016e*/);
			array[7] = 254;
			object object_1beo = array2;
			while (true)
			{
				num = (num & -129) | 0x10C;
				int num7 = 1739568690;
				while (true)
				{
					int num8 = num7 ^ 0x79CA4C83;
					num = (num & -129) | 0x10C;
					uint num9 = (uint)num8;
					MemoryStream memoryStream;
					uint num10;
					int num12;
					int num13;
					object obj;
					switch ((uint)num8 % 4u)
					{
					case 3u:
						break;
					case 2u:
						array[70] = 248;
						array[3] = 81;
						array[4] = 2;
						array[5] = 178;
						array[6] = 141;
						array[8] = 232;
						QRCoder.PngByteQRCode.aba41bc5444c4cd7875e3b5a7b863448 = array;
						goto IL_0212;
					default:
						array[6] = 141;
						array[70] = 248;
						array[5] = 178;
						tRNS = Structs_e0d5.tRNS;
						array[4] = 2;
						array[3] = 81;
						array[8] = 232;
						QRCoder.PngByteQRCode.aba41bc5444c4cd7875e3b5a7b863448 = array;
						goto IL_0bca;
					case 1u:
						goto IL_0b59;
					case 0u:
						goto IL_0bca;
						IL_0bca:
						memoryStream = new MemoryStream((byte[])object_0bed);
						array[6] = 141;
						input = memoryStream;
						num10 = num11;
						array[70] = 248;
						array[5] = 178;
						num12 = (int)num10 * -588922300;
						array[4] = 2;
						num13 = num12 ^ -1168284341;
						array[3] = 81;
						array[8] = 232;
						QRCoder.PngByteQRCode.aba41bc5444c4cd7875e3b5a7b863448 = array;
						goto IL_0b95;
						IL_0b95:
						num14 = num13;
						switch ((num11 = (uint)(206202068 - (~num14 * -1630396477 - -185993634) * -643586491)) % 3)
						{
						case 2u:
							break;
						case 0u:
							goto IL_0b59;
						case 1u:
							goto IL_0bca;
						default:
							goto IL_0c1c;
						}
						goto IL_0212;
						IL_0b59:
						array[6] = 141;
						array[70] = 248;
						array[3] = 81;
						array[4] = 2;
						array[5] = 178;
						array[8] = 232;
						QRCoder.PngByteQRCode.aba41bc5444c4cd7875e3b5a7b863448 = array;
						num13 = -555799516;
						goto IL_0b95;
						IL_0212:
						obj = new BinaryReader((Stream)input);
						try
						{
							while (true)
							{
								IL_021b:
								num -= 306;
								array[70] = 248;
								array[6] = 141;
								array[3] = 81;
								array[4] = 2;
								array[5] = 178;
								array[8] = 232;
								QRCoder.PngByteQRCode.aba41bc5444c4cd7875e3b5a7b863448 = array;
								ushort type15 = ((BinaryReader)obj).ReadUInt16();
								while (true)
								{
									IL_0265:
									int num16 = 1559208943;
									while (true)
									{
										int num17;
										int num20;
										int num21;
										int num22;
										int num23;
										int num24;
										int num25;
										int num28;
										int num29;
										int num30;
										int num31;
										int num32;
										int num33;
										int num34;
										int num35;
										int num36;
										int num37;
										int num38;
										int num39;
										int num40;
										int num41;
										int num42;
										int num43;
										int num44;
										int num45;
										int num46;
										switch ((num9 = (uint)(num16 ^ 0x79CA4C83)) % 5)
										{
										case 0u:
											break;
										case 4u:
											goto end_IL_026a;
										case 1u:
											goto IL_0298;
										case 3u:
											switch ((num11 = (uint)(206202068 - (~num14 * -1630396477 - -185993634) * -643586491)) % 66)
											{
											case 61u:
												break;
											case 26u:
												goto IL_0298;
											case 65u:
												goto IL_03d6;
											default:
												goto IL_03fd;
											case 56u:
												goto IL_0410;
											case 23u:
												goto IL_0423;
											case 9u:
												goto IL_0435;
											case 4u:
												goto IL_0465;
											case 52u:
												goto IL_0481;
											case 51u:
												goto IL_0493;
											case 2u:
												goto IL_04aa;
											case 31u:
												break;
											case 54u:
												goto IL_04df;
											case 40u:
												goto IL_0531;
											case 28u:
												goto IL_0545;
											case 34u:
												break;
											case 44u:
												goto IL_0583;
											case 10u:
												goto IL_0595;
											case 32u:
												goto IL_05b0;
											case 45u:
												goto IL_05c3;
											case 12u:
												goto IL_05dd;
											case 49u:
												break;
											case 64u:
												goto IL_05f4;
											case 3u:
												goto IL_0606;
											case 14u:
												goto IL_0636;
											case 13u:
												goto IL_0667;
											case 37u:
												goto IL_0697;
											case 22u:
												goto IL_06c0;
											case 11u:
												goto IL_06d3;
											case 63u:
												break;
											case 53u:
												goto IL_06f3;
											case 50u:
												goto IL_0706;
											case 27u:
												goto IL_0719;
											case 1u:
												goto IL_072b;
											case 33u:
												goto IL_075b;
											case 48u:
												break;
											case 15u:
												goto IL_077c;
											case 55u:
												goto IL_0792;
											case 43u:
												goto IL_07a8;
											case 21u:
												goto IL_07c4;
											case 20u:
												goto IL_07d7;
											case 5u:
												goto IL_07ea;
											case 24u:
												goto IL_07fd;
											case 39u:
												goto IL_0827;
											case 35u:
												goto IL_083d;
											case 60u:
												goto IL_0858;
											case 58u:
												goto IL_0888;
											case 41u:
												goto IL_08a4;
											case 16u:
												goto IL_08b7;
											case 29u:
												goto IL_08e7;
											case 42u:
												goto IL_08fa;
											case 62u:
												goto IL_090c;
											case 8u:
												goto IL_093c;
											case 46u:
												goto IL_095a;
											case 59u:
												goto IL_0967;
											case 0u:
												goto IL_097a;
											case 36u:
												goto IL_0991;
											case 57u:
												break;
											case 18u:
												goto IL_09b2;
											case 19u:
												goto IL_09c9;
											case 47u:
												goto IL_09dc;
											case 30u:
												goto IL_09f1;
											case 25u:
												goto IL_0a18;
											case 17u:
												goto IL_0a25;
											case 38u:
												goto IL_0a37;
											case 7u:
												goto IL_0a44;
											case 6u:
												break;
											}
											goto end_IL_026a;
										default:
											goto IL_0410;
											IL_0a44:
											result = null;
											num17 = (int)((num11 * 983528592) ^ 0x32CA1433);
											goto IL_029d;

											IL_0a37:
											result = null;
											num17 = 102885936;
											goto IL_029d;

											IL_0a25:
											((BinaryReader)obj).ReadInt16();
											num17 = 1959606377;
											goto IL_029d;

											IL_0a18:
											result = null;
											num17 = 817274637;
											goto IL_029d;

											IL_09f1:
											num18 = BitConverter.ToInt32((byte[])obj2, 0);
											num17 = ((int)num11 * -956741193) ^ 0x79444E01;
											goto IL_029d;

											IL_09dc:
											arr15 = ((BinaryReader)obj).ReadBytes(15);
											num17 = 47489584;
											goto IL_029d;
											IL_09c9:
											type19 = ((BinaryReader)obj).ReadUInt16();
											num17 = 1083660961;
											goto IL_029d;
											IL_09b2:
											parameters = rSAParameters;
											num17 = ((int)num11 * -803467878) ^ -1681788610;
											goto IL_029d;
											IL_0991:
											b = ((BinaryReader)obj).ReadByte();
											num17 = (int)(num11 * 1515822991) ^ -965797173;
											goto IL_029d;
											IL_097a:
											num18--;
											num17 = ((int)num11 * -978513871) ^ 0x68ECAA83;
											goto IL_029d;
											IL_0967:
											num17 = (int)((num11 * 1416689201) ^ 0x3A7F78B3);
											goto IL_029d;
											IL_095a:
											result = null;
											num17 = -213570056;
											goto IL_029d;
											IL_093c:
											exponent = ((BinaryReader)obj).ReadBytes(count);
											num17 = ((int)num11 * -231741668) ^ 0x734956A;
											goto IL_029d;
											IL_090c:
											if (type19 != 33027)
											{
												num20 = -648039640;
												num21 = -648039640;
											}
											else
											{
												num20 = 909207322;
												num21 = 909207322;
											}
											num17 = num20 ^ (int)(num11 * 1056672600);
											goto IL_029d;
											IL_0481:
											b2 = ((BinaryReader)obj).ReadByte();
											num17 = 343824141;
											goto IL_029d;
											IL_0465:
											b3 = ((BinaryReader)obj).ReadByte();
											num17 = ((int)num11 * -2136843975) ^ -716843593;
											goto IL_029d;
											IL_0606:
											if (type19 != 33283)
											{
												num22 = -425573191;
												num23 = -425573191;
											}
											else
											{
												num22 = -354884968;
												num23 = -354884968;
											}
											num17 = num22 ^ (int)(num11 * 1122686610);
											goto IL_029d;
											IL_08fa:
											((BinaryReader)obj).ReadInt16();
											num17 = -1890524124;
											goto IL_029d;
											IL_08e7:
											count = ((BinaryReader)obj).ReadByte();
											num17 = 581591345;
											goto IL_029d;
											IL_08b7:
											if (type15 == 33072)
											{
												num24 = -501683397;
												num25 = -501683397;
											}
											else
											{
												num24 = 1752543191;
												num25 = 1752543191;
											}
											num17 = num24 ^ (int)(num11 * 1443818454);
											goto IL_029d;
											IL_05f4:
											((BinaryReader)obj).ReadByte();
											num17 = 613021082;
											goto IL_029d;
											IL_05dd:
											b4 = ((BinaryReader)obj).ReadByte();
											num17 = 598224748;
											goto IL_029d;
											IL_0493:
											result = obj3;
											num17 = (int)((num11 * 1891430856) ^ 0x2C31CEAC);
											goto IL_029d;
											IL_08a4:
											num26 = ((BinaryReader)obj).ReadUInt16();
											num17 = -1238560740;
											goto IL_029d;
											IL_0888:
											rSAParameters.Modulus = (byte[]?)modulus;
											num17 = ((int)num11 * -497910927) ^ -1710675756;
											goto IL_029d;
											IL_0858:
											if (num27 != 33026)
											{
												num28 = 942461044;
												num29 = 942461044;
											}
											else
											{
												num28 = 2083520287;
												num29 = 2083520287;
											}
											num17 = num28 ^ (int)(num11 * 106881057);
											goto IL_029d;
											IL_05b0:
											num17 = ((int)num11 * -153719019) ^ -1889957171;
											goto IL_029d;
											IL_0595:
											obj2 = new byte[4] { b, b2, 0, 0 };
											num17 = -873443225;
											goto IL_029d;
											IL_05c3:
											obj3 = new RSACryptoServiceProvider();
											num17 = ((int)num11 * -1343467617) ^ -1736094157;
											goto IL_029d;
											IL_083d:
											((BinaryReader)obj).ReadByte();
											num17 = ((int)num11 * -1264794500) ^ -572031523;
											goto IL_029d;
											IL_0827:
											result = null;
											num17 = ((int)num11 * -523345924) ^ -1176117478;
											goto IL_029d;
											IL_07fd:
											((BinaryReader)obj).BaseStream.Seek(-1L, SeekOrigin.Current);
											num17 = (int)(num11 * 1507769120) ^ -1215918791;
											goto IL_029d;
											IL_07ea:
											num17 = (int)((num11 * 256232201) ^ 0x16DF4D8A);
											goto IL_029d;
											IL_07d7:
											num27 = ((BinaryReader)obj).ReadUInt16();
											num17 = 1491329285;
											goto IL_029d;
											IL_07c4:
											num17 = ((int)num11 * -1531937042) ^ 0x16CF579C;
											goto IL_029d;
											IL_07a8:
											((RSA)obj3).ImportParameters(parameters);
											num17 = ((int)num11 * -1529117686) ^ -1388262334;
											goto IL_029d;
											IL_0792:
											result = null;
											num17 = ((int)num11 * -1868403835) ^ 0xD56E070;
											goto IL_029d;
											IL_077c:
											result = null;
											num17 = (int)(num11 * 2132600689) ^ -1395655543;
											goto IL_029d;
											IL_075b:
											rSAParameters.Exponent = (byte[]?)exponent;
											num17 = ((int)num11 * -95633147) ^ -394044798;
											goto IL_029d;
											IL_072b:
											if (a12d8359c3b4480ca2fc2537a52a0ca8(arr15, object_1beo))
											{
												num30 = 825556877;
												num31 = 825556877;
											}
											else
											{
												num30 = 936075633;
												num31 = 936075633;
											}
											num17 = num30 ^ ((int)num11 * -961325561);
											goto IL_029d;
											IL_0545:
											b2 = 0;
											num32 = a999fbf5299144caa731c75edd1c4be0;
											if ((num32 << 9) + 182816 == (int)(16 * (3036 + (num11 << 7))))
											{
												goto IL_021b;
											}
											num17 = (int)(num11 * 1911128336) ^ -132766737;
											goto IL_029d;
											IL_0435:
											if (num27 == 33282)
											{
												num33 = 687027789;
												num34 = 687027789;
											}
											else
											{
												num33 = 440066362;
												num34 = 440066362;
											}
											num17 = num33 ^ ((int)num11 * -1276101116);
											goto IL_029d;
											IL_0583:
											((BinaryReader)obj).ReadByte();
											num17 = -2030118475;
											goto IL_029d;
											IL_0719:
											((BinaryReader)obj).ReadInt16();
											num17 = 971828733;
											goto IL_029d;
											IL_0706:
											num17 = ((int)num11 * -231704524) ^ -1926100103;
											goto IL_029d;
											IL_06f3:
											num17 = (int)((num11 * 432042389) ^ 0x4EE99320);
											goto IL_029d;
											IL_06d3:
											rSAParameters = default(RSAParameters);
											num17 = ((int)num11 * -1533440579) ^ 0x17E16F50;
											goto IL_029d;
											IL_06c0:
											b = ((BinaryReader)obj).ReadByte();
											num17 = -33865626;
											goto IL_029d;
											IL_0697:
											if (b4 <= 0)
											{
												num35 = 972703981;
												num36 = 972703981;
											}
											else
											{
												num35 = 1933062739;
												num36 = 1933062739;
											}
											num17 = num35 ^ (int)(num11 * 1606329221);
											goto IL_029d;
											IL_04df:
											if (b3 != 0)
											{
												num37 = 443644651;
												num38 = 443644651;
											}
											else
											{
												num37 = -978703232;
												num38 = -978703232;
											}
											num17 = num37 ^ (int)(num11 * 2019493318);
											goto IL_029d;
											IL_029d:
											num14 = num17;
											num16 = 2105454585;
											continue;
											IL_0531:
											modulus = ((BinaryReader)obj).ReadBytes(num18);
											num17 = 1866186981;
											goto IL_029d;
											IL_0667:
											if (num26 == 33328)
											{
												num39 = 1338990506;
												num40 = 1338990506;
											}
											else
											{
												num39 = -2003271765;
												num40 = -2003271765;
											}
											num17 = num39 ^ ((int)num11 * -1789718954);
											goto IL_029d;
											IL_0298:
											num17 = 1654887193;
											goto IL_029d;
											IL_04aa:
											if (type15 == 33328)
											{
												num41 = 684170365;
												num42 = 684170365;
											}
											else
											{
												num41 = 1774944216;
												num42 = 1774944216;
											}
											num17 = num41 ^ ((int)num11 * -463530508);
											goto IL_029d;
											IL_03d6:
											if (num26 != 33072 || (0x359 ^ ((0x82E & num4) * (int)(num9 << 14))) == 0)
											{
												num43 = -230271815;
												num44 = -230271815;
											}
											else
											{
												num43 = -969258762;
												num44 = -969258762;
											}
											num17 = num43 ^ ((int)num11 * -381832113);
											goto IL_029d;
											IL_0636:
											if (((BinaryReader)obj).ReadByte() != 2)
											{
												num45 = 1132579402;
												num46 = 1132579402;
											}
											else
											{
												num45 = -2044125980;
												num46 = -2044125980;
											}
											num17 = num45 ^ ((int)num11 * -2106111021);
											goto IL_029d;
											IL_03fd:
											num16 = (int)(num9 * 972578402) ^ -1437850214;
											continue;
											IL_0423:
											((BinaryReader)obj).ReadByte();
											num17 = -854063398;
											goto IL_029d;
											IL_0410:
											num17 = (int)(num11 * 1378691925) ^ -1740473523;
											goto IL_029d;
										}
										goto IL_0265;
										continue;
										end_IL_026a:
										break;
									}
									break;
								}
								break;
							}
						}
						catch (Exception)
						{
							num += 365;
							while (true)
							{
								IL_0a6a:
								num = (num & -290) | 0x50;
								int num47 = 488282599;
								while (true)
								{
									uint num48 = (num9 = (uint)(num47 ^ 0x79CA4C83));
									num = (num & -290) | 0x50;
									int num49;
									switch (num48 % 5)
									{
									case 2u:
										break;
									default:
										num49 = ((int)num11 * -1275452340) ^ 0x7CC3EC51;
										goto IL_0a8c;
									case 3u:
										result = null;
										num47 = 805713302;
										continue;
									case 1u:
										goto IL_0afc;
									case 0u:
										goto IL_0b0a;
										IL_0a8c:
										num14 = num49;
										switch ((num11 = (uint)(206202068 - (~num14 * -1630396477 - -185993634) * -643586491)) % 3)
										{
										case 2u:
											break;
										case 0u:
											goto IL_0afc;
										default:
											goto IL_0b03;
										case 1u:
											goto IL_0b0a;
										}
										goto case 3u;
										IL_0b0a:
										num -= 156;
										goto end_IL_0ac9;
										IL_0b03:
										num47 = 348488050;
										continue;
										IL_0afc:
										num49 = 1436614677;
										goto IL_0a8c;
									}
									goto IL_0a6a;
									continue;
									end_IL_0ac9:
									break;
								}
								break;
							}
						}
						return (RSACryptoServiceProvider)result;
					}
					break;
					IL_0c1c:
					array[4] = 2;
					array[3] = 81;
					if ((0x40 & (num2 + num2 << 6)) != (0x40 & ((tRNS << 9) - 9825)))
					{
						goto end_IL_0191;
					}
					num7 = 39994481;
					array[70] = 248;
					array[5] = 178;
					array[6] = 141;
					array[8] = 232;
					QRCoder.PngByteQRCode.aba41bc5444c4cd7875e3b5a7b863448 = array;
				}
				continue;
				end_IL_0191:
				break;
			}
		}
	}
