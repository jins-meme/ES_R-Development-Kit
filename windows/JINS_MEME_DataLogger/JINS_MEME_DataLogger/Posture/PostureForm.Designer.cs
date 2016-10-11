namespace JINS_MEME_DataLogger
{
    partial class PostureForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.postureControl = new JINS_MEME_DataLogger.UserCtlSensorPosture();
            this.SuspendLayout();
            // 
            // postureControl
            // 
            this.postureControl.DebugMode = true;
            this.postureControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.postureControl.GentenLineColorXBlue = 1F;
            this.postureControl.GentenLineColorXGreen = 1F;
            this.postureControl.GentenLineColorXRed = 1F;
            this.postureControl.GentenLineColorYBlue = 1F;
            this.postureControl.GentenLineColorYGreen = 1F;
            this.postureControl.GentenLineColorYRed = 1F;
            this.postureControl.GentenLineColorZBlue = 1F;
            this.postureControl.GentenLineColorZGreen = 1F;
            this.postureControl.GentenLineColorZRed = 1F;
            this.postureControl.GraphLockAtAngleH = 135F;
            this.postureControl.GraphLockAtAngleV = 30F;
            this.postureControl.GraphLockAtDistance = 2F;
            this.postureControl.Location = new System.Drawing.Point(0, 0);
            this.postureControl.Name = "postureControl";
            this.postureControl.SensHoko = JINS_MEME_DataLogger.UserCtlSensorPostureCtl.SENS_HOKOU.SEI_HOKO;
            this.postureControl.Size = new System.Drawing.Size(294, 273);
            this.postureControl.TabIndex = 0;
            // 
            // PostureForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(294, 273);
            this.ControlBox = false;
            this.Controls.Add(this.postureControl);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "PostureForm";
            this.ShowInTaskbar = false;
            this.Text = "PostureForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PostureForm_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private UserCtlSensorPosture postureControl;
    }
}