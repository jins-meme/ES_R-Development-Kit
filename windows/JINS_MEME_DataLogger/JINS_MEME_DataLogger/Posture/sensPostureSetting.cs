using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace JINS_MEME_DataLogger
{
    public partial class sensPostureSetting : Form
    {
        /// <summary>
        /// Singletonで実装。
        /// </summary>
        private SettingTable setting = new SettingTable("Tracer");
        /// <summary>
        /// 変更前方位角値
        /// </summary>
        private float genAngleH = 0;
        /// <summary>
        /// 変更前仰角値
        /// </summary>
        private float genAngleV = 0;
        /// <summary>
        /// 変更前中心からの距離値
        /// </summary>
        private float genDistance = 0;
        /// <summary>
        /// オーナーフォーム
        /// </summary>
        private UserCtlSensorPosture ownerForm=null;


        public sensPostureSetting(UserCtlSensorPosture ownerForm)
        {
            this.ownerForm = ownerForm;
            InitializeComponent();
            // 初期設定
            SetInit();

        }
        /// <summary>
        /// 初期設定
        /// </summary>
        private void SetInit() 
        {
            genAngleH = ownerForm.GraphLockAtAngleH;
            genAngleV = ownerForm.GraphLockAtAngleV;
            genDistance = ownerForm.GraphLockAtDistance;

            trackBarAngleH.Value = (int)genAngleH;
            trackBarAngleV.Value = (int)genAngleV;
            trackBarDistance.Value = (int)(genDistance * 10.0);
            
            lblAngleH.Text = trackBarAngleH.Value.ToString();
            lblAngleV.Text = trackBarAngleV.Value.ToString();
            lblDistance.Text = (trackBarDistance.Value / 10.0).ToString("0.0");

        }
        /// <summary>
        /// 方位角スクロール
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trackBarAngleH_Scroll(object sender, EventArgs e)
        {
            lblAngleH.Text = trackBarAngleH.Value.ToString();
            ownerForm.GraphLockAtAngleH = trackBarAngleH.Value;
        }
        /// <summary>
        /// 仰角スクロール
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trackBarAngleV_Scroll(object sender, EventArgs e)
        {
            lblAngleV.Text = trackBarAngleV.Value.ToString();
            ownerForm.GraphLockAtAngleV = trackBarAngleV.Value;

        }
        /// <summary>
        /// 変更前中心からの距離
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trackBarDistance_Scroll(object sender, EventArgs e)
        {
            lblDistance.Text = (trackBarDistance.Value / 10.0).ToString("0.0");
            ownerForm.GraphLockAtDistance = (float)(trackBarDistance.Value / 10.0);

        }
        /// <summary>
        /// 設定ボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReg_Click(object sender, EventArgs e)
        {
            setting.SetDouble("SensorPosture", "SensorPosture_LockAtAngleH", (double)trackBarAngleH.Value);
            setting.SetDouble("SensorPosture", "SensorPosture_LockAtAngleV", (double)trackBarAngleV.Value);
            setting.SetDouble("SensorPosture", "SensorPosture_LockAtDistance", (double)trackBarDistance.Value / 10.0);

            this.Close();
        }
        /// <summary>
        /// キャンセルボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClose_Click(object sender, EventArgs e)
        {
            ownerForm.GraphLockAtAngleH=genAngleH;
            ownerForm.GraphLockAtAngleV = genAngleV;
            ownerForm.GraphLockAtDistance = genDistance;

            this.Close();
        }
        
    }
}
