using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using OnTopCapture.Utils.Enums;
using Windows.Foundation;
using static OnTopCapture.Utils.ExternalApi;
namespace OnTopCapture.Utils
{
    static class WindowHelper
    {
        /// <summary>
        /// Set window as transparent
        /// </summary>
        /// <param name="hwnd">Handle to window</param>
        /// <param name="enabled">Is transparency enabled</param>
        public static void SetWindowExTransparent(IntPtr hwnd, bool enabled)
        {
            var extendedStyle = GetWindowLong(hwnd, (int)GWL.GWL_EXSTYLE);
            SetWindowLong(hwnd, (int)GWL.GWL_EXSTYLE, enabled ? extendedStyle | (int)WindowStyles.WS_EX_TRANSPARENT : extendedStyle & ~(int)WindowStyles.WS_EX_TRANSPARENT);
        }
        /// <summary>
        /// Get window specific value from extra window memory
        /// </summary>
        /// <param name="hWnd">Handle to window</param>
        /// <param name="type">Type of long value to get</param>
        /// <returns></returns>
        static IntPtr GetWindowLongPtr(IntPtr hWnd, GWL type)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, (int)type);
            else
                return GetWindowLongPtr32(hWnd, (int)type);
        }

        /// <summary>
        /// Checks if source window is valid for screen capture
        /// </summary>
        /// <param name="hwnd">Handle to window</param>
        /// <returns></returns>
        public static bool IsWindowValidForCapture(IntPtr hwnd)
        {
            // Invalid handle
            if (hwnd.ToInt32() == 0)
                return false;

            // Is Win32 shell window
            if (hwnd == GetShellWindow())
                return false;

            // Window is invisible
            if (!IsWindowVisible(hwnd))
                return false;

            // Window is not root window from a group of windows
            if (GetAncestor(hwnd, GetAncestorFlags.GetRoot) != hwnd)
                return false;

            // Window handle is equal to current process window handle
            if (hwnd == Process.GetCurrentProcess().MainWindowHandle)
                return false;

            // Window is set as "Cloaked"
            var hrTemp = DwmGetWindowAttribute(hwnd, DWMWindowAttribute.Cloaked, out bool cloaked, Marshal.SizeOf<bool>());
            if (hrTemp == 0 && cloaked)
                return false;

            // Window is has popup style
            var style = (WindowStyles)GetWindowLongPtr(hwnd, GWL.GWL_STYLE).ToInt64();
            if (style.HasFlag(WindowStyles.WS_POPUP) && !style.HasFlag(WindowStyles.WS_POPUPWINDOW))
                return false;

            return true;
        }
        /// <summary>
        /// Get info about the window
        /// </summary>
        /// <param name="hwnd">Handle to window</param>
        /// <returns></returns>
        public static WindowInfo GetWindowInfo(IntPtr hwnd)
        {
            StringBuilder caption = new StringBuilder(1024);
            StringBuilder className = new StringBuilder(1024);

            GetWindowText(hwnd, caption, caption.Capacity);
            GetClassName(hwnd, className, className.Capacity);
            GetWindowThreadProcessId(hwnd, out uint processId);

            WindowInfo info = new WindowInfo()
            {
                Handle = hwnd,
                ClassName = className.ToString(),
                ProcessId = processId,
                Process = Process.GetProcessById((int)processId)
            };

            // Get title or caption from the window
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

        /// <summary>
        /// Get child window handles from window group parent
        /// </summary>
        /// <param name="parentHwnd">Parent of the window group</param>
        /// <returns></returns>
        public static List<IntPtr> GetChildWindowHandles(IntPtr parentHwnd)
        {
            var output = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(output);
            try
            {
                EnumChildWindows(parentHwnd, EnumWindow, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }

            return output;
        }
        /// <summary>
        /// Get child windows from window group parent
        /// </summary>
        /// <param name="parent">Parent of window group</param>
        /// <returns></returns>
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
        /// <summary>
        /// Query currently opened windows
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Get currently available monitors
        /// </summary>
        /// <returns></returns>
        public static List<MonitorInfo> GetMonitors()
        {
            var result = new List<MonitorInfo>(); 
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
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
