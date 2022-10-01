using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
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
            if (hwnd == Process.GetCurrentProcess().MainWindowHandle)
            {
                return false;
            }
            var hrTemp = DwmGetWindowAttribute(hwnd, DWMWindowAttribute.Cloaked, out bool cloaked, Marshal.SizeOf<bool>());
            if (hrTemp == 0 && cloaked)
            {
                return false;
            }

            var style = (WindowStyles)GetWindowLongPtr(hwnd, (int)GWL.GWL_STYLE).ToInt64();
            if (style.HasFlag(WindowStyles.WS_POPUP) && !style.HasFlag(WindowStyles.WS_POPUPWINDOW))
            {
                return false;
            }
            return true;
        }

        public static WindowInfo GetWindowInfo(IntPtr winHandle)
        {
            StringBuilder caption = new StringBuilder(1024);
            StringBuilder className = new StringBuilder(1024);

            GetWindowText(winHandle, caption, caption.Capacity);
            GetClassName(winHandle, className, className.Capacity);
            GetWindowThreadProcessId(winHandle, out uint processId);

            WindowInfo info = new WindowInfo();
            info.Handle = winHandle;
            info.ClassName = className.ToString();
            info.ProcessId = processId;
            info.Process = Process.GetProcessById((int)processId);
            if (!string.IsNullOrEmpty(caption.ToString().Trim()))
            {
                info.Caption = caption.ToString();
            }
            else
            {
                caption = new StringBuilder(Convert.ToInt32(SendMessage(info.Handle, WindowsMessage.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero)) + 1);
                SendMessage(info.Handle, WindowsMessage.WM_GETTEXT, caption.Capacity, caption);
                info.Caption = caption.ToString();
            }

            return info;
        }
        public static List<IntPtr> GetChildWindowHandles(IntPtr parent)
        {
            var result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                EnumWindowProc childProc = EnumWindow;
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }

            return result;
        }

        public static List<WindowInfo> GetChildWindowsInfo(WindowInfo parent)
        {
            var result = new List<WindowInfo>();
            IntPtr childHandle = GetWindow(parent.Handle, GetWindowCmd.GW_CHILD);
            while (childHandle != IntPtr.Zero)
            {
                WindowInfo childInfo = GetWindowInfo(childHandle);
                childInfo.Parent = parent;
                childInfo.ChildWindows = GetChildWindowsInfo(childInfo);
                result.Add(childInfo);
                childHandle = FindWindowEx(parent.Handle, childHandle, null, null);
            }

            return result;
        }
        public static List<WindowInfo> GetWindows()
        {
            IntPtr desktopWindow = GetDesktopWindow();
            var windows = new List<WindowInfo>();
            var winHandles = GetChildWindowHandles(desktopWindow);
            foreach (var handle in winHandles)
            {
                if (IsWindowValidForCapture(handle))
                {
                    var info = GetWindowInfo(handle);
                    if (!string.IsNullOrEmpty(info.Caption))
                        windows.Add(info);
                }
            }

            return windows;
        }
        public static List<MonitorInfo> GetMonitors()
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
                            Handle = hMonitor,
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
