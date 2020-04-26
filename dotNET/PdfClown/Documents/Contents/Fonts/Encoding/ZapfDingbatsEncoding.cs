/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Clown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the License, or (at your option) any later version.

  This Program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY,
  either expressed or implied; without even the implied warranty of MERCHANTABILITY or
  FITNESS FOR A PARTICULAR PURPOSE. See the License for more details.

  You should have received a copy of the GNU Lesser General Public License along with this
  Program (see README files); if not, go to the GNU website (http://www.gnu.org/licenses/).

  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

using PdfClown.Objects;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
      <summary>ZapfDingbats encoding [PDF:1.7:D.5].</summary>
    */
    internal sealed class ZapfDingbatsEncoding : Encoding
    {
        public static readonly ZapfDingbatsEncoding Instance = new ZapfDingbatsEncoding();
        public ZapfDingbatsEncoding()
        {
            Put(040, "space");
            Put(041, "a1");
            Put(042, "a2");
            Put(043, "a202");
            Put(044, "a3");
            Put(045, "a4");
            Put(046, "a5");
            Put(047, "a119");
            Put(050, "a118");
            Put(051, "a117");
            Put(052, "a11");
            Put(053, "a12");
            Put(054, "a13");
            Put(055, "a14");
            Put(056, "a15");
            Put(057, "a16");
            Put(060, "a105");
            Put(061, "a17");
            Put(062, "a18");
            Put(063, "a19");
            Put(064, "a20");
            Put(065, "a21");
            Put(066, "a22");
            Put(067, "a23");
            Put(070, "a24");
            Put(071, "a25");
            Put(072, "a26");
            Put(073, "a27");
            Put(074, "a28");
            Put(075, "a6");
            Put(076, "a7");
            Put(077, "a8");
            Put(0100, "a9");
            Put(0101, "a10");
            Put(0102, "a29");
            Put(0103, "a30");
            Put(0104, "a31");
            Put(0105, "a32");
            Put(0106, "a33");
            Put(0107, "a34");
            Put(0110, "a35");
            Put(0111, "a36");
            Put(0112, "a37");
            Put(0113, "a38");
            Put(0114, "a39");
            Put(0115, "a40");
            Put(0116, "a41");
            Put(0117, "a42");
            Put(0120, "a43");
            Put(0121, "a44");
            Put(0122, "a45");
            Put(0123, "a46");
            Put(0124, "a47");
            Put(0125, "a48");
            Put(0126, "a49");
            Put(0127, "a50");
            Put(0130, "a51");
            Put(0131, "a52");
            Put(0132, "a53");
            Put(0133, "a54");
            Put(0134, "a55");
            Put(0135, "a56");
            Put(0136, "a57");
            Put(0137, "a58");
            Put(0140, "a59");
            Put(0141, "a60");
            Put(0142, "a61");
            Put(0143, "a62");
            Put(0144, "a63");
            Put(0145, "a64");
            Put(0146, "a65");
            Put(0147, "a66");
            Put(0150, "a67");
            Put(0151, "a68");
            Put(0152, "a69");
            Put(0153, "a70");
            Put(0154, "a71");
            Put(0155, "a72");
            Put(0156, "a73");
            Put(0157, "a74");
            Put(0160, "a203");
            Put(0161, "a75");
            Put(0162, "a204");
            Put(0163, "a76");
            Put(0164, "a77");
            Put(0165, "a78");
            Put(0166, "a79");
            Put(0167, "a81");
            Put(0170, "a82");
            Put(0171, "a83");
            Put(0172, "a84");
            Put(0173, "a97");
            Put(0174, "a98");
            Put(0175, "a99");
            Put(0176, "a100");
            Put(0241, "a101");
            Put(0242, "a102");
            Put(0243, "a103");
            Put(0244, "a104");
            Put(0245, "a106");
            Put(0246, "a107");
            Put(0247, "a108");
            Put(0250, "a112");
            Put(0251, "a111");
            Put(0252, "a110");
            Put(0253, "a109");
            Put(0254, "a120");
            Put(0255, "a121");
            Put(0256, "a122");
            Put(0257, "a123");
            Put(0260, "a124");
            Put(0261, "a125");
            Put(0262, "a126");
            Put(0263, "a127");
            Put(0264, "a128");
            Put(0265, "a129");
            Put(0266, "a130");
            Put(0267, "a131");
            Put(0270, "a132");
            Put(0271, "a133");
            Put(0272, "a134");
            Put(0273, "a135");
            Put(0274, "a136");
            Put(0275, "a137");
            Put(0276, "a138");
            Put(0277, "a139");
            Put(0300, "a140");
            Put(0301, "a141");
            Put(0302, "a142");
            Put(0303, "a143");
            Put(0304, "a144");
            Put(0305, "a145");
            Put(0306, "a146");
            Put(0307, "a147");
            Put(0310, "a148");
            Put(0311, "a149");
            Put(0312, "a150");
            Put(0313, "a151");
            Put(0314, "a152");
            Put(0315, "a153");
            Put(0316, "a154");
            Put(0317, "a155");
            Put(0320, "a156");
            Put(0321, "a157");
            Put(0322, "a158");
            Put(0323, "a159");
            Put(0324, "a160");
            Put(0325, "a161");
            Put(0326, "a163");
            Put(0327, "a164");
            Put(0330, "a196");
            Put(0331, "a165");
            Put(0332, "a192");
            Put(0333, "a166");
            Put(0334, "a167");
            Put(0335, "a168");
            Put(0336, "a169");
            Put(0337, "a170");
            Put(0340, "a171");
            Put(0341, "a172");
            Put(0342, "a173");
            Put(0343, "a162");
            Put(0344, "a174");
            Put(0345, "a175");
            Put(0346, "a176");
            Put(0347, "a177");
            Put(0350, "a178");
            Put(0351, "a179");
            Put(0352, "a193");
            Put(0353, "a180");
            Put(0354, "a199");
            Put(0355, "a181");
            Put(0356, "a200");
            Put(0357, "a182");
            Put(0361, "a201");
            Put(0362, "a183");
            Put(0363, "a184");
            Put(0364, "a197");
            Put(0365, "a185");
            Put(0366, "a194");
            Put(0367, "a198");
            Put(0370, "a186");
            Put(0371, "a195");
            Put(0372, "a187");
            Put(0373, "a188");
            Put(0374, "a189");
            Put(0375, "a190");
            Put(0376, "a191");
        }

        public override PdfDirectObject GetPdfObject()
        {
            return PdfName.Get("ZapfDingbatsEncoding");
        }
    }
}