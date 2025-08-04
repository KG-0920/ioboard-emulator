namespace APP_B
{
    partial class MainForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。
        /// このメソッドの内容をコード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.textLog = new System.Windows.Forms.TextBox();
            this.numericPort = new System.Windows.Forms.NumericUpDown();
            this.checkOutput = new System.Windows.Forms.CheckBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.btnRead = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numericPort)).BeginInit();
            this.SuspendLayout();
            // 
            // textLog
            // 
            this.textLog.Location = new System.Drawing.Point(12, 12);
            this.textLog.Multiline = true;
            this.textLog.Name = "textLog";
            this.textLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textLog.Size = new System.Drawing.Size(360, 150);
            this.textLog.TabIndex = 0;
            // 
            // numericPort
            // 
            this.numericPort.Location = new System.Drawing.Point(12, 180);
            this.numericPort.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.numericPort.Name = "numericPort";
            this.numericPort.Size = new System.Drawing.Size(120, 23);
            this.numericPort.TabIndex = 1;
            // 
            // checkOutput
            // 
            this.checkOutput.AutoSize = true;
            this.checkOutput.Location = new System.Drawing.Point(150, 182);
            this.checkOutput.Name = "checkOutput";
            this.checkOutput.Size = new System.Drawing.Size(81, 19);
            this.checkOutput.TabIndex = 2;
            this.checkOutput.Text = "出力ON/OFF";
            this.checkOutput.UseVisualStyleBackColor = true;
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(12, 220);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(100, 23);
            this.btnSend.TabIndex = 3;
            this.btnSend.Text = "出力制御";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // btnRead
            // 
            this.btnRead.Location = new System.Drawing.Point(130, 220);
            this.btnRead.Name = "btnRead";
            this.btnRead.Size = new System.Drawing.Size(100, 23);
            this.btnRead.TabIndex = 4;
            this.btnRead.Text = "入力読取";
            this.btnRead.UseVisualStyleBackColor = true;
            this.btnRead.Click += new System.EventHandler(this.btnRead_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 261);
            this.Controls.Add(this.btnRead);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.checkOutput);
            this.Controls.Add(this.numericPort);
            this.Controls.Add(this.textLog);
            this.Name = "MainForm";
            this.Text = "APP_B";
            ((System.ComponentModel.ISupportInitialize)(this.numericPort)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox textLog;
        private System.Windows.Forms.NumericUpDown numericPort;
        private System.Windows.Forms.CheckBox checkOutput;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnRead;
    }
}
