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
using System.Numerics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Composition;

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

        public CaptureCompositor(Compositor c)
        {
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
        public void StartCaptureFromItem(GraphicsCaptureItem item)
        {
            // Start capturing source graphics item
            StopCapture();

            // Create screen capture instance for that specific item
            Capture = new ScreenCapture(GraphicsDevice, item);
            ContentBrush.Surface = Capture.CreateSurface(ContentCompositor);
            Capture.StartCapture();
        }
        /// <summary>
        /// Stop current capture
        /// </summary>
        public void StopCapture()
        {
            Capture?.Dispose();
            ContentBrush.Surface = null;
        }
    }
}
