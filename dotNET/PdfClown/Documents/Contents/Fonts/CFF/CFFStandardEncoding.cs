/*
 * https://github.com/apache/pdfbox
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
namespace PdfClown.Documents.Contents.Fonts.CCF
{
	/**
	 * This is specialized CFFEncoding. It's used if the EncodingId of a font is set to 0.
	 * 
	 * @author Villu Ruusmann
	 */
	public sealed class CFFStandardEncoding : CFFEncoding
	{
		private CFFStandardEncoding()
		{
		}

		public static readonly CFFStandardEncoding Instance = new CFFStandardEncoding();

		static CFFStandardEncoding()
		{
			Instance.Add(0, 0);
			Instance.Add(1, 0);
			Instance.Add(2, 0);
			Instance.Add(3, 0);
			Instance.Add(4, 0);
			Instance.Add(5, 0);
			Instance.Add(6, 0);
			Instance.Add(7, 0);
			Instance.Add(8, 0);
			Instance.Add(9, 0);
			Instance.Add(10, 0);
			Instance.Add(11, 0);
			Instance.Add(12, 0);
			Instance.Add(13, 0);
			Instance.Add(14, 0);
			Instance.Add(15, 0);
			Instance.Add(16, 0);
			Instance.Add(17, 0);
			Instance.Add(18, 0);
			Instance.Add(19, 0);
			Instance.Add(20, 0);
			Instance.Add(21, 0);
			Instance.Add(22, 0);
			Instance.Add(23, 0);
			Instance.Add(24, 0);
			Instance.Add(25, 0);
			Instance.Add(26, 0);
			Instance.Add(27, 0);
			Instance.Add(28, 0);
			Instance.Add(29, 0);
			Instance.Add(30, 0);
			Instance.Add(31, 0);
			Instance.Add(32, 1);
			Instance.Add(33, 2);
			Instance.Add(34, 3);
			Instance.Add(35, 4);
			Instance.Add(36, 5);
			Instance.Add(37, 6);
			Instance.Add(38, 7);
			Instance.Add(39, 8);
			Instance.Add(40, 9);
			Instance.Add(41, 10);
			Instance.Add(42, 11);
			Instance.Add(43, 12);
			Instance.Add(44, 13);
			Instance.Add(45, 14);
			Instance.Add(46, 15);
			Instance.Add(47, 16);
			Instance.Add(48, 17);
			Instance.Add(49, 18);
			Instance.Add(50, 19);
			Instance.Add(51, 20);
			Instance.Add(52, 21);
			Instance.Add(53, 22);
			Instance.Add(54, 23);
			Instance.Add(55, 24);
			Instance.Add(56, 25);
			Instance.Add(57, 26);
			Instance.Add(58, 27);
			Instance.Add(59, 28);
			Instance.Add(60, 29);
			Instance.Add(61, 30);
			Instance.Add(62, 31);
			Instance.Add(63, 32);
			Instance.Add(64, 33);
			Instance.Add(65, 34);
			Instance.Add(66, 35);
			Instance.Add(67, 36);
			Instance.Add(68, 37);
			Instance.Add(69, 38);
			Instance.Add(70, 39);
			Instance.Add(71, 40);
			Instance.Add(72, 41);
			Instance.Add(73, 42);
			Instance.Add(74, 43);
			Instance.Add(75, 44);
			Instance.Add(76, 45);
			Instance.Add(77, 46);
			Instance.Add(78, 47);
			Instance.Add(79, 48);
			Instance.Add(80, 49);
			Instance.Add(81, 50);
			Instance.Add(82, 51);
			Instance.Add(83, 52);
			Instance.Add(84, 53);
			Instance.Add(85, 54);
			Instance.Add(86, 55);
			Instance.Add(87, 56);
			Instance.Add(88, 57);
			Instance.Add(89, 58);
			Instance.Add(90, 59);
			Instance.Add(91, 60);
			Instance.Add(92, 61);
			Instance.Add(93, 62);
			Instance.Add(94, 63);
			Instance.Add(95, 64);
			Instance.Add(96, 65);
			Instance.Add(97, 66);
			Instance.Add(98, 67);
			Instance.Add(99, 68);
			Instance.Add(100, 69);
			Instance.Add(101, 70);
			Instance.Add(102, 71);
			Instance.Add(103, 72);
			Instance.Add(104, 73);
			Instance.Add(105, 74);
			Instance.Add(106, 75);
			Instance.Add(107, 76);
			Instance.Add(108, 77);
			Instance.Add(109, 78);
			Instance.Add(110, 79);
			Instance.Add(111, 80);
			Instance.Add(112, 81);
			Instance.Add(113, 82);
			Instance.Add(114, 83);
			Instance.Add(115, 84);
			Instance.Add(116, 85);
			Instance.Add(117, 86);
			Instance.Add(118, 87);
			Instance.Add(119, 88);
			Instance.Add(120, 89);
			Instance.Add(121, 90);
			Instance.Add(122, 91);
			Instance.Add(123, 92);
			Instance.Add(124, 93);
			Instance.Add(125, 94);
			Instance.Add(126, 95);
			Instance.Add(127, 0);
			Instance.Add(128, 0);
			Instance.Add(129, 0);
			Instance.Add(130, 0);
			Instance.Add(131, 0);
			Instance.Add(132, 0);
			Instance.Add(133, 0);
			Instance.Add(134, 0);
			Instance.Add(135, 0);
			Instance.Add(136, 0);
			Instance.Add(137, 0);
			Instance.Add(138, 0);
			Instance.Add(139, 0);
			Instance.Add(140, 0);
			Instance.Add(141, 0);
			Instance.Add(142, 0);
			Instance.Add(143, 0);
			Instance.Add(144, 0);
			Instance.Add(145, 0);
			Instance.Add(146, 0);
			Instance.Add(147, 0);
			Instance.Add(148, 0);
			Instance.Add(149, 0);
			Instance.Add(150, 0);
			Instance.Add(151, 0);
			Instance.Add(152, 0);
			Instance.Add(153, 0);
			Instance.Add(154, 0);
			Instance.Add(155, 0);
			Instance.Add(156, 0);
			Instance.Add(157, 0);
			Instance.Add(158, 0);
			Instance.Add(159, 0);
			Instance.Add(160, 0);
			Instance.Add(161, 96);
			Instance.Add(162, 97);
			Instance.Add(163, 98);
			Instance.Add(164, 99);
			Instance.Add(165, 100);
			Instance.Add(166, 101);
			Instance.Add(167, 102);
			Instance.Add(168, 103);
			Instance.Add(169, 104);
			Instance.Add(170, 105);
			Instance.Add(171, 106);
			Instance.Add(172, 107);
			Instance.Add(173, 108);
			Instance.Add(174, 109);
			Instance.Add(175, 110);
			Instance.Add(176, 0);
			Instance.Add(177, 111);
			Instance.Add(178, 112);
			Instance.Add(179, 113);
			Instance.Add(180, 114);
			Instance.Add(181, 0);
			Instance.Add(182, 115);
			Instance.Add(183, 116);
			Instance.Add(184, 117);
			Instance.Add(185, 118);
			Instance.Add(186, 119);
			Instance.Add(187, 120);
			Instance.Add(188, 121);
			Instance.Add(189, 122);
			Instance.Add(190, 0);
			Instance.Add(191, 123);
			Instance.Add(192, 0);
			Instance.Add(193, 124);
			Instance.Add(194, 125);
			Instance.Add(195, 126);
			Instance.Add(196, 127);
			Instance.Add(197, 128);
			Instance.Add(198, 129);
			Instance.Add(199, 130);
			Instance.Add(200, 131);
			Instance.Add(201, 0);
			Instance.Add(202, 132);
			Instance.Add(203, 133);
			Instance.Add(204, 0);
			Instance.Add(205, 134);
			Instance.Add(206, 135);
			Instance.Add(207, 136);
			Instance.Add(208, 137);
			Instance.Add(209, 0);
			Instance.Add(210, 0);
			Instance.Add(211, 0);
			Instance.Add(212, 0);
			Instance.Add(213, 0);
			Instance.Add(214, 0);
			Instance.Add(215, 0);
			Instance.Add(216, 0);
			Instance.Add(217, 0);
			Instance.Add(218, 0);
			Instance.Add(219, 0);
			Instance.Add(220, 0);
			Instance.Add(221, 0);
			Instance.Add(222, 0);
			Instance.Add(223, 0);
			Instance.Add(224, 0);
			Instance.Add(225, 138);
			Instance.Add(226, 0);
			Instance.Add(227, 139);
			Instance.Add(228, 0);
			Instance.Add(229, 0);
			Instance.Add(230, 0);
			Instance.Add(231, 0);
			Instance.Add(232, 140);
			Instance.Add(233, 141);
			Instance.Add(234, 142);
			Instance.Add(235, 143);
			Instance.Add(236, 0);
			Instance.Add(237, 0);
			Instance.Add(238, 0);
			Instance.Add(239, 0);
			Instance.Add(240, 0);
			Instance.Add(241, 144);
			Instance.Add(242, 0);
			Instance.Add(243, 0);
			Instance.Add(244, 0);
			Instance.Add(245, 145);
			Instance.Add(246, 0);
			Instance.Add(247, 0);
			Instance.Add(248, 146);
			Instance.Add(249, 147);
			Instance.Add(250, 148);
			Instance.Add(251, 149);
			Instance.Add(252, 0);
			Instance.Add(253, 0);
			Instance.Add(254, 0);
			Instance.Add(255, 0);
		}
	}
}