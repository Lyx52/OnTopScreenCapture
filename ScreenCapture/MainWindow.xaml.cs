using OnTopCapture.Capture;
using Composition.WindowsRuntimeHelpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.UI.Composition;
using System.Runtime.InteropServices;

namespace OnTopCapture
{

    public partial class MainWindow : Window
    {
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);
        private IntPtr WindowHandle;
        private Compositor WindowCompositor;
        private CompositionTarget TargetComposition;
        private ContainerVisual Root;
        private CaptureCompositor Compositor;
        private ObservableCollection<Process> Processes;
        private bool mIsOnTop = false;
        private bool mIsCapturing = false;
        public bool IsCapturing
        {
            get => mIsCapturing;
            set
            {
                mIsCapturing = value;
                StopCaptureButton.IsEnabled = value;
                PrimaryMonitorCaptureButton.IsEnabled = !value;
            }
        }
        public bool IsOnTop
        {
            get => mIsOnTop;
            set
            {
                mIsOnTop = value;
                this.Topmost = value;
                if (mIsOnTop)
                {
                    this.Activate();
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var interopWindow = new WindowInteropHelper(this);
            WindowHandle = interopWindow.Handle;
            InitComposition();
            QueryWindows();
        }

        private void InitComposition()
        {
            // Create the compositor.
            WindowCompositor = new Compositor();

            // Create a target for the window.
            TargetComposition = WindowCompositor.CreateDesktopWindowTarget(WindowHandle, true);

            // Attach the root visual.
            Root = WindowCompositor.CreateContainerVisual();
            Root.RelativeSizeAdjustment = Vector2.One;
            TargetComposition.Root = Root;

            // Setup the rest of the sample application.
            Compositor = new CaptureCompositor(WindowCompositor);
            Root.Children.InsertAtTop(Compositor.Visual);
        }

        private void QueryWindows()
        {
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
            {
                var processses = Process.GetProcesses().Where((process) =>
                {
                    if (string.IsNullOrWhiteSpace(process.MainWindowTitle) || process.MainWindowHandle == WindowHandle)
                        return false;
                    return WindowEnumerationHelper.IsWindowValidForCapture(process.MainWindowHandle);
                }).ToList();
                Processes = new ObservableCollection<Process>(processses);
                ProcessCaptureList.Items.Clear();
                foreach (Process p in Processes)
                {
                    var item = new MenuItem { Header = $"{p.MainWindowTitle} ({p.ProcessName} - {p.Id})" };
                    item.Click += ((s, e) => {
                        if (IsIconic(p.MainWindowHandle))
                        {
                            ShowWindow(p.MainWindowHandle, 9); // SW_RESTORE
                            this.Activate(); // Restore ontop app as topmost
                        }
                        this.StartHwndCapture(p.MainWindowHandle);
                    });
                    ProcessCaptureList.Items.Add(item);
                }
            }
        }
        private void StartHwndCapture(IntPtr hwnd)
        {
            GraphicsCaptureItem item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null)
            {
                Compositor.StartCaptureFromItem(item);
                this.IsCapturing = true;
            }
        }

        private void StartHmonCapture(IntPtr hmon)
        {
            GraphicsCaptureItem item = CaptureHelper.CreateItemForMonitor(hmon);
            if (item != null)
            {
                Compositor.StartCaptureFromItem(item);
            }
        }

        private void StartPrimaryMonitorCapture()
        {
            MonitorInfo monitor = (from m in MonitorEnumerationHelper.GetMonitors()
                           where m.IsPrimary
                           select m).First();
            StartHmonCapture(monitor.Hmon);
        }

        private void StopCapture()
        {
            this.IsCapturing = false;
            Compositor.StopCapture();
        }
        private void ProcessList_Click(object sender, RoutedEventArgs e)
        {
            this.QueryWindows();
        }
        private void WindowOnTop_Click(object sender, RoutedEventArgs e)
        {
            this.IsOnTop = WindowOnTopButton.IsChecked;
        }
        private void PrimaryMonitorCapture_Click(object sender, RoutedEventArgs e)
        {
            this.IsCapturing = true;
            this.StartPrimaryMonitorCapture();
        }

        
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.StopCapture();
            Application.Current.Shutdown();
        }
        private void StopCapturing_Click(object sender, RoutedEventArgs e)
        {
            this.StopCapture();
        }
    }
}
