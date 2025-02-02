﻿namespace TradeLink.AppKit
{
    partial class ResponseList
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ResponseList));
            this._list = new System.Windows.Forms.ListBox();
            this._choose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _list
            // 
            this._list.Dock = System.Windows.Forms.DockStyle.Fill;
            this._list.FormattingEnabled = true;
            this._list.Location = new System.Drawing.Point(0, 0);
            this._list.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this._list.Name = "_list";
            this._list.Size = new System.Drawing.Size(254, 228);
            this._list.TabIndex = 0;
            this._list.SelectedIndexChanged += new System.EventHandler(this._list_SelectedIndexChanged);
            // 
            // _choose
            // 
            this._choose.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._choose.Location = new System.Drawing.Point(0, 206);
            this._choose.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this._choose.Name = "_choose";
            this._choose.Size = new System.Drawing.Size(254, 22);
            this._choose.TabIndex = 1;
            this._choose.Text = "OK";
            this._choose.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this._choose.UseVisualStyleBackColor = true;
            this._choose.Visible = false;
            this._choose.Click += new System.EventHandler(this._choose_Click);
            // 
            // ResponseList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(254, 228);
            this.Controls.Add(this._choose);
            this.Controls.Add(this._list);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "ResponseList";
            this.ShowInTaskbar = false;
            this.Text = "Choose response to trade:";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox _list;
        private System.Windows.Forms.Button _choose;
    }
}