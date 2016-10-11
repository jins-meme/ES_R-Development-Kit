using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

using Tao.OpenGl;
using Tao.Platform.Windows;
using Tao.FreeGlut;

namespace JINS_MEME_DataLogger
{
    public class GlAnimatorBase : IDisposable
    {
        // --- Fields ---
        protected IntPtr hDC;                                              // Private GDI Device Context
        protected IntPtr hRC;                                              // Permanent Rendering Context
        protected Control parent;                                               // Our Current Windows Form
        protected Color backColor = Color.FromArgb(255, 0, 0, 0);

        public bool StartMove { get; set; }
        public bool Selected { get; set; }

        public bool InitGLWindow(Control parent, int width, int height, int bits)
        {
            int pixelFormat;                                                    // Holds The Results After Searching For A Match

            this.parent = parent;

            GC.Collect();                                                       // Request A Collection
            // This Forces A Swap
            Kernel.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);


            Gdi.PIXELFORMATDESCRIPTOR pfd = new Gdi.PIXELFORMATDESCRIPTOR();    // pfd Tells Windows How We Want Things To Be
            pfd.nSize = (short)Marshal.SizeOf(pfd);                            // Size Of This Pixel Format Descriptor
            pfd.nVersion = 1;                                                   // Version Number
            pfd.dwFlags = Gdi.PFD_DRAW_TO_WINDOW |                              // Format Must Support Window
                Gdi.PFD_SUPPORT_OPENGL |                                        // Format Must Support OpenGL
                Gdi.PFD_DOUBLEBUFFER;                                           // Format Must Support Double Buffering
            pfd.iPixelType = (byte)Gdi.PFD_TYPE_RGBA;                          // Request An RGBA Format
            pfd.cColorBits = (byte)bits;                                       // Select Our Color Depth
            pfd.cRedBits = 0;                                                   // Color Bits Ignored
            pfd.cRedShift = 0;
            pfd.cGreenBits = 0;
            pfd.cGreenShift = 0;
            pfd.cBlueBits = 0;
            pfd.cBlueShift = 0;
            pfd.cAlphaBits = 0;                                                 // No Alpha Buffer
            pfd.cAlphaShift = 0;                                                // Shift Bit Ignored
            pfd.cAccumBits = 0;                                                 // No Accumulation Buffer
            pfd.cAccumRedBits = 0;                                              // Accumulation Bits Ignored
            pfd.cAccumGreenBits = 0;
            pfd.cAccumBlueBits = 0;
            pfd.cAccumAlphaBits = 0;
            pfd.cDepthBits = 16;                                                // 16Bit Z-Buffer (Depth Buffer)
            pfd.cStencilBits = 0;                                               // No Stencil Buffer
            pfd.cAuxBuffers = 0;                                                // No Auxiliary Buffer
            pfd.iLayerType = (byte)Gdi.PFD_MAIN_PLANE;                         // Main Drawing Layer
            pfd.bReserved = 0;                                                  // Reserved
            pfd.dwLayerMask = 0;                                                // Layer Masks Ignored
            pfd.dwVisibleMask = 0;
            pfd.dwDamageMask = 0;

            hDC = User.GetDC(parent.Handle);                                      // Attempt To Get A Device Context
            if (hDC == IntPtr.Zero)
            {                                            // Did We Get A Device Context?
                KillGLWindow();                                                 // Reset The Display
                MessageBox.Show("Can't Create A GL Device Context.", "ERROR",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            pixelFormat = Gdi.ChoosePixelFormat(hDC, ref pfd);                  // Attempt To Find An Appropriate Pixel Format
            if (pixelFormat == 0)
            {                                              // Did Windows Find A Matching Pixel Format?
                KillGLWindow();                                                 // Reset The Display
                MessageBox.Show("Can't Find A Suitable PixelFormat.", "ERROR",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!Gdi.SetPixelFormat(hDC, pixelFormat, ref pfd))
            {                // Are We Able To Set The Pixel Format?
                KillGLWindow();                                                 // Reset The Display
                MessageBox.Show("Can't Set The PixelFormat.", "ERROR",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            hRC = Wgl.wglCreateContext(hDC);                                    // Attempt To Get The Rendering Context
            if (hRC == IntPtr.Zero)
            {                                            // Are We Able To Get A Rendering Context?
                KillGLWindow();                                                 // Reset The Display
                MessageBox.Show("Can't Create A GL Rendering Context.", "ERROR",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!Wgl.wglMakeCurrent(hDC, hRC))
            {                                 // Try To Activate The Rendering Context
                KillGLWindow();                                                 // Reset The Display
                MessageBox.Show("Can't Activate The GL Rendering Context.", "ERROR",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            ReSizeGLScene(width, height);                                       // Set Up Our Perspective GL Screen

            if (!InitGL())
            {                                                     // Initialize Our Newly Created GL Window
                KillGLWindow();                                                 // Reset The Display
                MessageBox.Show("Initialization Failed.", "ERROR",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;                                                        // Everything Went OK
        }

        public virtual void Dispose()
        {
            KillGLWindow();
        }


        /// <summary>
        ///     Draws everything.
        /// </summary>
        /// <returns>
        ///     Returns <c>true</c> on success, otherwise <c>false</c>.
        /// </returns>
        public virtual bool DrawGLScene()
        {
            return true;
        }

        /// <summary>
        ///     All setup for OpenGL goes here.
        /// </summary>
        /// <returns>
        ///     Returns <c>true</c> on success, otherwise <c>false</c>.
        /// </returns>
        protected bool InitGL()
        {                                                                       // All Setup For OpenGL Goes Here
            Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
            Gl.glEnable(Gl.GL_TEXTURE_2D);                                      // Enable Texture Mapping ( NEW )
            Gl.glShadeModel(Gl.GL_SMOOTH);                                      // Enable Smooth Shading
            Gl.glClearColor((float)backColor.R / 256.0F, (float)backColor.G / 256.0F, (float)backColor.B / 256.0F, (float)backColor.A / 256.0F);
            //Gl.glClearColor(0, 0, 0, 0.5f);                                     // Black Background
            Gl.glClearDepth(1);                                                 // Depth Buffer Setup
            Gl.glEnable(Gl.GL_DEPTH_TEST);                                      // Enables Depth Testing
            Gl.glDepthFunc(Gl.GL_LEQUAL);                                       // The Type Of Depth Testing To Do
            Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);         // Really Nice Perspective Calculations

            return true;                                                        // Initialization Went OK
        }

        ///// <summary>
        /////     Properly kill the window.
        ///// </summary>
        protected void KillGLWindow()
        {
            if (hRC != IntPtr.Zero)
            {                                            // Do We Have A Rendering Context?
                if (!Wgl.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero))
                {             // Are We Able To Release The DC and RC Contexts?
                    MessageBox.Show("Release Of DC And RC Failed.", "SHUTDOWN ERROR",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (!Wgl.wglDeleteContext(hRC))
                {                                // Are We Able To Delete The RC?
                    MessageBox.Show("Release Rendering Context Failed.", "SHUTDOWN ERROR",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                hRC = IntPtr.Zero;                                              // Set RC To Null
            }

            if (hDC != IntPtr.Zero)
            {                                            // Do We Have A Device Context?
                if (parent != null && !parent.IsDisposed)
                {                          // Do We Have A Window?
                    if (parent.Handle != IntPtr.Zero)
                    {                            // Do We Have A Window Handle?
                        if (!User.ReleaseDC(parent.Handle, hDC))
                        {                 // Are We Able To Release The DC?
                            MessageBox.Show("Release Device Context Failed.", "SHUTDOWN ERROR",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }

                hDC = IntPtr.Zero;                                              // Set DC To Null
            }

        }

        /// <summary>
        ///     Resizes and initializes the GL window.
        /// </summary>
        /// <param name="width">
        ///     The new window width.
        /// </param>
        /// <param name="height">
        ///     The new window height.
        /// </param>
        public void ReSizeGLScene(int width, int height)
        {
            if (height == 0)
            {                                                   // Prevent A Divide By Zero...
                height = 1;                                                     // By Making Height Equal To One
            }

            Gl.glViewport(0, 0, width, height);                                 // Reset The Current Viewport
            Gl.glMatrixMode(Gl.GL_PROJECTION);                                  // Select The Projection Matrix
            Gl.glLoadIdentity();                                                // Reset The Projection Matrix
            Glu.gluPerspective(45, width / (double)height, 0.1, 100);          // Calculate The Aspect Ratio Of The Window
            Gl.glMatrixMode(Gl.GL_MODELVIEW);                                   // Select The Modelview Matrix
            Gl.glLoadIdentity();                                                // Reset The Modelview Matrix
        }

        /// <summary>
        ///     Load bitmaps and convert to textures.
        /// </summary>
        /// <returns>
        ///     <c>true</c> on success, otherwise <c>false</c>.
        /// </returns>
        protected bool LoadGLTextures(ref int texture, Bitmap textureImage)
        {
            bool status = false;                                                // Status Indicator

            // Check For Errors, If Bitmap's Not Found, Quit
            if (textureImage != null)
            {
                status = true;                                                  // Set The Status To True

                // Rectangle For Locking The Bitmap In Memory
                Rectangle rectangle = new Rectangle(0, 0, textureImage.Width, textureImage.Height);
                // Get The Bitmap's Pixel Data From The Locked Bitmap
                BitmapData bitmapData = textureImage.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                // Typical Texture Generation Using Data From The Bitmap
                Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
                //Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB8, textureImage[0].Width, textureImage[0].Height, 0, Gl.GL_BGR, Gl.GL_UNSIGNED_BYTE, bitmapData.Scan0);
                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
                Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);

                // これを使うと2のｎ乗でないサイズの画像も読める
                Glu.gluBuild2DMipmaps(Gl.GL_TEXTURE_2D, Gl.GL_RGBA, textureImage.Width, textureImage.Height, Gl.GL_BGR, Gl.GL_UNSIGNED_BYTE, bitmapData.Scan0);

                if (textureImage != null)
                {                                   // If Texture Exists
                    textureImage.UnlockBits(bitmapData);                     // Unlock The Pixel Data From Memory
                }
            }

            return status;                                                      // Return The Status
        }

    }
}
