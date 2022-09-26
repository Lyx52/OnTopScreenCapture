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

using Composition.WindowsRuntimeHelpers;
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
        private ContainerVisual RootContainer;

        private SpriteVisual ContentSprite;
        private CompositionSurfaceBrush ContentBrush;

        private IDirect3DDevice GraphicsDevice;
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
            ContentBrush.Stretch = CompositionStretch.Fill;
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

        public void StartCaptureFromItem(GraphicsCaptureItem item)
        {
            StopCapture();
            Capture = new ScreenCapture(GraphicsDevice, item);
            ContentBrush.Surface = Capture.CreateSurface(ContentCompositor);

            Capture.StartCapture();
        }

        public void StopCapture()
        {
            Capture?.Dispose();
            ContentBrush.Surface = null;
        }
    }
}
