﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.Memory;

namespace SixLabors.ImageSharp.Formats.Jpeg.PdfJsPort.Components
{
    /// <summary>
    /// Performs the inverse Descrete Cosine Transform on each frame component.
    /// </summary>
    internal static class PdfJsIDCT
    {
        private const int DctCos1 = 4017;     // cos(pi/16)
        private const int DctSin1 = 799;      // sin(pi/16)
        private const int DctCos3 = 3406;     // cos(3*pi/16)
        private const int DctSin3 = 2276;     // sin(3*pi/16)
        private const int DctCos6 = 1567;     // cos(6*pi/16)
        private const int DctSin6 = 3784;     // sin(6*pi/16)
        private const int DctSqrt2 = 5793;    // sqrt(2)
        private const int DctSqrt1D2 = 2896;  // sqrt(2) / 2
        private const int MaxJSample = 255;
        private const int CenterJSample = 128;

        /// <summary>
        /// A port of Poppler's IDCT method which in turn is taken from:
        /// Christoph Loeffler, Adriaan Ligtenberg, George S. Moschytz,
        /// 'Practical Fast 1-D DCT Algorithms with 11 Multiplications',
        /// IEEE Intl. Conf. on Acoustics, Speech &amp; Signal Processing, 1989, 988-991.
        /// </summary>
        /// <param name="component">The frame component</param>
        /// <param name="blockBufferOffset">The block buffer offset</param>
        /// <param name="computationBufferRef">The computational buffer for holding temp values ref</param>
        /// <param name="quantizationTableRef">The quantization table ref</param>
        public static void QuantizeAndInverse(PdfJsFrameComponent component, int blockBufferOffset, ref short computationBufferRef, ref short quantizationTableRef)
        {
            ref short blockDataRef = ref MemoryMarshal.GetReference(component.BlockData.Slice(blockBufferOffset));
            int v0, v1, v2, v3, v4, v5, v6, v7;
            int p0, p1, p2, p3, p4, p5, p6, p7;
            int t;

            // inverse DCT on rows
            for (int row = 0; row < 64; row += 8)
            {
                int r1 = row + 1;
                int r2 = row + 2;
                int r3 = row + 3;
                int r4 = row + 4;
                int r5 = row + 5;
                int r6 = row + 6;
                int r7 = row + 7;

                // gather block data
                p0 = Unsafe.Add(ref blockDataRef, row);
                p1 = Unsafe.Add(ref blockDataRef, r1);
                p2 = Unsafe.Add(ref blockDataRef, r2);
                p3 = Unsafe.Add(ref blockDataRef, r3);
                p4 = Unsafe.Add(ref blockDataRef, r4);
                p5 = Unsafe.Add(ref blockDataRef, r5);
                p6 = Unsafe.Add(ref blockDataRef, r6);
                p7 = Unsafe.Add(ref blockDataRef, r7);

                // dequant p0
                p0 *= Unsafe.Add(ref quantizationTableRef, row);

                // check for all-zero AC coefficients
                if ((p1 | p2 | p3 | p4 | p5 | p6 | p7) == 0)
                {
                    t = ((DctSqrt2 * p0) + 512) >> 10;
                    short st = (short)t;
                    Unsafe.Add(ref computationBufferRef, row) = st;
                    Unsafe.Add(ref computationBufferRef, r1) = st;
                    Unsafe.Add(ref computationBufferRef, r2) = st;
                    Unsafe.Add(ref computationBufferRef, r3) = st;
                    Unsafe.Add(ref computationBufferRef, r4) = st;
                    Unsafe.Add(ref computationBufferRef, r5) = st;
                    Unsafe.Add(ref computationBufferRef, r6) = st;
                    Unsafe.Add(ref computationBufferRef, r7) = st;
                    continue;
                }

                // dequant p1 ... p7
                p1 *= Unsafe.Add(ref quantizationTableRef, r1);
                p2 *= Unsafe.Add(ref quantizationTableRef, r2);
                p3 *= Unsafe.Add(ref quantizationTableRef, r3);
                p4 *= Unsafe.Add(ref quantizationTableRef, r4);
                p5 *= Unsafe.Add(ref quantizationTableRef, r5);
                p6 *= Unsafe.Add(ref quantizationTableRef, r6);
                p7 *= Unsafe.Add(ref quantizationTableRef, r7);

                // stage 4
                v0 = ((DctSqrt2 * p0) + CenterJSample) >> 8;
                v1 = ((DctSqrt2 * p4) + CenterJSample) >> 8;
                v2 = p2;
                v3 = p6;
                v4 = ((DctSqrt1D2 * (p1 - p7)) + CenterJSample) >> 8;
                v7 = ((DctSqrt1D2 * (p1 + p7)) + CenterJSample) >> 8;
                v5 = p3 << 4;
                v6 = p5 << 4;

                // stage 3
                v0 = (v0 + v1 + 1) >> 1;
                v1 = v0 - v1;
                t = ((v2 * DctSin6) + (v3 * DctCos6) + CenterJSample) >> 8;
                v2 = ((v2 * DctCos6) - (v3 * DctSin6) + CenterJSample) >> 8;
                v3 = t;
                v4 = (v4 + v6 + 1) >> 1;
                v6 = v4 - v6;
                v7 = (v7 + v5 + 1) >> 1;
                v5 = v7 - v5;

                // stage 2
                v0 = (v0 + v3 + 1) >> 1;
                v3 = v0 - v3;
                v1 = (v1 + v2 + 1) >> 1;
                v2 = v1 - v2;
                t = ((v4 * DctSin3) + (v7 * DctCos3) + 2048) >> 12;
                v4 = ((v4 * DctCos3) - (v7 * DctSin3) + 2048) >> 12;
                v7 = t;
                t = ((v5 * DctSin1) + (v6 * DctCos1) + 2048) >> 12;
                v5 = ((v5 * DctCos1) - (v6 * DctSin1) + 2048) >> 12;
                v6 = t;

                // stage 1
                Unsafe.Add(ref computationBufferRef, row) = (short)(v0 + v7);
                Unsafe.Add(ref computationBufferRef, row + 7) = (short)(v0 - v7);
                Unsafe.Add(ref computationBufferRef, row + 1) = (short)(v1 + v6);
                Unsafe.Add(ref computationBufferRef, row + 6) = (short)(v1 - v6);
                Unsafe.Add(ref computationBufferRef, row + 2) = (short)(v2 + v5);
                Unsafe.Add(ref computationBufferRef, row + 5) = (short)(v2 - v5);
                Unsafe.Add(ref computationBufferRef, row + 3) = (short)(v3 + v4);
                Unsafe.Add(ref computationBufferRef, row + 4) = (short)(v3 - v4);
            }

            // inverse DCT on columns
            for (int col = 0; col < 8; ++col)
            {
                int c8 = col + 8;
                int c16 = col + 16;
                int c24 = col + 24;
                int c32 = col + 32;
                int c40 = col + 40;
                int c48 = col + 48;
                int c56 = col + 56;

                p0 = Unsafe.Add(ref computationBufferRef, col);
                p1 = Unsafe.Add(ref computationBufferRef, c8);
                p2 = Unsafe.Add(ref computationBufferRef, c16);
                p3 = Unsafe.Add(ref computationBufferRef, c24);
                p4 = Unsafe.Add(ref computationBufferRef, c32);
                p5 = Unsafe.Add(ref computationBufferRef, c40);
                p6 = Unsafe.Add(ref computationBufferRef, c48);
                p7 = Unsafe.Add(ref computationBufferRef, c56);

                // check for all-zero AC coefficients
                if ((p1 | p2 | p3 | p4 | p5 | p6 | p7) == 0)
                {
                    t = ((DctSqrt2 * p0) + 8192) >> 14;

                    // convert to 8 bit
                    t = (t < -2040) ? 0 : (t >= 2024) ? MaxJSample : (t + 2056) >> 4;
                    short st = (short)t;

                    Unsafe.Add(ref blockDataRef, col) = st;
                    Unsafe.Add(ref blockDataRef, c8) = st;
                    Unsafe.Add(ref blockDataRef, c16) = st;
                    Unsafe.Add(ref blockDataRef, c24) = st;
                    Unsafe.Add(ref blockDataRef, c32) = st;
                    Unsafe.Add(ref blockDataRef, c40) = st;
                    Unsafe.Add(ref blockDataRef, c48) = st;
                    Unsafe.Add(ref blockDataRef, c56) = st;
                    continue;
                }

                // stage 4
                v0 = ((DctSqrt2 * p0) + 2048) >> 12;
                v1 = ((DctSqrt2 * p4) + 2048) >> 12;
                v2 = p2;
                v3 = p6;
                v4 = ((DctSqrt1D2 * (p1 - p7)) + 2048) >> 12;
                v7 = ((DctSqrt1D2 * (p1 + p7)) + 2048) >> 12;
                v5 = p3;
                v6 = p5;

                // stage 3
                // Shift v0 by 128.5 << 5 here, so we don't need to shift p0...p7 when
                // converting to UInt8 range later.
                v0 = ((v0 + v1 + 1) >> 1) + 4112;
                v1 = v0 - v1;
                t = ((v2 * DctSin6) + (v3 * DctCos6) + 2048) >> 12;
                v2 = ((v2 * DctCos6) - (v3 * DctSin6) + 2048) >> 12;
                v3 = t;
                v4 = (v4 + v6 + 1) >> 1;
                v6 = v4 - v6;
                v7 = (v7 + v5 + 1) >> 1;
                v5 = v7 - v5;

                // stage 2
                v0 = (v0 + v3 + 1) >> 1;
                v3 = v0 - v3;
                v1 = (v1 + v2 + 1) >> 1;
                v2 = v1 - v2;
                t = ((v4 * DctSin3) + (v7 * DctCos3) + 2048) >> 12;
                v4 = ((v4 * DctCos3) - (v7 * DctSin3) + 2048) >> 12;
                v7 = t;
                t = ((v5 * DctSin1) + (v6 * DctCos1) + 2048) >> 12;
                v5 = ((v5 * DctCos1) - (v6 * DctSin1) + 2048) >> 12;
                v6 = t;

                // stage 1
                p0 = v0 + v7;
                p7 = v0 - v7;
                p1 = v1 + v6;
                p6 = v1 - v6;
                p2 = v2 + v5;
                p5 = v2 - v5;
                p3 = v3 + v4;
                p4 = v3 - v4;

                // convert to 8-bit integers
                p0 = (p0 < 16) ? 0 : (p0 >= 4080) ? MaxJSample : p0 >> 4;
                p1 = (p1 < 16) ? 0 : (p1 >= 4080) ? MaxJSample : p1 >> 4;
                p2 = (p2 < 16) ? 0 : (p2 >= 4080) ? MaxJSample : p2 >> 4;
                p3 = (p3 < 16) ? 0 : (p3 >= 4080) ? MaxJSample : p3 >> 4;
                p4 = (p4 < 16) ? 0 : (p4 >= 4080) ? MaxJSample : p4 >> 4;
                p5 = (p5 < 16) ? 0 : (p5 >= 4080) ? MaxJSample : p5 >> 4;
                p6 = (p6 < 16) ? 0 : (p6 >= 4080) ? MaxJSample : p6 >> 4;
                p7 = (p7 < 16) ? 0 : (p7 >= 4080) ? MaxJSample : p7 >> 4;

                // store block data
                Unsafe.Add(ref blockDataRef, col) = Unsafe.As<int, short>(ref Unsafe.AsRef(p0));
                Unsafe.Add(ref blockDataRef, c8) = Unsafe.As<int, short>(ref Unsafe.AsRef(p1));
                Unsafe.Add(ref blockDataRef, c16) = Unsafe.As<int, short>(ref Unsafe.AsRef(p2));
                Unsafe.Add(ref blockDataRef, c24) = Unsafe.As<int, short>(ref Unsafe.AsRef(p3));
                Unsafe.Add(ref blockDataRef, c32) = Unsafe.As<int, short>(ref Unsafe.AsRef(p4));
                Unsafe.Add(ref blockDataRef, c40) = Unsafe.As<int, short>(ref Unsafe.AsRef(p5));
                Unsafe.Add(ref blockDataRef, c48) = Unsafe.As<int, short>(ref Unsafe.AsRef(p6));
                Unsafe.Add(ref blockDataRef, c56) = Unsafe.As<int, short>(ref Unsafe.AsRef(p7));
            }
        }
    }
}