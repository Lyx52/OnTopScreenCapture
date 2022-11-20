using OnTopCapture.Capture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Windows.UI.Composition;

namespace OnTopCapture
{
    /// <summary>
    /// Interaction logic for AreaSelectionWindow.xaml
    /// </summary>
    public partial class AreaSelectionWindow : Window
    {
        private SelectionBackElement SelectorElement;
        public CaptureArea SelectedArea
        {
            get => new CaptureArea
            {
                XOffset = (int)SelectorElement.LastDrawnRect.Left,
                YOffset = (int)SelectorElement.LastDrawnRect.Top,
                Width = (int)SelectorElement.LastDrawnRect.Width,
                Height = (int)SelectorElement.LastDrawnRect.Height,
            };
        }
        public AreaSelectionWindow(int width, int height)
        {
            InitializeComponent();
            this.MinHeight = width;
            this.MaxHeight = height;
            this.Width = width;
            this.Height = height;
            SelectorElement = new SelectionBackElement(this);
            this.AddChild(SelectorElement);
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e) => Close();
    }
}
