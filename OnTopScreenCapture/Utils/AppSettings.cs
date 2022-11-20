using OnTopCapture.Capture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnTopCapture.Utils
{
    public sealed class AppSettings
    {
        public int DefaultOpacity { get; set; } = 100;
        public bool IsOnTopByDefault { get; set; } = false;
        public bool IsHelpTextVisibleAlways { get; set; } = false;
        public bool IsCursorCapturingEnabled { get; set; } = false;
        public List<CaptureArea> SavedAreas { get; set; } = new List<CaptureArea>();
    }
}
