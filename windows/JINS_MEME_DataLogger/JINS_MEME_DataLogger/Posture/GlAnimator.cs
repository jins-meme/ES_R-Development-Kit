using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

using Tao.OpenGl;
using Tao.Platform.Windows;
using Tao.FreeGlut;

namespace JINS_MEME_DataLogger
{
    public class GlAnimator : GlAnimatorBase
    {
        /// <summary>排他用MUTEX。</summary>
        private static Mutex mutex = new Mutex(false);

        private float R2D = 57.2957795131F;     //conversion parameter for radian to degree

        float[] ResetQuat = new float[]
        {
            1, 0, 0, 0
        };


        /// <summary>
        /// クォータニオン値
        /// </summary>
        private float[] quat;
        public float[] Quat
        {
            get { return quat; }
            set 
            {
                float[] mlQuaternion = value;
                quat = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    if (mlQuaternion[i] > 32767)
                    {
                        mlQuaternion[i] -= 65536;
                    }
                    quat[i] = ((float)mlQuaternion[i]) / 16384.0f;
                }
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public GlAnimator()
        {
        }

        protected void LoadGLTextures(ref int texture, Bitmap textureImage)
        {
            // Rectangle For Locking The Bitmap In Memory
            Rectangle rectangle = new Rectangle(0, 0, textureImage.Width, textureImage.Height);
            // Get The Bitmap's Pixel Data From The Locked Bitmap
            //BitmapData bitmapData = textureImage.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData bitmapData = textureImage.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            // Create Linear Filtered Texture
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, texture);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
            //Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, textureImage.Width, textureImage.Height, 0, Gl.GL_BGR, Gl.GL_UNSIGNED_BYTE, bitmapData.Scan0);
            //Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, textureImage.Width, textureImage.Height, 0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, bitmapData.Scan0);
            // これを使うと2のｎ乗でないサイズの画像も読める
            Glu.gluBuild2DMipmaps(Gl.GL_TEXTURE_2D, Gl.GL_RGBA, textureImage.Width, textureImage.Height, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, bitmapData.Scan0);

            if (textureImage != null)
            {                            // If Texture Exists
                textureImage.UnlockBits(bitmapData);              // Unlock The Pixel Data From Memory
            }

        }



        /// <summary>
        ///     Draws everything.
        /// </summary>
        /// <returns>
        ///     Returns <c>true</c> on success, otherwise <c>false</c>.
        /// </returns>
        public override bool DrawGLScene()
        {
            try
            {
                //排他する
                mutex.WaitOne();

                float flt;

                if (quat == null || quat.Length != 4)
                {
                    return true;
                }
    
                //the light position at (0, 0, 5)
                float[] lightpos = new float[]
                {
                    0.0F, 0.0F, 5.0F, 0.0F
                };
    
                //a diffuse light source with white light
                float[] light_diffuse = new float[]
                {
                    1.0F, 1.0F, 1.0F, 1.0F
                };

                //keep the qData.quat into ResetQuat
                //ResetQuat can help adjust the direction of teapot 
                //if( flg_ResetRotate == 1 ) // if aligment button was pressed, the direction reset
                //{
                //    flg_ResetRotate = 0;
                //    memcpy(ResetQuat, qData.quat, sizeof(ResetQuat));
                //}

                Gl.glDepthMask(Gl.GL_TRUE);
                Gl.glEnable(Gl.GL_COLOR_MATERIAL);
                Gl.glEnable(Gl.GL_LIGHTING);
                //give a diffuse light source with white light and at (0, 0, 5)
                Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_DIFFUSE, light_diffuse);
                Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_POSITION, lightpos);
                Gl.glEnable(Gl.GL_LIGHT0);

                Gl.glShadeModel(Gl.GL_SMOOTH);

                Gl.glDisable(Gl.GL_TEXTURE_2D);
                Gl.glDisable(Gl.GL_BLEND);

                //set the Projection matrix
                Gl.glMatrixMode(Gl.GL_PROJECTION);
                Gl.glLoadIdentity();
                //use perspective view
                Glu.gluPerspective(45.0f, 1.0f, 0.1f, 100.0f);

                //set the ModelView matrix
                Gl.glMatrixMode(Gl.GL_MODELVIEW);
                Gl.glLoadIdentity();
                //set the observation position
                Glu.gluLookAt(0, 0, 3, 0, 0, 0, 0, 1, 0);

                Gl.glClearColor(0.0f, 0.0f, 0.0f, 0.0f);
                Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
                Gl.glColor4f(0.7f, 0.7f, 1.0f, 1.0f);

                //----------------------------------------------------------------------------------
                //The direction of coordinate systems for OpenGL and Invensense are different
                //In OpenGL, the X axis is toward right on Screen, the Y axis is toward up
                //and the Z axis is from the center of the screen towards the viewer
                //In Invensesen definition, the X axis is toward right on motion device, the Y axis is toward front
                //and the Z axis is toward up
                //So, we have to transfer the direction of Invensense coordinate system to OpenGL system
    
                //In order to transfer coordinate system, have to choose a desired direction of the motion devie at first.
                //Assume the motino device puts on the table which is perpendicular with the screen, and the front of the motion device is toward screen
                //So, can have the following relation    
                //    Invensense              transfer result              OpenGL
                //        X(right)                  X(right)                 X(right)
                //        Y(to screen)              Z(up)                    Y(up)
                //        Z(up)                     -Y(from screen)          Z(from screen)
                //----------------------------------------------------------------------------------
    
                //the quaternion have four elements
                //the first element, quat[0], represents a rotation angle after using acos()
                //the remaining elememts quat[1], quat[2], quat[3] represnet the the rotation axis of X-Y-Z coordinate system

                //rotate teapot along Y axis, the rotation angle depends on ResetQuat which is used to adjust the direction
                Gl.glRotatef(-mEULAR_ANGLE_Z_FROM_QUAT_YXZ_CONVNETION(ResetQuat) * R2D, 0, 1, 0);

                //compute the rotation angle from quat[0] 
                flt = (float) Math.Acos(quat[0]);
    
                //void glRotatef( GLfloat angle, GLfloat x, GLfloat y, GLfloat z); 
                //rotate object "angle" degrees along (x, y, z)
                //should transfer the Invensense coordinate system to OpenGL coordinate system for the rotation axis (x,y,z)
                //OpenGL X = Invensense X <=> quat[1]
                //OpenGL Y = Invensense Z <=> quat[3]
                //OpenGL Z = Invensense -Y <=> -quat[2]
                Gl.glRotatef((float)(2* flt * 180 / 3.1415), (float) quat[1],
                          (float) quat[3], (float) - quat[2]);

                //rotate teapot 90 degree along Y axis, let the direction of spout equal the front of the motion device
                Gl.glRotatef(90, 0, 1, 0);

                //draw a teapot by OpengGL library
                Glut.glutSolidTeapot(0.6);

            
                // この２行を入れないと描画しない
                Gl.glFlush();                                                       // Flush The GL Pipeline
                Gdi.SwapBuffers(hDC);


                //Gl.glDisable(Gl.GL_ALPHA_TEST);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                // Mutexロック解放
                mutex.ReleaseMutex();
            }

            return true;
        }

        private float mEULAR_ANGLE_Z_FROM_QUAT_YXZ_CONVNETION(float[] q)
        {
            return (float)Math.Atan2( -2 * (q[1]*q[2]-q[0]*q[3]), 1-2*(q[1]*q[1]+q[3]*q[3])) ;
        }


    }
}
