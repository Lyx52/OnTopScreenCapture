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

using OnTopCapture.Capture.Composition;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Security.Policy;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Services.Maps;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;
using Size = System.Windows.Size;
namespace OnTopCapture.Capture
{
    public class CaptureCompositor : IDisposable
    {
        private Compositor ContentCompositor;

        /// <summary>
        /// Root visual container, contains all visual items
        /// </summary>
        private ContainerVisual RootContainer;

        /// <summary>
        /// Sprite that is drawn onto
        /// </summary>
        private SpriteVisual ContentSprite;

        /// <summary>
        /// Brush that draws the content
        /// </summary>
        private CompositionSurfaceBrush ContentBrush;

        /// <summary>
        /// Graphics device which is used to capture the frames
        /// </summary>
        private IDirect3DDevice GraphicsDevice;

        /// <summary>
        /// Instance of screen capture
        /// </summary>
        private ScreenCapture Capture;
        
        private Size WindowSize { get; set; }
        private Size LastItemSize { get; set; }
        private CaptureArea CurrentCaptureArea { get; set; } = null;

        public CaptureCompositor(Compositor c, Size windowSize)
        {
            WindowSize = windowSize;
            ContentCompositor = c;
            GraphicsDevice = Direct3D11Helper.CreateDevice();

            // Setup the root.
            RootContainer = ContentCompositor.CreateContainerVisual();
            RootContainer.RelativeSizeAdjustment = Vector2.One;

            // Setup the content.
            ContentBrush = ContentCompositor.CreateSurfaceBrush();

            // Fill the content to the visual size
            ContentBrush.Stretch = CompositionStretch.Fill;

            // Create new sprite visual to draw to 
            ContentSprite = ContentCompositor.CreateSpriteVisual();
            ContentSprite.RelativeSizeAdjustment = Vector2.One;
            ContentSprite.Brush = ContentBrush;
            RootContainer.Children.InsertAtTop(ContentSprite);
        }

        public Visual Visual => RootContainer;
        public double Opacity
        {
            set => RootContainer.Opacity = (float)value;
        }

        public void Dispose()
        {
            StopCapture();
            ContentCompositor = null;
            RootContainer.Dispose();
            ContentSprite.Dispose();
            ContentBrush.Dispose();
            GraphicsDevice.Dispose();
        }
        /// <summary>
        /// Start capturing frames from a graphics capture source
        /// </summary>
        /// <param name="item"></param>
        public void StartCaptureFromItem(GraphicsCaptureItem item, CaptureArea area = null)
        {
            // Start capturing source graphics item
            StopCapture();

            // Create screen capture instance for that specific item
            Capture = new ScreenCapture(GraphicsDevice, item, area);
            ContentBrush.Surface = Capture.CreateSurface(ContentCompositor);

            // Clip content
            CurrentCaptureArea = area;
            LastItemSize = new Size(item.Size.Width, item.Size.Height);
            SetAreaClip(CurrentCaptureArea, LastItemSize);
            Capture.StartCapture();
        }

        public void SetAreaClip(CaptureArea area, Size sourceSize)
        {
            if (area is CaptureArea)
            {
                // Relative start X/Y scaled to capture window size
                var relLeft = (double)area.XOffset / sourceSize.Width;
                var relTop = (double)area.YOffset / sourceSize.Height;
                var relSX = (int)(WindowSize.Width * relLeft);
                var relSY = (int)(WindowSize.Height * relTop);

                // Relative end X/Y scaled to capture window size
                var relRight = (double)(area.XOffset + area.Width) / sourceSize.Width;
                var relBottom = (double)(area.YOffset + area.Height) / sourceSize.Height;
                var relEX = (int)(WindowSize.Width - (WindowSize.Width * relRight));
                var relEY = (int)(WindowSize.Height - (WindowSize.Height * relBottom));

                // Negative offset to hide the start x/y clip
                ContentSprite.Offset = new Vector3(-relSX, -relSY, 0);

                // Size to end + start relative to window size + 1
                ContentSprite.RelativeSizeAdjustment = new Vector2(1f + ((float)(relEX + relSX) / (float)WindowSize.Width), 1f + ((float)(relEY + relSX) / (float)WindowSize.Height));

                // Clip source content
                ContentSprite.Clip = ContentCompositor.CreateInsetClip(relSX, relSY, relEX, relEY);
            }
        }

        /// <summary>
        /// Stop current capture
        /// </summary>
        public void StopCapture()
        {
            CurrentCaptureArea = null;
            Capture?.Dispose();
            ContentBrush.Surface = null;
        }
        public void SetWindowSize(Size windowSize)
        {
            WindowSize = windowSize;
            SetAreaClip(CurrentCaptureArea, LastItemSize);
        }
    }
}
