using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using Tao.OpenGl;
using Tao.Platform.Windows;
using Tao.FreeGlut;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// OPenGL 描画コントロールベースクラス
    /// </summary>
    public class OpenGLCtlBase : IDisposable
    {


        // デバイスコンテキストハンドル
        protected IntPtr hRC = IntPtr.Zero;
        // レンダリングコンテキスト
        protected IntPtr hDC = IntPtr.Zero;
        /// <summary>
        /// 描画コントロール
        /// </summary>
        protected Control viewCtlObj = null;

        /// <summary>
        /// 視点角度（水平）
        /// </summary>
        private float graphLockAtAngleH = 45;
        /// <summary>
        /// 視点角度（垂直）
        /// </summary>
        private float graphLockAtAngleV = 30;
        /// <summary>
        /// 中心点からの距離
        /// </summary>
        private float graphLockAtDistance = 23;

        /// <summary>
        /// 入力視点角度（水平）
        /// </summary>
        private float graphInpLockAtAngleH = 45;
        /// <summary>
        /// 入力視点角度（垂直）
        /// </summary>
        private float graphInpLockAtAngleV = 30;
        /// <summary>
        /// 入力中心点からの距離
        /// </summary>
        private float graphInpLockAtDistance = 23;
        /// <summary>
        /// グラフ背景色
        /// </summary>
        private OpenGLColor_Bean graphBackColor = new OpenGLColor_Bean();

        /// <summary>
        /// グラフ視点座標
        /// </summary>
        private OpenGL3D_Bean lockAtPoint = new OpenGL3D_Bean();

        /// <summary>
        /// 視点角度（水平）
        /// </summary>
        public float GraphLockAtAngleH 
        { 
            get { return graphLockAtAngleH; } 
            set 
            { 
                graphLockAtAngleH = value;
                graphInpLockAtAngleH = value;
                // 視点座標計算
                lockAtPoint.Copy(CaleLoatAtPos(graphLockAtAngleH, graphLockAtAngleV, graphLockAtDistance));
                // 図形表示メイン
                DrowGraphMain();
            } 
        }
        /// <summary>
        /// 視点角度（垂直）
        /// </summary>
        public float GraphLockAtAngleV 
        { 
            get { return graphLockAtAngleV; } 
            set 
            { 
                graphLockAtAngleV = value;
                graphInpLockAtAngleV = value;
                // 視点座標計算
                lockAtPoint.Copy(CaleLoatAtPos(graphLockAtAngleH, graphLockAtAngleV, graphLockAtDistance));
                // 図形表示メイン
                DrowGraphMain();
            } 
        }
        /// <summary>
        /// 中心点からの距離
        /// </summary>
        public float GraphLockAtDistance 
        { 
            get { return graphLockAtDistance; } 
            set 
            { 
                graphLockAtDistance = value;
                graphInpLockAtDistance = value;
                // 視点座標計算
                lockAtPoint.Copy(CaleLoatAtPos(graphLockAtAngleH, graphLockAtAngleV, graphLockAtDistance));
                // 図形表示メイン
                DrowGraphMain();
            } 
        }

        /// <summary>
        /// グラフ背景色
        /// </summary>
        public OpenGLColor_Bean GraphBackColor {get {return graphBackColor; } }

        ///// <summary>
        ///// グラフ視点座標
        ///// </summary>
        //public OpenGL3D_Bean LockAtPoint { get { return lockAtPoint; } }


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="handle">描画コントロール</param>
        public OpenGLCtlBase(Control viewCtlObj) 
        {
             SetViewObject(viewCtlObj);

        }
        /// <summary>
        /// ディストラクタ
        /// </summary>
        public void Dispose()
        {
            try
            {
                Wgl.wglMakeCurrent(this.hDC, IntPtr.Zero);
                Wgl.wglDeleteContext(this.hRC);
            }
            catch (Exception ex)
            {
                Tracer.WriteError("ディストラクタ中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }
        }
        /// <summary>
        /// 表示コントロール設定
        /// </summary>
        /// <param name="viewCtlObj"></param>
        public void SetViewObject(Control viewCtlObj) 
        {
            
            try
            {
                this.viewCtlObj = viewCtlObj;
                if (this.viewCtlObj != null) 
                {
                    // ピクセルフォーマットの設定をする
                    SetupPixelFormat();
                    // OpenGLの初期設定
                    SetupOpenGL();
                    // 初期データ設定
                    SetInitData();

                    // 視点座標計算
                    lockAtPoint.Copy(CaleLoatAtPos(graphLockAtAngleH, graphLockAtAngleV, graphLockAtDistance));
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("表示コントロール設定中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }
        }
        /// <summary>
        /// ピクセルフォーマットの設定をする
        /// </summary>
        protected void SetupPixelFormat()
        {
            try
            {
                //PIXELFORMATDESCRIPTORの設定
                Gdi.PIXELFORMATDESCRIPTOR pfd = new Gdi.PIXELFORMATDESCRIPTOR();
                // OpenGLをサポート、ウィンドウに描画、ダブルバッファ。
                pfd.dwFlags = Gdi.PFD_SUPPORT_OPENGL | Gdi.PFD_DRAW_TO_WINDOW | Gdi.PFD_DOUBLEBUFFER;
                pfd.iPixelType = Gdi.PFD_TYPE_RGBA;     //RGBAフォーマット
                pfd.cColorBits = 32;                    // 32bit/pixel
                pfd.cAlphaBits = 8;                     // アルファチャンネル8bit (0にするとアルファチャンネル無しになる)
                pfd.cDepthBits = 32;                    // デプスバッファ32bit

                //デバイスコンテキストハンドルの収録
                this.hDC = User.GetDC(viewCtlObj.Handle);

                //ピクセルフォーマットを選択
                int pixelFormat = Gdi.ChoosePixelFormat(this.hDC, ref pfd);
                if (pixelFormat == 0)
                {
                    this.viewCtlObj = null;
                    throw new Exception("Error: Cant't Find A Suitable PixelFormat.");
                }

                //ピクセルフォーマットを設定
                if (!Gdi.SetPixelFormat(this.hDC, pixelFormat, ref pfd))
                {
                    this.viewCtlObj = null;
                    throw new Exception("Error: Cant't Set The PixelFormat.");
                }

                //レンダリングコンテキストを生成
                this.hRC = Wgl.wglCreateContext(this.hDC);
                if (this.hRC == IntPtr.Zero)
                {
                    this.viewCtlObj = null;
                    throw new Exception("Error: Cant Create A GLRendering Context.");
                }

                //レンダリングコンテキストをカレントにする
                Wgl.wglMakeCurrent(this.hDC, this.hRC);

                //GLエラーのチェック
                int err = Gl.glGetError();
                if (err != Gl.GL_NO_ERROR)
                {
                    this.viewCtlObj = null;
                    throw new Exception("GL Error:" + err.ToString());
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("ピクセルフォーマットの設定中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }
        }
        /// <summary>
        /// OpenGLの初期設定
        /// </summary>
        protected void SetupOpenGL()
        {
            try
            {
                //バッファをクリアする色
                Gl.glClearColor(0.0f, 0.0f, 0.0f, 0.0f);

                //深度テストを有効
                Gl.glEnable(Gl.GL_DEPTH_TEST);
                Gl.glDepthFunc(Gl.GL_LEQUAL);

                //スムースシェイディング
                Gl.glShadeModel(Gl.GL_SMOOTH);
            }
            catch (Exception ex)
            {
                Tracer.WriteError("OpenGLの初期設定中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }

        }
        /// <summary>
        /// 初期データ設定
        /// </summary>
        protected virtual void SetInitData() 
        { 
        }
        /// <summary>
        /// 図形表示メイン
        /// </summary>
        public void DrowGraphMain() 
        {
            try
            {
                // 軌跡を最後まで表示
                if (viewCtlObj != null)
                {
                    //レンダリングコンテキストをカレントにする
                    Wgl.wglMakeCurrent(this.hDC, this.hRC);
                    // 図形表示初期設定
                    DisplyInit();

                    // 射影行列初期設定
                    ProjectionInit();

                    // モデルビュー配列設定
                    ModelViewInit();

                    // 図形描画
                    DrowGraph();


                    Gl.glFlush();
                    //ダブルバッファ
                    Wgl.wglSwapBuffers(this.hDC);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("図形表示メイン中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }
        }
        /// <summary>
        /// シミュレーション図形表示メイン
        /// </summary>
        /// <param name="showEndSeq">表示終了Seq</param>
        public void DrowSimuGraphMain(int showEndSeq)
        {
            try
            {
                // 軌跡を最後まで表示
                if (viewCtlObj != null)
                {
                    //レンダリングコンテキストをカレントにする
                    Wgl.wglMakeCurrent(this.hDC, this.hRC);
                    // 図形表示初期設定
                    DisplyInit();

                    // 射影行列初期設定
                    ProjectionInit();

                    // モデルビュー配列設定
                    ModelViewInit();

                    // 図形描画
                    DrowGraphSimu(showEndSeq);


                    Gl.glFlush();
                    //ダブルバッファ
                    Wgl.wglSwapBuffers(this.hDC);

                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("シミュレーション図形表示メイン中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }

        }
        /// <summary>
        /// 図形表示初期設定
        /// </summary>
        protected  void DisplyInit() 
        {
            try
            {
                //バッファをクリアする色
                Gl.glClearColor(graphBackColor.Red, graphBackColor.Green, graphBackColor.Blue, graphBackColor.Alpha);

                //バッファをクリア
                Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

                //ビューポートの設定
                if (viewCtlObj != null)
                {
                    Gl.glViewport(0, 0, viewCtlObj.ClientRectangle.Width, viewCtlObj.ClientRectangle.Height);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("図形表示初期設定中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }
        }
        
        /// <summary>
        /// 射影行列初期設定
        /// </summary>
        protected  void ProjectionInit() 
        {
            try
            {
                if (viewCtlObj != null)
                {
                    Gl.glMatrixMode(Gl.GL_PROJECTION);
                    //配列初期化
                    Gl.glLoadIdentity();
                    // 視体積の設定(表示域が指定されているので中心からの距離を大きくしたときに表示がかける場合はパラメータ最後の値を大きくする)
                    Glu.gluPerspective(45.0, (double)viewCtlObj.ClientRectangle.Width / (double)viewCtlObj.ClientRectangle.Height, 0.1, 150.0); //視野の設定
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("射影行列初期設定中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }
        }
        /// <summary>
        /// モデルビュー配列設定
        /// </summary>
        protected void ModelViewInit() 
        {
            try
            {
                if (viewCtlObj != null)
                {
                    Gl.glMatrixMode(Gl.GL_MODELVIEW);
                    // モデルビュー配列の初期化
                    Gl.glLoadIdentity();
                    // 視点設定
                    Glu.gluLookAt(lockAtPoint.X, lockAtPoint.Y, lockAtPoint.Z, 0, 0, 0, 0, 1, 0);
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("モデルビュー配列設定中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }
        }
        /// <summary>
        /// 図形描画
        /// </summary>
        protected virtual void DrowGraph() 
        { 
        }
        /// <summary>
        /// シミュレーション図形描画
        /// </summary>
        /// <param name="showEndSeq">表示終了Seq</param>
        protected virtual void DrowGraphSimu(int showEndSeq)
        {

        }



        /// <summary>
        /// XYZ軸描画
        /// </summary>
        /// <param name="lengthX">X軸線長</param>
        /// <param name="colorX">X軸線色</param>
        /// <param name="lengthY">Y軸線長</param>
        /// <param name="colorY">Y軸線色</param>
        /// <param name="lengthZ">Z軸線長</param>
        /// <param name="colorZ">Z軸線色</param>
        protected void DrowXYZAxis(float lengthX, OpenGLColor_Bean colorX, float lengthY, OpenGLColor_Bean colorY, float lengthZ, OpenGLColor_Bean colorZ)
        {
            try
            {
                /// XYZ軸追加表示
                DrowXYZAxisSub(lengthX, colorX, lengthY, colorY, lengthZ, colorZ);

                //X軸線
                Gl.glBegin(Gl.GL_LINES);
                {
                    Gl.glColor3f(colorX.Red, colorX.Green, colorX.Blue);
                    Gl.glVertex3f(0f, 0f, 0f);
                    Gl.glVertex3f(lengthX, 0f, 0f);
                }
                Gl.glEnd();
                //Y軸線
                Gl.glBegin(Gl.GL_LINES);
                {
                    Gl.glColor3f(colorY.Red, colorY.Green, colorY.Blue);
                    Gl.glVertex3f(0f, 0f, 0f);
                    Gl.glVertex3f(0f, lengthY, 0f);
                }
                Gl.glEnd();
                //Z軸線
                Gl.glBegin(Gl.GL_LINES);
                {
                    Gl.glColor3f(colorZ.Red, colorZ.Green, colorZ.Blue);
                    Gl.glVertex3f(0f, 0f, 0f);
                    Gl.glVertex3f(0f, 0f, lengthZ);
                }
                Gl.glEnd();
            }
            catch (Exception ex)
            {
                Tracer.WriteError("XYZ軸描画中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

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
        protected virtual void DrowXYZAxisSub(float lengthX, OpenGLColor_Bean colorX, float lengthY, OpenGLColor_Bean colorY, float lengthZ, OpenGLColor_Bean colorZ)
        {
        }

        /// <summary>
        /// 視点座標計算
        /// </summary>
        /// <param name="angleH">視点角度（水平）</param>
        /// <param name="angleV">視点角度（垂直）</param>
        /// <param name="distance">中心点からの距離</param>
        /// <returns></returns>
        private OpenGL3D_Bean CaleLoatAtPos(float angleH, float angleV, float distance)
        {
            OpenGL3D_Bean result = new OpenGL3D_Bean();
            try
            {
                // 垂直方向
                result.Y = (float)(distance * Math.Sin(angleV * 3.1415 / 180f));
                float syaH = (float)(distance * Math.Cos(angleV * 3.1415 / 180f));
                // 水平方向座標
                if (Math.Abs(angleH) < 90 || (Math.Abs(angleH) >= 180 && Math.Abs(angleH) < 270))
                {
                    // 0～90度
                    result.X = (float)(syaH * Math.Cos(angleH * 3.1415 / 180f));
                    result.Z = (float)(syaH * Math.Cos((90 - angleH) * 3.1415 / 180f));
                }
                else //if (Math.Abs(angleH) < 180)
                {
                    // 90～180度
                    result.Z = (float)(syaH * Math.Cos((angleH - 90) * 3.1415 / 180f));
                    result.X = -(float)(syaH * Math.Cos((180 - angleH) * 3.1415 / 180f));
                }
            }
            catch (Exception ex)
            {
                Tracer.WriteError("視点座標計算中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }
            return result;
        }
        /// <summary>
        /// 入力設定値に戻す
        /// </summary>
        public void ResetInpSettei() 
        {
            // 視点角度（水平）
            graphLockAtAngleH = graphInpLockAtAngleH;
            // 視点角度（垂直）
            graphLockAtAngleV = graphInpLockAtAngleV;
            // 中心点からの距離
            graphLockAtDistance = graphInpLockAtDistance;
            // 視点座標計算
            lockAtPoint.Copy(CaleLoatAtPos(graphLockAtAngleH, graphLockAtAngleV, graphLockAtDistance));
            // 図形表示メイン
            DrowGraphMain();
        }

    }
}
