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
using SharpDX.DXGI;
using System;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Composition;

namespace OnTopCapture.Capture
{
    public class ScreenCapture : IDisposable
    {
        /// <summary>
        /// Graphics source that is being captured
        /// </summary>
        private GraphicsCaptureItem item;

        /// <summary>
        /// Frames captured from the application
        /// </summary>
        private Direct3D11CaptureFramePool framePool;

        /// <summary>
        /// Current screen capture session
        /// </summary>
        private GraphicsCaptureSession session;

        /// <summary>
        /// Last known window size
        /// </summary>
        private SizeInt32 lastSize;

        private IDirect3DDevice device;

        /// <summary>
        /// D3D11 graphics device instance that is bound to swapchain
        /// </summary>
        private SharpDX.Direct3D11.Device d3dDevice;

        /// <summary>
        /// Swapchain that is used to transfer captures frames onto
        /// </summary>
        private SharpDX.DXGI.SwapChain1 swapChain;

        public ScreenCapture(IDirect3DDevice d, GraphicsCaptureItem app, CaptureArea area)
        {
            item = app;
            device = d;
            d3dDevice = Direct3D11Helper.CreateSharpDXDevice(device);

            // Create swapchain that captures the application window or primary screen
            var dxgiFactory = new SharpDX.DXGI.Factory2();
            var description = new SharpDX.DXGI.SwapChainDescription1()
            {
                Width = item.Size.Width,
                Height = item.Size.Height,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SharpDX.DXGI.SampleDescription()
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                BufferCount = 2,
                Scaling = SharpDX.DXGI.Scaling.Stretch,
                SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,
                AlphaMode = SharpDX.DXGI.AlphaMode.Premultiplied,
                Flags = SharpDX.DXGI.SwapChainFlags.None
            };
            swapChain = new SharpDX.DXGI.SwapChain1(dxgiFactory, d3dDevice, ref description);

            // Create framepool
            framePool = Direct3D11CaptureFramePool.Create(
                device,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                item.Size);
            session = framePool.CreateCaptureSession(item);
            lastSize = item.Size;

            // Attach frame handler function to framepool
            framePool.FrameArrived += OnFrameArrived;
        }

        public void Dispose()
        {
            session?.Dispose();
            framePool?.Dispose();
            swapChain?.Dispose();
            d3dDevice?.Dispose();
        }


        /// <summary>
        /// Start screen capture session
        /// </summary>
        public void StartCapture()
        {
            session.StartCapture();
        }

        /// <summary>
        /// Create capture surface that is bound to a swapchain
        /// </summary>
        /// <param name="compositor">Current visual compositor</param>
        /// <returns></returns>
        public ICompositionSurface CreateSurface(Compositor compositor)
        {
            return compositor.CreateCompositionSurfaceForSwapChain(swapChain);
        }

        /// <summary>
        /// Copy frame from source to swapchain
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            // Window size has changed
            var sizeChanged = false;

            using (var frame = sender.TryGetNextFrame())
            {
                if (frame.ContentSize.Width != lastSize.Width ||
                    frame.ContentSize.Height != lastSize.Height)
                {
                    // The thing we have been capturing has changed size.
                    // We need to resize the swap chain first, then blit the pixels.
                    // After we do that, retire the frame and then recreate the frame pool.
                    sizeChanged = true;
                    lastSize = frame.ContentSize;
                    swapChain.ResizeBuffers(
                        2, 
                        lastSize.Width, 
                        lastSize.Height, 
                        SharpDX.DXGI.Format.B8G8R8A8_UNorm, 
                        SharpDX.DXGI.SwapChainFlags.None);
                }
                
                // Get frame from first framebuffer
                using (var backBuffer = swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0))
                using (var bitmap = Direct3D11Helper.CreateSharpDXTexture2D(frame.Surface))
                {
                    d3dDevice.ImmediateContext.CopyResource(bitmap, backBuffer);
                }

            } // Retire the frame.

            swapChain.Present(0, SharpDX.DXGI.PresentFlags.None);
            // If size has changed we need to recreate framepool according to new size
            if (sizeChanged)
            {
                framePool.Recreate(
                    device,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    2,
                    lastSize);
            }
        }
    }
}
