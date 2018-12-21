﻿using System;

namespace DataTool.Flag {
    [AttributeUsage(AttributeTargets.Field)]
    public class CLIFlagAttribute : Attribute {
        public object   Default    = null;
        public string   Flag       = null;
        public string   Help       = null;
        public bool     NeedsValue = false;
        public string[] Parser     = null;
        public int      Positional = -1;
        public bool     Required   = false;
        public string[] Valid      = null;

        public new string ToString() { return Flag; }
    }
}
