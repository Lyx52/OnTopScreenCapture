using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Globalization;

namespace OnTopCapture
{
    public class SelectionBackElement : FrameworkElement
    {
        private DrawingVisual SelectionVisual;
        private Boolean IsDrawing;
        private Point DrawStart;
        private Brush SelectionBrush;
        private Pen SelectionPen;
        private FrameworkElement Parent;
        public Rect LastDrawnRect { get; private set; }
        public SelectionBackElement(FrameworkElement parent)
        {
            SelectionBrush = new SolidColorBrush(Colors.Yellow);
            SelectionPen = new Pen(new SolidColorBrush(Colors.Yellow), 1);
            SelectionVisual = new DrawingVisual();
            this.AddVisualChild(SelectionVisual);

            this.Parent = parent;
            parent.MouseDown += MouseDownHandler;
            parent.MouseMove += MouseMoveHandler;
            parent.MouseUp += MouseUpHandler;
        }

        protected override Int32 VisualChildrenCount
        {
            get { return 1; }
        }

        protected override Visual GetVisualChild(Int32 index)
        {
            return SelectionVisual;
        }

        private void MouseDownHandler(Object sender, MouseButtonEventArgs e)
        {
            DrawStart = e.GetPosition(this);
            IsDrawing = true;
        }

        private void MouseMoveHandler(Object sender, MouseEventArgs e)
        {
            if (IsDrawing && e.LeftButton == MouseButtonState.Pressed)
            {
                Point endPoint = e.GetPosition(this);
                DrawSelection(SelectionBrush, SelectionPen, DrawStart, endPoint);
            }
        }

        private void MouseUpHandler(Object sender, MouseButtonEventArgs e)
        {
            DrawStart = e.GetPosition(this);
            IsDrawing = false;
        }

        private void DrawSelection(Brush fill, Pen pen, Point startPoint, Point endPoint)
        {
            Vector vector = endPoint - startPoint;
            if (vector.Length > 10)
            {
                using (DrawingContext dc = SelectionVisual.RenderOpen())
                {
                    LastDrawnRect = new Rect(startPoint, endPoint);
                    dc.DrawGeometry(fill, pen, new RectangleGeometry(LastDrawnRect));
                }
            }
        }
    }
}
