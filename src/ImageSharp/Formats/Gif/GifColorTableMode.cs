﻿// Copyright (c) Six Labors and contributors.
// Licensed under the GNU Affero General Public License, Version 3.

namespace SixLabors.ImageSharp.Formats.Gif
{
    /// <summary>
    /// Provides enumeration for the available color table modes.
    /// </summary>
    public enum GifColorTableMode
    {
        /// <summary>
        /// A single color table is calculated from the first frame and reused for subsequent frames.
        /// </summary>
        Global,

        /// <summary>
        /// A unique color table is calculated for each frame.
        /// </summary>
        Local
    }
}
