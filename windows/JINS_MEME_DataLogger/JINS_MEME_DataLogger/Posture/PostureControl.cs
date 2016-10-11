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


namespace JINS_MEME_DataLogger
{
    public partial class PostureControl : UserControl
    {
        private GlAnimator glAnimator = new GlAnimator();


        public PostureControl()
        {
            InitializeComponent();

            this.CreateParams.ClassStyle = this.CreateParams.ClassStyle |       // Redraw On Size, And Own DC For Window.
                User.CS_HREDRAW | User.CS_VREDRAW | User.CS_OWNDC;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);            // No Need To Erase Form Background
            this.SetStyle(ControlStyles.DoubleBuffer, true);                    // Buffer Control
            this.SetStyle(ControlStyles.Opaque, true);                          // No Need To Draw Form Background
            this.SetStyle(ControlStyles.ResizeRedraw, true);                    // Redraw On Resize
            this.SetStyle(ControlStyles.UserPaint, true);                       // We'll Handle Painting Ourselves

        }

        /// <summary>
        /// 描画要求
        /// </summary>
        /// <param name="quat"></param>
        public void Draw(float[] qt)
        {
            glAnimator.Quat = qt;
        }

        /// <summary>
        /// ロード
        /// アニメータを初期化しておく
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PostureControl_Load(object sender, EventArgs e)
        {
            glAnimator.InitGLWindow(this.pictureBox, this.Width, this.Height, 16);
        }

        /// <summary>
        /// リサイズされたらGLSceneもリサイズする
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PostureControl_Resize(object sender, EventArgs e)
        {
            glAnimator.InitGLWindow(this.pictureBox, this.Width, this.Height, 16);
        }

        /// <summary>
        /// アニメーションタイマー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void animationTimer_Tick(object sender, EventArgs e)
        {
            //BringToFront();
            glAnimator.DrawGLScene();
        }
    }
}
