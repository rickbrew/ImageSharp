// Copyright (c) Six Labors and contributors.
// Licensed under the GNU Affero General Public License, Version 3.

namespace SixLabors.ImageSharp.Formats.Bmp
{
    /// <summary>
    /// Defines possible options, how skipped pixels during decoding of run length encoded bitmaps should be treated.
    /// </summary>
    public enum RleSkippedPixelHandling : int
    {
        /// <summary>
        /// Undefined pixels should be black. This is the default behavior and equal to how System.Drawing handles undefined pixels.
        /// </summary>
        Black = 0,

        /// <summary>
        /// Undefined pixels should be transparent.
        /// </summary>
        Transparent = 1,

        /// <summary>
        /// Undefined pixels should have the first color of the palette.
        /// </summary>
        FirstColorOfPalette = 2
    }
}
