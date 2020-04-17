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
namespace PdfClown.Documents.Contents.Fonts.TTF{

using System.IO;

/**
 * A vertical header 'vhea' table in a TrueType or OpenType font.
 *
 * Supports versions 1.0 and 1.1, for which the only difference is changing
 * the specification names and descriptions of the ascender, descender,
 * and lineGap fields to vertTypoAscender, vertTypoDescender, vertTypeLineGap.
 *
 * This table is required by the OpenType CJK Font Guidelines for "all
 * OpenType fonts that are used for vertical writing".
 * 
 * This table is specified in both the TrueType and OpenType specifications.
 * 
 * @author Glenn Adams
 * 
 */
public class VerticalHeaderTable : TTFTable
{
    /**
     * A tag that identifies this table type.
     */
    public static readonly string TAG = "vhea";
    
    private float version;
    private short ascender;
    private short descender;
    private short lineGap;
    private int advanceHeightMax;
    private short minTopSideBearing;
    private short minBottomSideBearing;
    private short yMaxExtent;
    private short caretSlopeRise;
    private short caretSlopeRun;
    private short caretOffset;
    private short reserved1;
    private short reserved2;
    private short reserved3;
    private short reserved4;
    private short metricDataFormat;
    private int numberOfVMetrics;

    VerticalHeaderTable(TrueTypeFont font)
    {
        :base(font);
    }

    /**
     * This will read the required data from the stream.
     * 
     * @param ttf The font that is being read.
     * @param data The stream to read the data from.
     * @ If there is an error reading the data.
     */
    override
    void Read(TrueTypeFont ttf, TTFDataStream data) 
    {
        version = data.Read32Fixed();
        ascender = data.ReadSignedShort();
        descender = data.ReadSignedShort();
        lineGap = data.ReadSignedShort();
        advanceHeightMax = data.ReadUnsignedShort();
        minTopSideBearing = data.ReadSignedShort();
        minBottomSideBearing = data.ReadSignedShort();
        yMaxExtent = data.ReadSignedShort();
        caretSlopeRise = data.ReadSignedShort();
        caretSlopeRun = data.ReadSignedShort();
        caretOffset = data.ReadSignedShort();
        reserved1 = data.ReadSignedShort();
        reserved2 = data.ReadSignedShort();
        reserved3 = data.ReadSignedShort();
        reserved4 = data.ReadSignedShort();
        metricDataFormat = data.ReadSignedShort();
        numberOfVMetrics = data.ReadUnsignedShort();
        initialized = true;
    }
    
    /**
     * @return Returns the advanceHeightMax.
     */
    public int getAdvanceHeightMax()
    {
        return advanceHeightMax;
    }
    /**
     * @return Returns the ascender.
     */
    public short Ascender
    {
        return ascender;
    }
    /**
     * @return Returns the caretSlopeRise.
     */
    public short getCaretSlopeRise()
    {
        return caretSlopeRise;
    }
    /**
     * @return Returns the caretSlopeRun.
     */
    public short getCaretSlopeRun()
    {
        return caretSlopeRun;
    }
    /**
     * @return Returns the caretOffset.
     */
    public short getCaretOffset()
    {
        return caretOffset;
    }
    /**
     * @return Returns the descender.
     */
    public short getDescender()
    {
        return descender;
    }
    /**
     * @return Returns the lineGap.
     */
    public short getLineGap()
    {
        return lineGap;
    }
    /**
     * @return Returns the metricDataFormat.
     */
    public short getMetricDataFormat()
    {
        return metricDataFormat;
    }
    /**
     * @return Returns the minTopSideBearing.
     */
    public short getMinTopSideBearing()
    {
        return minTopSideBearing;
    }
    /**
     * @return Returns the minBottomSideBearing.
     */
    public short getMinBottomSideBearing()
    {
        return minBottomSideBearing;
    }
    /**
     * @return Returns the numberOfVMetrics.
     */
    public int getNumberOfVMetrics()
    {
        return numberOfVMetrics;
    }
    /**
     * @return Returns the reserved1.
     */
    public short Reserved1
    {
        return reserved1;
    }
    /**
     * @return Returns the reserved2.
     */
    public short Reserved2
    {
        return reserved2;
    }
    /**
     * @return Returns the reserved3.
     */
    public short Reserved3()
    {
        return reserved3;
    }
    /**
     * @return Returns the reserved4.
     */
    public short Reserved4
    {
        return reserved4;
    }
    /**
     * @return Returns the version.
     */
    public float Version
    {
        return version;
    }
    /**
     * @return Returns the yMaxExtent.
     */
    public short getYMaxExtent()
    {
        return yMaxExtent;
    }
}
