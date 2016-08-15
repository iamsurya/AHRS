namespace AHRSTEST
{
    partial class Form1
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
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbDevices = new System.Windows.Forms.ComboBox();
            this.button2 = new System.Windows.Forms.Button();
            this.cmbDebugLevel = new System.Windows.Forms.ComboBox();
            this.tbBetaVal = new System.Windows.Forms.TextBox();
            this.lbBeta = new System.Windows.Forms.Label();
            this.tbDebugFile = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(188, 181);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Go";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(202, 230);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Waiting";
            // 
            // cmbDevices
            // 
            this.cmbDevices.FormattingEnabled = true;
            this.cmbDevices.Location = new System.Drawing.Point(36, 35);
            this.cmbDevices.Name = "cmbDevices";
            this.cmbDevices.Size = new System.Drawing.Size(121, 21);
            this.cmbDevices.TabIndex = 2;
            this.cmbDevices.SelectedIndexChanged += new System.EventHandler(this.cmbDevices_SelectedIndexChanged);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(314, 181);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 3;
            this.button2.Text = "MultiWrite";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // cmbDebugLevel
            // 
            this.cmbDebugLevel.FormattingEnabled = true;
            this.cmbDebugLevel.Items.AddRange(new object[] {
            "CosTheta",
            "Quaternions",
            "Linear Acceleration",
            "Euler Angles"});
            this.cmbDebugLevel.Location = new System.Drawing.Point(268, 35);
            this.cmbDebugLevel.Name = "cmbDebugLevel";
            this.cmbDebugLevel.Size = new System.Drawing.Size(121, 21);
            this.cmbDebugLevel.TabIndex = 4;
            // 
            // tbBetaVal
            // 
            this.tbBetaVal.Location = new System.Drawing.Point(57, 73);
            this.tbBetaVal.Name = "tbBetaVal";
            this.tbBetaVal.Size = new System.Drawing.Size(100, 20);
            this.tbBetaVal.TabIndex = 5;
            this.tbBetaVal.Text = "1.0";
            // 
            // lbBeta
            // 
            this.lbBeta.AutoSize = true;
            this.lbBeta.Location = new System.Drawing.Point(13, 79);
            this.lbBeta.Name = "lbBeta";
            this.lbBeta.Size = new System.Drawing.Size(29, 13);
            this.lbBeta.TabIndex = 6;
            this.lbBeta.Text = "Beta";
            // 
            // tbDebugFile
            // 
            this.tbDebugFile.Location = new System.Drawing.Point(110, 114);
            this.tbDebugFile.Name = "tbDebugFile";
            this.tbDebugFile.Size = new System.Drawing.Size(206, 20);
            this.tbDebugFile.TabIndex = 7;
            this.tbDebugFile.Text = "C:\\temp\\quatdebug.txt";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 121);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "ASCII Output File";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(458, 261);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbDebugFile);
            this.Controls.Add(this.lbBeta);
            this.Controls.Add(this.tbBetaVal);
            this.Controls.Add(this.cmbDebugLevel);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.cmbDevices);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "AHRS TEST";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbDevices;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ComboBox cmbDebugLevel;
        private System.Windows.Forms.TextBox tbBetaVal;
        private System.Windows.Forms.Label lbBeta;
        private System.Windows.Forms.TextBox tbDebugFile;
        private System.Windows.Forms.Label label2;
    }
}

