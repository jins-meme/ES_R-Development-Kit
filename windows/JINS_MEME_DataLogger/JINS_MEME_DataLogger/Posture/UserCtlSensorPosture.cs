using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Tao.OpenGl;
using Tao.Platform.Windows;
using Tao.FreeGlut;

using GLenum = System.UInt32;
using GLboolean = System.Boolean;
using GLbitfield = System.UInt32;
using GLbyte = System.SByte;
using GLshort = System.Int16;
using GLint = System.Int32;
using GLsizei = System.Int32;
using GLubyte = System.Byte;
using GLushort = System.UInt16;
using GLuint = System.UInt32;
using GLfloat = System.Single;
using GLclampf = System.Single;
using GLdouble = System.Double;
using GLclampd = System.Double;

namespace JINS_MEME_DataLogger
{
    /// <summary>
    /// OpenGL センサー姿勢表示コントロール
    /// </summary>
    public partial class UserCtlSensorPosture : UserControl
    {
        /// <summary>
        /// OpenGLセンサー位置表示グラフ　コントロールクラス
        /// </summary>
        private UserCtlSensorPostureCtl userCtlSensorPostureCtl = new UserCtlSensorPostureCtl(null);

        /// <summary>
        /// 視点角度（水平）
        /// </summary>
        [Category("SensorLocus Control")]
        [Description("視点角度（水平）")]
        public float GraphLockAtAngleH
        {
            get { return userCtlSensorPostureCtl.GraphLockAtAngleH; }
            set
            {
                userCtlSensorPostureCtl.GraphLockAtAngleH = value;
                // グラフ表示
                userCtlSensorPostureCtl.DrowGraphMain();
            }
        }
        /// <summary>
        /// 視点角度（垂直）
        /// </summary>
        [Category("SensorLocus Control")]
        [Description("視点角度（垂直）")]
        public float GraphLockAtAngleV
        {
            get { return userCtlSensorPostureCtl.GraphLockAtAngleV; }
            set
            {
                userCtlSensorPostureCtl.GraphLockAtAngleV = value;
                // グラフ表示
                userCtlSensorPostureCtl.DrowGraphMain();
            }
        }
        /// <summary>
        /// 中心点からの距離
        /// </summary>
        [Category("SensorLocus Control")]
        [Description("中心点からの距離")]
        public float GraphLockAtDistance
        {
            get { return userCtlSensorPostureCtl.GraphLockAtDistance; }
            set
            {
                userCtlSensorPostureCtl.GraphLockAtDistance = value;
                // グラフ表示
                userCtlSensorPostureCtl.DrowGraphMain();
            }
        }
        /// <summary>
        /// 原点軸表示色（X軸)赤
        /// </summary>
        [Category("SensorLocus Control")]
        [Description("原点軸表示色（X軸)赤")]
        public float GentenLineColorXRed
        {
            get { return userCtlSensorPostureCtl.GentenLineColorX.Red; }
            set
            {
                userCtlSensorPostureCtl.GentenLineColorX.Red = value;
                // グラフ表示
                userCtlSensorPostureCtl.DrowGraphMain();
            }
        }
        /// <summary>
        /// 原点軸表示色（X軸)緑
        /// </summary>
        [Category("SensorLocus Control")]
        [Description("原点軸表示色（X軸)緑")]
        public float GentenLineColorXGreen
        {
            get { return userCtlSensorPostureCtl.GentenLineColorX.Green; }
            set
            {
                userCtlSensorPostureCtl.GentenLineColorX.Green = value;
                // グラフ表示
                userCtlSensorPostureCtl.DrowGraphMain();
            }
        }
        /// <summary>
        /// 原点軸表示色（X軸)青
        /// </summary>
        [Category("SensorLocus Control")]
        [Description("原点軸表示色（X軸)青")]
        public float GentenLineColorXBlue
        {
            get { return userCtlSensorPostureCtl.GentenLineColorX.Blue; }
            set
            {
                userCtlSensorPostureCtl.GentenLineColorX.Blue = value;
                // グラフ表示
                userCtlSensorPostureCtl.DrowGraphMain();
            }
        }
        /// <summary>
        /// 原点軸表示色（Y軸)赤
        /// </summary>
        [Category("SensorLocus Control")]
        [Description("原点軸表示色（Y軸)赤")]
        public float GentenLineColorYRed
        {
            get { return userCtlSensorPostureCtl.GentenLineColorY.Red; }
            set
            {
                userCtlSensorPostureCtl.GentenLineColorY.Red = value;
                // グラフ表示
                userCtlSensorPostureCtl.DrowGraphMain();
            }
        }
        /// <summary>
        /// 原点軸表示色（Y軸)緑
        /// </summary>
        [Category("SensorLocus Control")]
        [Description("原点軸表示色（Y軸)緑")]
        public float GentenLineColorYGreen
        {
            get { return userCtlSensorPostureCtl.GentenLineColorY.Green; }
            set
            {
                userCtlSensorPostureCtl.GentenLineColorY.Green = value;
                // グラフ表示
                userCtlSensorPostureCtl.DrowGraphMain();
            }
        }
        /// <summary>
        /// 原点軸表示色（Y軸)青
        /// </summary>
        [Category("SensorLocus Control")]
        [Description("原点軸表示色（Y軸)青")]
        public float GentenLineColorYBlue
        {
            get { return userCtlSensorPostureCtl.GentenLineColorY.Blue; }
            set
            {
                userCtlSensorPostureCtl.GentenLineColorY.Blue = value;
                // グラフ表示
                userCtlSensorPostureCtl.DrowGraphMain();
            }
        }
        /// <summary>
        /// 原点軸表示色（Y軸)赤
        /// </summary>
        [Category("SensorLocus Control")]
        [Description("原点軸表示色（Z軸)赤")]
        public float GentenLineColorZRed
        {
            get { return userCtlSensorPostureCtl.GentenLineColorZ.Red; }
            set
            {
                userCtlSensorPostureCtl.GentenLineColorZ.Red = value;
                // グラフ表示
                userCtlSensorPostureCtl.DrowGraphMain();
            }
        }
        /// <summary>
        /// 原点軸表示色（X軸)緑
        /// </summary>
        [Category("SensorLocus Control")]
        [Description("原点軸表示色（Z軸)緑")]
        public float GentenLineColorZGreen
        {
            get { return userCtlSensorPostureCtl.GentenLineColorZ.Green; }
            set
            {
                userCtlSensorPostureCtl.GentenLineColorZ.Green = value;
                // グラフ表示
                userCtlSensorPostureCtl.DrowGraphMain();
            }
        }
        /// <summary>
        /// 原点軸表示色（X軸)青
        /// </summary>
        [Category("SensorLocus Control")]
        [Description("原点軸表示色（Z軸)青")]
        public float GentenLineColorZBlue
        {
            get { return userCtlSensorPostureCtl.GentenLineColorZ.Blue; }
            set
            {
                userCtlSensorPostureCtl.GentenLineColorZ.Blue = value;
                // グラフ表示
                userCtlSensorPostureCtl.DrowGraphMain();
            }
        }
        /// <summary>
        /// デバックモード
        /// </summary>
        [Category("SensorLocus Control")]
        [Description("デバックモード")]
        public bool DebugMode
        {
            get { return userCtlSensorPostureCtl.DebugMode; }
            set { userCtlSensorPostureCtl.DebugMode = value;}
        }
        /// <summary>
        /// センサー方向
        /// </summary>
        public UserCtlSensorPostureCtl.SENS_HOKOU SensHoko
        {
            get { return userCtlSensorPostureCtl.SensHoko; }
            set
            {
                userCtlSensorPostureCtl.SensHoko = value;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public UserCtlSensorPosture()
        {
            InitializeComponent();
            
        }
        /// <summary>
        /// 生成時の処理
        /// </summary>
        /// <param name="e"></param>
        protected override void OnHandleCreated(EventArgs e)
        {
            try 
            {
                base.OnHandleCreated(e);

                this.SetStyle(ControlStyles.UserPaint, true);                       // true：コントロールは、オペレーティング システムによってではなく、独自に描画されます
                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);            // true:コントロールはウィンドウ メッセージ WM_ERASEBKGND を無視することによって、ちらつきを抑えます。このスタイルは、UserPaint ビットが true に設定されている場合だけ適用されます。
                this.SetStyle(ControlStyles.DoubleBuffer, false);                   // true:描画はバッファで実行され、完了後に、結果が画面に出力されます。ダブル バッファリングは、コントロールの描画によるちらつきを防ぎます。DoubleBuffer を true に設定した場合は、UserPaint および AllPaintingInWmPaint も true に設定する必要があります。
                this.SetStyle(ControlStyles.Opaque, true);                          // true:コントロールが不透明に描画され、背景は描画されません。
                this.SetStyle(ControlStyles.ResizeRedraw, true);                    // true:コントロールのサイズが変更されると、そのコントロールが再描画されます。

                // 描画コントロールセット
                userCtlSensorPostureCtl.SetViewObject(this);
                userCtlSensorPostureCtl.LblE = lblN;
                userCtlSensorPostureCtl.LblZ = lblZ;
                userCtlSensorPostureCtl.LblN = lblE;
                userCtlSensorPostureCtl.LblDebugMon = lblDebugMon;
                lblDebugMon.Visible = userCtlSensorPostureCtl.DebugMode;

                // グラフ描画
                userCtlSensorPostureCtl.DrowGraphMain();
            }
            catch (Exception ex) 
            {
                Tracer.WriteError("生成時の処理中にエラー発生　メッセージ {0} \r\n{1}", ex.Message.ToString(), ex.StackTrace.ToString());

            }
        }
        /// <summary>
        /// コントロールハンドル破棄
        /// </summary>
        /// <param name="e"></param>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (userCtlSensorPostureCtl != null)
            {
                userCtlSensorPostureCtl.Dispose();
            }
            base.OnHandleDestroyed(e);
        }
        /// <summary>
        /// 描画処理
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (userCtlSensorPostureCtl != null)
            {
                userCtlSensorPostureCtl.DrowGraphMain();
            }
        }
        /// <summary>
        /// リサイズ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserCtlSensorPosture_Resize(object sender, EventArgs e)
        {
            if (userCtlSensorPostureCtl != null)
            {
                userCtlSensorPostureCtl.Resizu();
            }

        }
        public void ResetSensPosture() 
        {
            long[] data = new long[]{0,0,0,0};
            //データ設定
            userCtlSensorPostureCtl.SetValue(data);
            // グラフ再表示
            userCtlSensorPostureCtl.DrowGraphMain();
        }
        /// <summary>
        /// センサーデータ設定
        /// </summary>
        /// <param name="data">センサーデータ</param>
        public void SetValue(long[] data)
        {
            //データ設定
            userCtlSensorPostureCtl.SetValue(data);
            // グラフ再表示
            userCtlSensorPostureCtl.DrowGraphMain();

        }

        private void UserCtlSensorPosture_Paint(object sender, PaintEventArgs e)
        {
            if (userCtlSensorPostureCtl != null)
            {
                userCtlSensorPostureCtl.DrowGraphMain();
            }

        }

        private void UserCtlSensorPosture_Move(object sender, EventArgs e)
        {
            if (userCtlSensorPostureCtl != null)
            {
                userCtlSensorPostureCtl.DrowGraphMain();
            }

        }
        /// <summary>
        /// マウスボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserCtlSensorPosture_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) 
            {
                //sensPostureSetting frm = new sensPostureSetting(this);
                //frm.ShowDialog();
            }
        }


    }
}
