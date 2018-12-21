﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace TankLib.Math {
    /// <summary>2 component XY vector</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DebuggerDisplay("X: {X}, Y: {Y}")]
    public struct teVec2 {
        /// <summary>X component</summary>
        public float X;

        /// <summary>Y component</summary>
        public float Y;

        public teVec2(float x, float y) {
            X = x;
            Y = y;
        }

        public static teVec2 FromHalf(IReadOnlyList<ushort> val) {
            if (val.Count != 2) throw new InvalidDataException();
            var x = Half.Half.ToHalf(val[0]);
            var y = Half.Half.ToHalf(val[1]);
            return new teVec2(x, y);
        }
    }
}
