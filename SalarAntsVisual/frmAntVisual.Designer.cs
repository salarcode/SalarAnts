namespace SalarAntsVisual
{
	partial class frmAntVisual
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
			this.picMap = new System.Windows.Forms.PictureBox();
			this.btnExit = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.picMap)).BeginInit();
			this.SuspendLayout();
			// 
			// picMap
			// 
			this.picMap.Location = new System.Drawing.Point(12, 38);
			this.picMap.Margin = new System.Windows.Forms.Padding(20);
			this.picMap.Name = "picMap";
			this.picMap.Size = new System.Drawing.Size(395, 246);
			this.picMap.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.picMap.TabIndex = 0;
			this.picMap.TabStop = false;
			// 
			// btnExit
			// 
			this.btnExit.Location = new System.Drawing.Point(12, 12);
			this.btnExit.Name = "btnExit";
			this.btnExit.Size = new System.Drawing.Size(75, 23);
			this.btnExit.TabIndex = 1;
			this.btnExit.Text = "&Exit";
			this.btnExit.UseVisualStyleBackColor = true;
			this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
			// 
			// frmAntVisual
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoScroll = true;
			this.AutoSize = true;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ClientSize = new System.Drawing.Size(626, 410);
			this.Controls.Add(this.btnExit);
			this.Controls.Add(this.picMap);
			this.Name = "frmAntVisual";
			this.Text = "Ant Visualizer";
			((System.ComponentModel.ISupportInitialize)(this.picMap)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox picMap;
		private System.Windows.Forms.Button btnExit;
	}
}

