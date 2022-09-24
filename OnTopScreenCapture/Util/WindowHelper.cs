//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using OnTopCapture.Util.Enums;
using Windows.Foundation;
using static OnTopCapture.Util.ExternalApi;
namespace OnTopCapture.Util
{
    static class WindowHelper
    {
        public static void SetWindowExTransparent(IntPtr hwnd, bool enabled)
        {
            var extendedStyle = GetWindowLong(hwnd, (int)GWL.GWL_EXSTYLE);
            SetWindowLong(hwnd, (int)GWL.GWL_EXSTYLE, enabled ? extendedStyle | (int)WindowStyles.WS_EX_TRANSPARENT : extendedStyle & ~(int)WindowStyles.WS_EX_TRANSPARENT);
        }

        static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        public static bool IsWindowValidForCapture(IntPtr hwnd)
        {
            if (hwnd.ToInt32() == 0)
            {
                return false;
            }
            if (hwnd == GetShellWindow())
            {
                return false;
            }
            if (!IsWindowVisible(hwnd))
            {
                return false;
            }

            if (GetAncestor(hwnd, GetAncestorFlags.GetRoot) != hwnd)
            {
                return false;
            }
            var style = (WindowStyles)GetWindowLongPtr(hwnd, (int)GWL.GWL_STYLE).ToInt64();
            if (style.HasFlag(WindowStyles.WS_DISABLED))
            {
                return false;
            }

            var cloaked = false;
            var hrTemp = DwmGetWindowAttribute(hwnd, DWMWindowAttribute.Cloaked, out cloaked, Marshal.SizeOf<bool>());
            if (hrTemp == 0 && cloaked)
            {
                return false;
            }

            return true;
        }
        public static IEnumerable<MonitorInfo> GetMonitors()
        {
            var result = new List<MonitorInfo>();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
                {
                    MonitorInfoEx mi = new MonitorInfoEx();
                    mi.Size = Marshal.SizeOf(mi);
                    bool success = GetMonitorInfo(hMonitor, ref mi);
                    if (success)
                    {
                        var info = new MonitorInfo
                        {
                            ScreenSize = new Vector2(mi.Monitor.right - mi.Monitor.left, mi.Monitor.bottom - mi.Monitor.top),
                            MonitorArea = new Rect(mi.Monitor.left, mi.Monitor.top, mi.Monitor.right - mi.Monitor.left, mi.Monitor.bottom - mi.Monitor.top),
                            WorkArea = new Rect(mi.WorkArea.left, mi.WorkArea.top, mi.WorkArea.right - mi.WorkArea.left, mi.WorkArea.bottom - mi.WorkArea.top),
                            IsPrimary = mi.Flags > 0,
                            Hmon = hMonitor,
                            DeviceName = mi.DeviceName
                        };
                        result.Add(info);
                    }
                    return true;
                }, IntPtr.Zero);
            return result;
        }

    }
}
