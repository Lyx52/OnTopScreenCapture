using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition;

namespace OnTopCapture.Capture
{
    public sealed class CaptureArea
    {
        public int XOffset { get; set; }
        public int YOffset { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
