namespace MEME_Academic_Sample
{
    partial class VersionForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VersionForm));
            this.applicationVersionLabel = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // applicationVersionLabel
            // 
            this.applicationVersionLabel.AutoSize = true;
            this.applicationVersionLabel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.applicationVersionLabel.Font = new System.Drawing.Font("Segoe UI Symbol", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.applicationVersionLabel.Location = new System.Drawing.Point(8, 8);
            this.applicationVersionLabel.Name = "applicationVersionLabel";
            this.applicationVersionLabel.Size = new System.Drawing.Size(206, 21);
            this.applicationVersionLabel.TabIndex = 1;
            this.applicationVersionLabel.Text = "Application Name + Version";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(472, 8);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 100);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 6;
            this.pictureBox1.TabStop = false;
            // 
            // VersionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 121);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.applicationVersionLabel);
            this.MaximumSize = new System.Drawing.Size(600, 160);
            this.MinimumSize = new System.Drawing.Size(600, 160);
            this.Name = "VersionForm";
            this.Text = "Version";
            this.Load += new System.EventHandler(this.VersionForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label applicationVersionLabel;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}