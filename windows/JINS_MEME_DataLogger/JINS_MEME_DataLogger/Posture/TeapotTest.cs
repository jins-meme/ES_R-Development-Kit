using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tao.OpenGl;
using Tao.Platform.Windows;
using Tao.FreeGlut;

namespace JINS_MEME_DataLogger
{
    public class TeapotTest
    {
        private const float R2D = 57.2957795131F;
        private QuatPacket_Bean qData = null;
        private byte flg_ResetRotate = 0;
        private float[] ResetQuat = new float[4] { 1, 0, 0, 0 };
        private int teapotShwoWait = 0;

        private long[] zenQuat = new long[4];

        public TeapotTest() { }



        private float mEULAR_ANGLE_Z_FROM_QUAT_YXZ_CONVNETION(float[] q)
        {
            return (float)Math.Atan2(-2 * (q[1] * q[2] - q[0] * q[3]), 1 - 2 * (q[1] * q[1] + q[3] * q[3]));
        }

        public void Main()
        {
            Glut.glutInit();
            Glut.glutInitDisplayMode(Glut.GLUT_DOUBLE | Glut.GLUT_RGBA | Glut.GLUT_DEPTH);

            Glut.glutInitWindowSize(480, 480);
            Glut.glutInitWindowPosition(500, 0);
            Glut.glutCreateWindow("InvenSense Tea Pot Demo");

            DemoInit();
            qData = new QuatPacket_Bean();

            //Glut.glutDisplayFunc(DemoDisplayMain);
            //Glut.glutKeyboardUpFunc(DemoKeyboardUpFunc);
            Glut.glutReshapeFunc(ReshapeWindowSize);
            Glut.glutIdleFunc(DemoDisplayIdle);
            Gl.glEnable(Gl.GL_BLEND);
            DemoDisplayMain();
            Glut.glutMainLoop();

        }

        public void SetQuat(long[] qt)
        {
            qData = new QuatPacket_Bean(qt);
            //IMUgetQuaternion(&qData);
            if (zenQuat[0] != qData.MlQuaternion[0] || zenQuat[1] != qData.MlQuaternion[1] || zenQuat[2] != qData.MlQuaternion[2] || zenQuat[3] != qData.MlQuaternion[3])
            {
                teapotShwoWait += 1;
                if (teapotShwoWait >= 1)
                {
                    teapotShwoWait = 0;
                    DemoDisplayMain();
                    zenQuat[0] = qData.MlQuaternion[0];
                    zenQuat[1] = qData.MlQuaternion[1];
                    zenQuat[2] = qData.MlQuaternion[2];
                    zenQuat[3] = qData.MlQuaternion[3];
                }
            }

        }

        /**********************************************************************/
        /*                                                                    */
        /* Function Name: DemoInit                                            */
        /*                                                                    */
        /*   Description:  OpenGL initialization                              */
        /*                                                                    */
        /*    Parameters:                                                     */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        private void DemoInit()
        {
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            Gl.glDepthFunc(Gl.GL_LESS); //  # The Type Of Depth Test To Do
            Gl.glEnable(Gl.GL_DEPTH_TEST); //  # Enables Depth Testing
            Gl.glLoadIdentity(); //  # Reset The Projection Matrix

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();

            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE);
            Gl.glEnable(Gl.GL_BLEND);
        }// end of DemoInit
        /**********************************************************************/
        /*                                                                    */
        /* Function Name: DemoDisplayMain                                     */
        /*                                                                    */
        /*   Description:  OpenGL display callback                            */
        /*                                                                    */
        /*    Parameters:                                                     */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        private void DemoDisplayMain()
        {
            TeaPotDisplay();
            Gl.glFlush();
            Glut.glutSwapBuffers();
            System.Threading.Thread.Sleep(4);
        }

        /**********************************************************************/
        /*                                                                    */
        /* Function Name: TeaPotDisplay                                       */
        /*                                                                    */
        /*   Description:  OpenGL display Tea Pot                             */
        /*                                                                    */
        /*    Parameters:                                                     */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        private void TeaPotDisplay()
        {
            float flt;

            //the light position at (0, 0, 5)
            float[] lightpos = new float[] { 0.0F, 0.0F, 5.0F, 0.0F };

            //a diffuse light source with white light
            float[] light_diffuse = new float[4] { 1.0F, 1.0F, 1.0F, 1.0F };

            //keep the qData.quat into ResetQuat
            //ResetQuat can help adjust the direction of teapot 
            if (flg_ResetRotate == 1) // if aligment button was pressed, the direction reset
            {
                flg_ResetRotate = 0;
                for (int i = 0; i < 4; i++)
                {
                    ResetQuat[i] = qData.Quat[i];
                }
            }

            Gl.glDepthMask(1);
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
            flt = (float)Math.Acos(qData.Quat[0]);

            //void glRotatef( GLfloat angle, GLfloat x, GLfloat y, GLfloat z); 
            //rotate object "angle" degrees along (x, y, z)
            //should transfer the Invensense coordinate system to OpenGL coordinate system for the rotation axis (x,y,z)
            //OpenGL X = Invensense X <=> quat[1]
            //OpenGL Y = Invensense Z <=> quat[3]
            //OpenGL Z = Invensense -Y <=> -quat[2]
            Gl.glRotatef((float)(2 * flt * 180 / 3.1415), (float)qData.Quat[1], (float)qData.Quat[3], (float)-qData.Quat[2]);

            //rotate teapot 90 degree along Y axis, let the direction of spout equal the front of the motion device
            Gl.glRotatef(90, 0, 1, 0);

            //draw a teapot by OpengGL library
            Glut.glutSolidTeapot(0.6);
        }
        /**********************************************************************/
        /*                                                                    */
        /* Function Name: ReshapeWindowSize                                   */
        /*                                                                    */
        /*   Description:  OpenGL reshape callback                            */
        /*                                                                    */
        /*    Parameters: w - width                                           */
        /*                h - height                                          */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        private void ReshapeWindowSize(int w, int h)
        {
            Gl.glViewport(0, 0, w, h);
        }
        /**********************************************************************/
        /*                                                                    */
        /* Function Name: DemoDisplayIdle                                     */
        /*                                                                    */
        /*   Description:  OpenGL global idle callback                        */
        /*                                                                    */
        /*    Parameters:                                                     */
        /*                                                                    */
        /*       Returns:                                                     */
        /*                                                                    */
        /**********************************************************************/
        private void DemoDisplayIdle()
        {
            Glut.glutPostRedisplay();
        }

    }
}
