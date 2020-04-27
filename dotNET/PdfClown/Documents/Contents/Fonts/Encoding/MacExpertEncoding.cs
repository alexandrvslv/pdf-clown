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
using PdfClown.Objects;

using System.Collections.Generic;

namespace PdfClown.Documents.Contents.Fonts
{
    /**
     * This is an interface to a text encoder.
     */
    internal class MacExpertEncoding : Encoding
    {
        public static readonly MacExpertEncoding Instance = new MacExpertEncoding();
        public MacExpertEncoding()
        {
            Encodings[PdfName.MacExpertEncoding] = this;

            Put(0276, "AEsmall");
            Put(0207, "Aacutesmall");
            Put(0211, "Acircumflexsmall");
            Put(047, "Acutesmall");
            Put(0212, "Adieresissmall");
            Put(0210, "Agravesmall");
            Put(0214, "Aringsmall");
            Put(0141, "Asmall");
            Put(0213, "Atildesmall");
            Put(0363, "Brevesmall");
            Put(0142, "Bsmall");
            Put(0256, "Caronsmall");
            Put(0215, "Ccedillasmall");
            Put(0311, "Cedillasmall");
            Put(0136, "Circumflexsmall");
            Put(0143, "Csmall");
            Put(0254, "Dieresissmall");
            Put(0372, "Dotaccentsmall");
            Put(0144, "Dsmall");
            Put(0216, "Eacutesmall");
            Put(0220, "Ecircumflexsmall");
            Put(0221, "Edieresissmall");
            Put(0217, "Egravesmall");
            Put(0145, "Esmall");
            Put(0104, "Ethsmall");
            Put(0146, "Fsmall");
            Put(0140, "Gravesmall");
            Put(0147, "Gsmall");
            Put(0150, "Hsmall");
            Put(042, "Hungarumlautsmall");
            Put(0222, "Iacutesmall");
            Put(0224, "Icircumflexsmall");
            Put(0225, "Idieresissmall");
            Put(0223, "Igravesmall");
            Put(0151, "Ismall");
            Put(0152, "Jsmall");
            Put(0153, "Ksmall");
            Put(0302, "Lslashsmall");
            Put(0154, "Lsmall");
            Put(0364, "Macronsmall");
            Put(0155, "Msmall");
            Put(0156, "Nsmall");
            Put(0226, "Ntildesmall");
            Put(0317, "OEsmall");
            Put(0227, "Oacutesmall");
            Put(0231, "Ocircumflexsmall");
            Put(0232, "Odieresissmall");
            Put(0362, "Ogoneksmall");
            Put(0230, "Ogravesmall");
            Put(0277, "Oslashsmall");
            Put(0157, "Osmall");
            Put(0233, "Otildesmall");
            Put(0160, "Psmall");
            Put(0161, "Qsmall");
            Put(0373, "Ringsmall");
            Put(0162, "Rsmall");
            Put(0247, "Scaronsmall");
            Put(0163, "Ssmall");
            Put(0271, "Thornsmall");
            Put(0176, "Tildesmall");
            Put(0164, "Tsmall");
            Put(0234, "Uacutesmall");
            Put(0236, "Ucircumflexsmall");
            Put(0237, "Udieresissmall");
            Put(0235, "Ugravesmall");
            Put(0165, "Usmall");
            Put(0166, "Vsmall");
            Put(0167, "Wsmall");
            Put(0170, "Xsmall");
            Put(0264, "Yacutesmall");
            Put(0330, "Ydieresissmall");
            Put(0171, "Ysmall");
            Put(0275, "Zcaronsmall");
            Put(0172, "Zsmall");
            Put(046, "ampersandsmall");
            Put(0201, "asuperior");
            Put(0365, "bsuperior");
            Put(0251, "centinferior");
            Put(043, "centoldstyle");
            Put(0202, "centsuperior");
            Put(072, "colon");
            Put(0173, "colonmonetary");
            Put(054, "comma");
            Put(0262, "commainferior");
            Put(0370, "commasuperior");
            Put(0266, "dollarinferior");
            Put(044, "dollaroldstyle");
            Put(045, "dollarsuperior");
            Put(0353, "dsuperior");
            Put(0245, "eightinferior");
            Put(070, "eightoldstyle");
            Put(0241, "eightsuperior");
            Put(0344, "esuperior");
            Put(0326, "exclamdownsmall");
            Put(041, "exclamsmall");
            Put(0126, "ff");
            Put(0131, "ffi");
            Put(0132, "ffl");
            Put(0127, "fi");
            Put(0320, "figuredash");
            Put(0114, "fiveeighths");
            Put(0260, "fiveinferior");
            Put(065, "fiveoldstyle");
            Put(0336, "fivesuperior");
            Put(0130, "fl");
            Put(0242, "fourinferior");
            Put(064, "fouroldstyle");
            Put(0335, "foursuperior");
            Put(057, "fraction");
            Put(055, "hyphen");
            Put(0137, "hypheninferior");
            Put(0321, "hyphensuperior");
            Put(0351, "isuperior");
            Put(0361, "lsuperior");
            Put(0367, "msuperior");
            Put(0273, "nineinferior");
            Put(071, "nineoldstyle");
            Put(0341, "ninesuperior");
            Put(0366, "nsuperior");
            Put(053, "onedotenleader");
            Put(0112, "oneeighth");
            Put(0174, "onefitted");
            Put(0110, "onehalf");
            Put(0301, "oneinferior");
            Put(061, "oneoldstyle");
            Put(0107, "onequarter");
            Put(0332, "onesuperior");
            Put(0116, "onethird");
            Put(0257, "osuperior");
            Put(0133, "parenleftinferior");
            Put(050, "parenleftsuperior");
            Put(0135, "parenrightinferior");
            Put(051, "parenrightsuperior");
            Put(056, "period");
            Put(0263, "periodinferior");
            Put(0371, "periodsuperior");
            Put(0300, "questiondownsmall");
            Put(077, "questionsmall");
            Put(0345, "rsuperior");
            Put(0175, "rupiah");
            Put(073, "semicolon");
            Put(0115, "seveneighths");
            Put(0246, "seveninferior");
            Put(067, "sevenoldstyle");
            Put(0340, "sevensuperior");
            Put(0244, "sixinferior");
            Put(066, "sixoldstyle");
            Put(0337, "sixsuperior");
            Put(040, "space");
            Put(0352, "ssuperior");
            Put(0113, "threeeighths");
            Put(0243, "threeinferior");
            Put(063, "threeoldstyle");
            Put(0111, "threequarters");
            Put(075, "threequartersemdash");
            Put(0334, "threesuperior");
            Put(0346, "tsuperior");
            Put(052, "twodotenleader");
            Put(0252, "twoinferior");
            Put(062, "twooldstyle");
            Put(0333, "twosuperior");
            Put(0117, "twothirds");
            Put(0274, "zeroinferior");
            Put(060, "zerooldstyle");
            Put(0342, "zerosuperior");
        }

        public override PdfDirectObject GetPdfObject()
        {
            return PdfName.MacExpertEncoding;
        }

    }
}
