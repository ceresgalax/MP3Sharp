﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MP3Sharp.Decoding.Decoders.LayerIII
{
    internal class SBI
    {
        public int[] l;
        public int[] s;

        public SBI()
        {
            l = new int[23];
            s = new int[14];
        }

        public SBI(int[] thel, int[] thes)
        {
            l = thel;
            s = thes;
        }
    }
}