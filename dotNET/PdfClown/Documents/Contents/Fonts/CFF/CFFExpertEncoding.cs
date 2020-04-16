/*
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
     * This is specialized CFFEncoding. It's used if the EncodingId of a font is set to 1.
     * 
     * @author Villu Ruusmann
     */
	public sealed class CFFExpertEncoding : CFFEncoding
	{

		private CFFExpertEncoding()
		{
		}

		public static readonly CFFExpertEncoding Instance = new CFFExpertEncoding();

		static CFFExpertEncoding()
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
			Instance.Add(33, 229);
			Instance.Add(34, 230);
			Instance.Add(35, 0);
			Instance.Add(36, 231);
			Instance.Add(37, 232);
			Instance.Add(38, 233);
			Instance.Add(39, 234);
			Instance.Add(40, 235);
			Instance.Add(41, 236);
			Instance.Add(42, 237);
			Instance.Add(43, 238);
			Instance.Add(44, 13);
			Instance.Add(45, 14);
			Instance.Add(46, 15);
			Instance.Add(47, 99);
			Instance.Add(48, 239);
			Instance.Add(49, 240);
			Instance.Add(50, 241);
			Instance.Add(51, 242);
			Instance.Add(52, 243);
			Instance.Add(53, 244);
			Instance.Add(54, 245);
			Instance.Add(55, 246);
			Instance.Add(56, 247);
			Instance.Add(57, 248);
			Instance.Add(58, 27);
			Instance.Add(59, 28);
			Instance.Add(60, 249);
			Instance.Add(61, 250);
			Instance.Add(62, 251);
			Instance.Add(63, 252);
			Instance.Add(64, 0);
			Instance.Add(65, 253);
			Instance.Add(66, 254);
			Instance.Add(67, 255);
			Instance.Add(68, 256);
			Instance.Add(69, 257);
			Instance.Add(70, 0);
			Instance.Add(71, 0);
			Instance.Add(72, 0);
			Instance.Add(73, 258);
			Instance.Add(74, 0);
			Instance.Add(75, 0);
			Instance.Add(76, 259);
			Instance.Add(77, 260);
			Instance.Add(78, 261);
			Instance.Add(79, 262);
			Instance.Add(80, 0);
			Instance.Add(81, 0);
			Instance.Add(82, 263);
			Instance.Add(83, 264);
			Instance.Add(84, 265);
			Instance.Add(85, 0);
			Instance.Add(86, 266);
			Instance.Add(87, 109);
			Instance.Add(88, 110);
			Instance.Add(89, 267);
			Instance.Add(90, 268);
			Instance.Add(91, 269);
			Instance.Add(92, 0);
			Instance.Add(93, 270);
			Instance.Add(94, 271);
			Instance.Add(95, 272);
			Instance.Add(96, 273);
			Instance.Add(97, 274);
			Instance.Add(98, 275);
			Instance.Add(99, 276);
			Instance.Add(100, 277);
			Instance.Add(101, 278);
			Instance.Add(102, 279);
			Instance.Add(103, 280);
			Instance.Add(104, 281);
			Instance.Add(105, 282);
			Instance.Add(106, 283);
			Instance.Add(107, 284);
			Instance.Add(108, 285);
			Instance.Add(109, 286);
			Instance.Add(110, 287);
			Instance.Add(111, 288);
			Instance.Add(112, 289);
			Instance.Add(113, 290);
			Instance.Add(114, 291);
			Instance.Add(115, 292);
			Instance.Add(116, 293);
			Instance.Add(117, 294);
			Instance.Add(118, 295);
			Instance.Add(119, 296);
			Instance.Add(120, 297);
			Instance.Add(121, 298);
			Instance.Add(122, 299);
			Instance.Add(123, 300);
			Instance.Add(124, 301);
			Instance.Add(125, 302);
			Instance.Add(126, 303);
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
			Instance.Add(161, 304);
			Instance.Add(162, 305);
			Instance.Add(163, 306);
			Instance.Add(164, 0);
			Instance.Add(165, 0);
			Instance.Add(166, 307);
			Instance.Add(167, 308);
			Instance.Add(168, 309);
			Instance.Add(169, 310);
			Instance.Add(170, 311);
			Instance.Add(171, 0);
			Instance.Add(172, 312);
			Instance.Add(173, 0);
			Instance.Add(174, 0);
			Instance.Add(175, 313);
			Instance.Add(176, 0);
			Instance.Add(177, 0);
			Instance.Add(178, 314);
			Instance.Add(179, 315);
			Instance.Add(180, 0);
			Instance.Add(181, 0);
			Instance.Add(182, 316);
			Instance.Add(183, 317);
			Instance.Add(184, 318);
			Instance.Add(185, 0);
			Instance.Add(186, 0);
			Instance.Add(187, 0);
			Instance.Add(188, 158);
			Instance.Add(189, 155);
			Instance.Add(190, 163);
			Instance.Add(191, 319);
			Instance.Add(192, 320);
			Instance.Add(193, 321);
			Instance.Add(194, 322);
			Instance.Add(195, 323);
			Instance.Add(196, 324);
			Instance.Add(197, 325);
			Instance.Add(198, 0);
			Instance.Add(199, 0);
			Instance.Add(200, 326);
			Instance.Add(201, 150);
			Instance.Add(202, 164);
			Instance.Add(203, 169);
			Instance.Add(204, 327);
			Instance.Add(205, 328);
			Instance.Add(206, 329);
			Instance.Add(207, 330);
			Instance.Add(208, 331);
			Instance.Add(209, 332);
			Instance.Add(210, 333);
			Instance.Add(211, 334);
			Instance.Add(212, 335);
			Instance.Add(213, 336);
			Instance.Add(214, 337);
			Instance.Add(215, 338);
			Instance.Add(216, 339);
			Instance.Add(217, 340);
			Instance.Add(218, 341);
			Instance.Add(219, 342);
			Instance.Add(220, 343);
			Instance.Add(221, 344);
			Instance.Add(222, 345);
			Instance.Add(223, 346);
			Instance.Add(224, 347);
			Instance.Add(225, 348);
			Instance.Add(226, 349);
			Instance.Add(227, 350);
			Instance.Add(228, 351);
			Instance.Add(229, 352);
			Instance.Add(230, 353);
			Instance.Add(231, 354);
			Instance.Add(232, 355);
			Instance.Add(233, 356);
			Instance.Add(234, 357);
			Instance.Add(235, 358);
			Instance.Add(236, 359);
			Instance.Add(237, 360);
			Instance.Add(238, 361);
			Instance.Add(239, 362);
			Instance.Add(240, 363);
			Instance.Add(241, 364);
			Instance.Add(242, 365);
			Instance.Add(243, 366);
			Instance.Add(244, 367);
			Instance.Add(245, 368);
			Instance.Add(246, 369);
			Instance.Add(247, 370);
			Instance.Add(248, 371);
			Instance.Add(249, 372);
			Instance.Add(250, 373);
			Instance.Add(251, 374);
			Instance.Add(252, 375);
			Instance.Add(253, 376);
			Instance.Add(254, 377);
			Instance.Add(255, 378);
		}
	}
}