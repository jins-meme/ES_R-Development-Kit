using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Tao.OpenGl;
using Tao.Platform.Windows;
using Tao.FreeGlut;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// センサー姿勢表示ユーザーコントロール　コントロールクラス
    /// </summary>
    public class UserCtlSensorPostureCtl : OpenGLCtlBase
    {
        public enum SENS_HOKOU
        {
            SEI_HOKO,               // 正方向
            GYAKU_HOKO,             // 逆方向
        }
        /// <summary>
        /// センサー本体座標配列
        /// </summary>
        private VertexInfoListConta SensVerInfoListConta = new VertexInfoListConta();
        /// <summary>
        /// センサー方向本体座標配列
        /// </summary>
        private VertexInfoListConta SensHokoVerInfoListConta = new VertexInfoListConta();

        /// <summary>
        /// 原点軸表示色（X軸)
        /// </summary>
        private OpenGLColor_Bean gentenLineColorX = new OpenGLColor_Bean(1f, 1f, 1f, 1f);
        /// <summary>
        /// 原点軸表示色（Y軸)
        /// </summary>
        private OpenGLColor_Bean gentenLineColorY = new OpenGLColor_Bean(1f, 1f, 1f, 1f);
        /// <summary>
        /// 原点軸表示色（Z軸)
        /// </summary>
        private OpenGLColor_Bean gentenLineColorZ = new OpenGLColor_Bean(1f, 1f, 1f, 1f);
        /// <summary>
        /// センサー方向
        /// </summary>
        private SENS_HOKOU sensHoko = SENS_HOKOU.SEI_HOKO;
        /// <summary>
        /// デバックモード
        /// </summary>
        private bool debugMode = false;

        /// <summary>
        /// 測定データ保存クラス
        /// </summary>
        private QuatPacket_Bean qData = null;
        /// <summary>
        /// 前回クウォータニオン
        /// </summary>
        private long[] zenQuat = new long[4];
        /// <summary>
        /// 描画カウンタ
        /// </summary>
        private int teapotShwoWait = 0;

        /// <summary>
        /// 原点軸表示色（X軸)
        /// </summary>
        public OpenGLColor_Bean GentenLineColorX { get { return gentenLineColorX; } }
        /// <summary>
        /// 原点軸表示色（Y軸)
        /// </summary>
        public OpenGLColor_Bean GentenLineColorY { get { return gentenLineColorY; } }
        /// <summary>
        /// 原点軸表示色（Z軸)
        /// </summary>
        public OpenGLColor_Bean GentenLineColorZ { get { return gentenLineColorZ; } }
        /// <summary>
        /// E極ラベル
        /// </summary>
        public Label LblN { get; set; }
        /// <summary>
        /// Z極ラベル
        /// </summary>
        public Label LblZ { get; set; }
        /// <summary>
        /// N極ラベル
        /// </summary>
        public Label LblE { get; set; }
        /// <summary>
        /// デバック表示ラベル
        /// </summary>
        public Label LblDebugMon { get; set; }
        /// <summary>
        /// センサー方向
        /// </summary>
        public SENS_HOKOU SensHoko 
        {
            get { return sensHoko; }
            set 
            {
                sensHoko = value;
                SetInitData();
                DrowGraphMain();
            }
        }
        /// <summary>
        /// デバックモード
        /// </summary>
        public bool DebugMode { 
            get { return debugMode; } 
            set 
            { debugMode = value;
            if (LblDebugMon != null) 
            {
                LblDebugMon.Visible = value;
            }
            } 
        }


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="viewCtlObj"></param>
        public UserCtlSensorPostureCtl(Control viewCtlObj) : base(viewCtlObj) 
        { 
        }
        /// <summary>
        /// 初期データ設定
        /// </summary>
        protected override void SetInitData()
        {
            try
            {
                int data = 1;
                if (sensHoko == SENS_HOKOU.GYAKU_HOKO)
                {
                    data = -1;
                }

                // センサー本体座標
                OpenGL3D_Bean pointVC0 = new OpenGL3D_Bean(0.25f, 0.05f, 0.50f);
                OpenGL3D_Bean pointVC1 = new OpenGL3D_Bean(0.25f, 0.05f, -0.50f);
                OpenGL3D_Bean pointVC2 = new OpenGL3D_Bean(-0.25f, 0.05f, -0.50f);
                OpenGL3D_Bean pointVC3 = new OpenGL3D_Bean(-0.25f, 0.05f, 0.50f);
                OpenGL3D_Bean pointVC4 = new OpenGL3D_Bean(0.25f, -0.05f, 0.50f);
                OpenGL3D_Bean pointVC5 = new OpenGL3D_Bean(0.25f, -0.05f, -0.50f);
                OpenGL3D_Bean pointVC6 = new OpenGL3D_Bean(-0.25f, -0.05f, -0.50f);
                OpenGL3D_Bean pointVC7 = new OpenGL3D_Bean(-0.25f, -0.05f, 0.50f);
                // センサー方向表示用三角
                //OpenGL3D_Bean pointVT0 = new OpenGL3D_Bean(0.00f * data, 0.051f, 0.45f * data);
                //OpenGL3D_Bean pointVT1 = new OpenGL3D_Bean(0.20f * data, 0.051f, -0.25f * data);
                //OpenGL3D_Bean pointVT2 = new OpenGL3D_Bean(-0.20f * data, 0.051f, -0.25f * data);
                //OpenGL3D_Bean pointVT3 = new OpenGL3D_Bean(0.00f * data, -0.051f, 0.45f * data);
                //OpenGL3D_Bean pointVT4 = new OpenGL3D_Bean(0.20f * data, -0.051f, -0.25f * data);
                //OpenGL3D_Bean pointVT5 = new OpenGL3D_Bean(-0.20f * data, -0.051f, -0.25f * data);

                OpenGL3D_Bean pointVT0 = new OpenGL3D_Bean(0.00f * data, 0.051f, -0.25f * data);
                OpenGL3D_Bean pointVT1 = new OpenGL3D_Bean(0.20f * data, 0.051f, 0.45f * data);
                OpenGL3D_Bean pointVT2 = new OpenGL3D_Bean(-0.20f * data, 0.051f, 0.45f * data);
                OpenGL3D_Bean pointVT3 = new OpenGL3D_Bean(0.00f * data, -0.051f, -0.25f * data);
                OpenGL3D_Bean pointVT4 = new OpenGL3D_Bean(0.20f * data, -0.051f, 0.45f * data);
                OpenGL3D_Bean pointVT5 = new OpenGL3D_Bean(-0.20f * data, -0.051f, 0.45f * data);

                // センサー本体座標
                // VC0-VC1-VC2-VC3
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC0.X, pointVC0.Y, pointVC0.Z, 0f, 1f, 0f, 0.7f, 0.7f, 0.7f, 1f), true);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC1.X, pointVC1.Y, pointVC1.Z, 0f, 1f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC2.X, pointVC2.Y, pointVC2.Z, 0f, 1f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC3.X, pointVC3.Y, pointVC3.Z, 0f, 1f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                // VC0-VC1-VC5-VC4
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC0.X, pointVC0.Y, pointVC0.Z, 1f, 0f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC1.X, pointVC1.Y, pointVC1.Z, 1f, 0f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC5.X, pointVC5.Y, pointVC2.Z, 1f, 0f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC4.X, pointVC4.Y, pointVC3.Z, 1f, 0f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                // VC1-VC2-VC6-VC5
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC1.X, pointVC1.Y, pointVC1.Z, 0f, 0f, -1f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC2.X, pointVC2.Y, pointVC2.Z, 0f, 0f, -1f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC6.X, pointVC6.Y, pointVC6.Z, 0f, 0f, -1f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC5.X, pointVC5.Y, pointVC5.Z, 0f, 0f, -1f, 0.7f, 0.7f, 0.7f, 1f), false);
                // VC2-VC3-VC7-VC6
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC2.X, pointVC2.Y, pointVC2.Z, -1f, 0f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC3.X, pointVC3.Y, pointVC3.Z, -1f, 0f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC7.X, pointVC7.Y, pointVC7.Z, -1f, 0f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC6.X, pointVC6.Y, pointVC6.Z, -1f, 0f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                // VC0-VC3-VC7-VC4
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC0.X, pointVC0.Y, pointVC3.Z, 0f, 0f, 1f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC3.X, pointVC3.Y, pointVC4.Z, 0f, 0f, 1f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC7.X, pointVC7.Y, pointVC7.Z, 0f, 0f, 1f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC4.X, pointVC4.Y, pointVC6.Z, 0f, 0f, 1f, 0.7f, 0.7f, 0.7f, 1f), false);
                // VC4-VC5-VC6-VC7
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC4.X, pointVC4.Y, pointVC4.Z, 0f, -1f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC5.X, pointVC5.Y, pointVC5.Z, 0f, -1f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC6.X, pointVC6.Y, pointVC6.Z, 0f, -1f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.AddItem(new VertexInfo_Bean(pointVC7.X, pointVC7.Y, pointVC7.Z, 0f, -1f, 0f, 0.7f, 0.7f, 0.7f, 1f), false);
                SensVerInfoListConta.SeVertexNoList(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 }, true);

                // センサー方向表示用三角
                //VT0-VT1-VT2
                SensHokoVerInfoListConta.AddItem(new VertexInfo_Bean(pointVT0.X, pointVT0.Y, pointVT0.Z, 0f, 1f, 0f, 0.7f, 0.3f, 0.3f, 1f), true);
                SensHokoVerInfoListConta.AddItem(new VertexInfo_Bean(pointVT1.X, pointVT1.Y, pointVT1.Z, 0f, 1f, 0f, 0.7f, 0.3f, 0.3f, 1f), false);
                SensHokoVerInfoListConta.AddItem(new VertexInfo_Bean(pointVT2.X, pointVT2.Y, pointVT2.Z, 0f, 1f, 0f, 0.7f, 0.3f, 0.3f, 1f), false);
                //VT3-VT4-VT5
                SensHokoVerInfoListConta.AddItem(new VertexInfo_Bean(pointVT3.X, pointVT3.Y, pointVT3.Z, 0f, 1f, 0f, 0.3f, 0.3f, 0.5f, 1f), false);
                SensHokoVerInfoListConta.AddItem(new VertexInfo_Bean(pointVT4.X, pointVT4.Y, pointVT4.Z, 0f, 1f, 0f, 0.3f, 0.3f, 0.5f, 1f), false);
                SensHokoVerInfoListConta.AddItem(new VertexInfo_Bean(pointVT5.X, pointVT5.Y, pointVT5.Z, 0f, 1f, 0f, 0.3f, 0.3f, 0.5f, 1f), false);
                SensHokoVerInfoListConta.SeVertexNoList(new int[] { 0, 1, 2, 3, 4, 5 }, true);
                // 描画パラメータ初期値設定
                GraphLockAtAngleH = 70;
                GraphLockAtAngleV = 30;
                GraphLockAtDistance = 2;
            }
            catch (Exception ex)
            {
                Tracer.WriteError("初期データ設定中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }

        }
        /// <summary>
        /// センサーデータ設定
        /// </summary>
        /// <param name="data">センサーデータ</param>
        public void SetValue(long[] qt)
        {
            try
            {
                qData = new QuatPacket_Bean(qt);
                //IMUgetQuaternion(&qData);
                if (zenQuat[0] != qData.MlQuaternion[0] || zenQuat[1] != qData.MlQuaternion[1] || zenQuat[2] != qData.MlQuaternion[2] || zenQuat[3] != qData.MlQuaternion[3])
                {
                    teapotShwoWait += 1;
                    if (teapotShwoWait >= 1)
                    {
                        teapotShwoWait = 0;
                        DrowGraphMain();
                        zenQuat[0] = qData.MlQuaternion[0];
                        zenQuat[1] = qData.MlQuaternion[1];
                        zenQuat[2] = qData.MlQuaternion[2];
                        zenQuat[3] = qData.MlQuaternion[3];
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("センサーデータ設定中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }

        }
        /// <summary>
        /// 図形描画
        /// </summary>
        protected override void DrowGraph()
        {
            try
            {
                float flt;
                Gl.glPushMatrix();
                {
                    // 西（X軸マイナス方向) ,上方向、南方向（Zプラス方向)の中央線描画
                    //DrowXYZAxis(-1.2f, gentenLineColorX, 0.7f, gentenLineColorY, 1.1f, gentenLineColorZ);
                    DrowXYZAxis(2.2f, gentenLineColorX, 1.0f, gentenLineColorY, -1.0f, gentenLineColorZ);
                    Gl.glPushMatrix();
                    {
                        if (qData != null)
                        {
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
                            //Gl.glRotatef(-mEULAR_ANGLE_Z_FROM_QUAT_YXZ_CONVNETION(ResetQuat) * R2D, 0, 1, 0);

                            //compute the rotation angle from quat[0] 
                            flt = (float)Math.Acos(qData.Quat[0]);

                            //void glRotatef( GLfloat angle, GLfloat x, GLfloat y, GLfloat z); 
                            //rotate object "angle" degrees along (x, y, z)
                            //should transfer the Invensense coordinate system to OpenGL coordinate system for the rotation axis (x,y,z)
                            //OpenGL X = Invensense X <=> quat[1]
                            //OpenGL Y = Invensense Z <=> quat[3]
                            //OpenGL Z = Invensense -Y <=> -quat[2]
                            Gl.glRotatef((float)(2 * flt * 180 / 3.1415), (float)qData.Quat[1], (float)qData.Quat[3], (float)-qData.Quat[2]);
                            if (DebugMode == true && LblDebugMon != null)
                            {
                                LblDebugMon.Text = string.Format("t={0}\r\nx={1}\r\ny={2}\r\nz={3}", (float)(2 * flt * 180 / 3.1415), (float)qData.Quat[1], (float)qData.Quat[3], (float)-qData.Quat[2]);
                            }
                        }
                        DrowSensir();
                        //Gl.glRotatef((float)(90 * 3.1415 / 180), 0, 1, 0);
                    }
                }
                Gl.glPopMatrix();
            }
            catch (Exception ex)
            {
                Tracer.WriteError("図形描画中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }

        }
        // 描画
        public void DrowSensir()
        {
            try
            {
                Gl.glEnableClientState(Gl.GL_VERTEX_ARRAY);
                Gl.glEnableClientState(Gl.GL_NORMAL_ARRAY);
                Gl.glEnableClientState(Gl.GL_COLOR_ARRAY);
                // センサー本体座標
                Gl.glVertexPointer(3, Gl.GL_FLOAT, 0, SensVerInfoListConta.VertexList);
                Gl.glNormalPointer(Gl.GL_FLOAT, 0, SensVerInfoListConta.NormalList);
                Gl.glColorPointer(3, Gl.GL_FLOAT, 0, SensVerInfoListConta.ColorList3);
                Gl.glDrawElements(Gl.GL_QUADS, SensVerInfoListConta.IndexnList.Length, Gl.GL_UNSIGNED_BYTE, SensVerInfoListConta.IndexnList);
                // センサー方向表示用三角
                Gl.glVertexPointer(3, Gl.GL_FLOAT, 0, SensHokoVerInfoListConta.VertexList);
                Gl.glNormalPointer(Gl.GL_FLOAT, 0, SensHokoVerInfoListConta.NormalList);
                Gl.glColorPointer(3, Gl.GL_FLOAT, 0, SensHokoVerInfoListConta.ColorList3);
                Gl.glDrawElements(Gl.GL_TRIANGLES, SensHokoVerInfoListConta.IndexnList.Length, Gl.GL_UNSIGNED_BYTE, SensHokoVerInfoListConta.IndexnList);

                Gl.glDisableClientState(Gl.GL_VERTEX_ARRAY);
                Gl.glDisableClientState(Gl.GL_NORMAL_ARRAY);
                Gl.glDisableClientState(Gl.GL_COLOR_ARRAY);
            }
            catch (Exception ex)
            {
                Tracer.WriteError("描画中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }
        }
        /// <summary>
        /// 画面リサイズ
        /// </summary>
        public void Resizu()
        {
            try
            {
                // 射影行列初期設定
                ProjectionInit();

                // モデルビュー配列設定
                ModelViewInit();
                // グラフ表示
                DrowGraphMain();
            }
            catch (Exception ex)
            {
                Tracer.WriteError("画面リサイズ中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }

        }
        /// <summary>
        /// XYZ軸追加表示
        /// </summary>
        /// <param name="lengthX">X軸線長</param>
        /// <param name="colorX">X軸線色</param>
        /// <param name="lengthY">Y軸線長</param>
        /// <param name="colorY">Y軸線色</param>
        /// <param name="lengthZ">Z軸線長</param>
        /// <param name="colorZ">Z軸線色</param>
        protected override void DrowXYZAxisSub(float lengthX, OpenGLColor_Bean colorX, float lengthY, OpenGLColor_Bean colorY, float lengthZ, OpenGLColor_Bean colorZ)
        {
            try
            {
                double[] modelview = new double[16];
                double[] projection = new double[16];
                int[] viewport = new int[4];
                double winX, winY, winZ;//ウィンドウ座標格納用

                //モデルビュー行列取得
                Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, modelview);

                //透視投影行列取得
                Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX, projection);
                //ビューポート取得
                Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);
                //座標変換の計算
                Glu.gluProject(lengthX, 0.0, 0.0, modelview, projection, viewport, out winX, out winY, out winZ);
                if (LblN != null)
                {
                    LblN.Left = (int)winX - LblN.Width;
                    LblN.Top = (int)(viewCtlObj.Size.Height - winY) - LblN.Height;

                }
                //座標変換の計算
                Glu.gluProject(0.0, lengthY, 0.0, modelview, projection, viewport, out winX, out winY, out winZ);
                if (LblZ != null)
                {
                    LblZ.Left = (int)winX;
                    LblZ.Top = (int)(viewCtlObj.Size.Height - winY);

                }
                Glu.gluProject(0.0, 0.0, lengthZ, modelview, projection, viewport, out winX, out winY, out winZ);
                if (LblE != null)
                {
                    LblE.Left = (int)winX - LblE.Width;
                    LblE.Top = (int)(viewCtlObj.Size.Height - winY) - LblE.Height;

                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError(" XYZ軸追加表示中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }
        }

    }
}
