using System;
using System.Numerics;
using Windows.Foundation;

namespace OnTopCapture.Utils
{
    /// <summary>
    /// Class that contains info about a monitor
    /// </summary>
    public sealed class MonitorInfo
    {
        public bool IsPrimary { get; set; }
        public Vector2 ScreenSize { get; set; }
        public Rect MonitorArea { get; set; }
        public Rect WorkArea { get; set; }
        public string DeviceName { get; set; }
        public IntPtr Handle { get; set; }
    }
}
