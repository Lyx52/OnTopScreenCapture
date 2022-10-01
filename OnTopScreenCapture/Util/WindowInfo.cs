using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace OnTopCapture.Util
{
    internal class WindowInfo
    {
        public List<WindowInfo> ChildWindows = new List<WindowInfo>();

        public string ClassName = string.Empty;

        public string Caption = string.Empty;

        public IntPtr Handle = IntPtr.Zero;

        public WindowInfo Parent;

        public uint ProcessId = 0;

        public Process Process;
    }
}
