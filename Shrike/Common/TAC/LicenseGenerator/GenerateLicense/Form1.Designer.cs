namespace GenerateLicense
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPageTemplate = new System.Windows.Forms.TabPage();
            this.tabPageGenerate = new System.Windows.Forms.TabPage();
            this.buttonLoadKeys = new System.Windows.Forms.Button();
            this.buttonGenerateMasterKeys = new System.Windows.Forms.Button();
            this.buttonLoadTemplate = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tbCustomer = new System.Windows.Forms.TextBox();
            this.tbSKU = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbTemplateText = new System.Windows.Forms.TextBox();
            this.btnGenerateLicense = new System.Windows.Forms.Button();
            this.btnSavePublicKey = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.btnValidateLicense = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPageTemplate.SuspendLayout();
            this.tabPageGenerate.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabPage1);
            this.tabControl.Controls.Add(this.tabPageTemplate);
            this.tabControl.Controls.Add(this.tabPageGenerate);
            this.tabControl.Controls.Add(this.tabPage2);
            this.tabControl.Location = new System.Drawing.Point(5, 3);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(559, 445);
            this.tabControl.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.btnSavePublicKey);
            this.tabPage1.Controls.Add(this.buttonGenerateMasterKeys);
            this.tabPage1.Controls.Add(this.buttonLoadKeys);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(551, 419);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "License Generation Keys";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPageTemplate
            // 
            this.tabPageTemplate.Controls.Add(this.buttonLoadTemplate);
            this.tabPageTemplate.Location = new System.Drawing.Point(4, 22);
            this.tabPageTemplate.Name = "tabPageTemplate";
            this.tabPageTemplate.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageTemplate.Size = new System.Drawing.Size(551, 417);
            this.tabPageTemplate.TabIndex = 1;
            this.tabPageTemplate.Text = "License Template";
            this.tabPageTemplate.UseVisualStyleBackColor = true;
            // 
            // tabPageGenerate
            // 
            this.tabPageGenerate.Controls.Add(this.btnGenerateLicense);
            this.tabPageGenerate.Controls.Add(this.tbTemplateText);
            this.tabPageGenerate.Controls.Add(this.tbSKU);
            this.tabPageGenerate.Controls.Add(this.tbCustomer);
            this.tabPageGenerate.Controls.Add(this.label2);
            this.tabPageGenerate.Controls.Add(this.label3);
            this.tabPageGenerate.Controls.Add(this.label1);
            this.tabPageGenerate.Location = new System.Drawing.Point(4, 22);
            this.tabPageGenerate.Name = "tabPageGenerate";
            this.tabPageGenerate.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageGenerate.Size = new System.Drawing.Size(551, 417);
            this.tabPageGenerate.TabIndex = 2;
            this.tabPageGenerate.Text = "Generate License";
            this.tabPageGenerate.UseVisualStyleBackColor = true;
            // 
            // buttonLoadKeys
            // 
            this.buttonLoadKeys.Location = new System.Drawing.Point(15, 12);
            this.buttonLoadKeys.Name = "buttonLoadKeys";
            this.buttonLoadKeys.Size = new System.Drawing.Size(249, 35);
            this.buttonLoadKeys.TabIndex = 0;
            this.buttonLoadKeys.Text = "Load Master Keys";
            this.buttonLoadKeys.UseVisualStyleBackColor = true;
            this.buttonLoadKeys.Click += new System.EventHandler(this.buttonLoadKeys_Click);
            // 
            // buttonGenerateMasterKeys
            // 
            this.buttonGenerateMasterKeys.Location = new System.Drawing.Point(15, 51);
            this.buttonGenerateMasterKeys.Name = "buttonGenerateMasterKeys";
            this.buttonGenerateMasterKeys.Size = new System.Drawing.Size(249, 37);
            this.buttonGenerateMasterKeys.TabIndex = 0;
            this.buttonGenerateMasterKeys.Text = "Generate Master Keys";
            this.buttonGenerateMasterKeys.UseVisualStyleBackColor = true;
            this.buttonGenerateMasterKeys.Click += new System.EventHandler(this.buttonGenerateMasterKeys_Click);
            // 
            // buttonLoadTemplate
            // 
            this.buttonLoadTemplate.Enabled = false;
            this.buttonLoadTemplate.Location = new System.Drawing.Point(6, 6);
            this.buttonLoadTemplate.Name = "buttonLoadTemplate";
            this.buttonLoadTemplate.Size = new System.Drawing.Size(249, 35);
            this.buttonLoadTemplate.TabIndex = 1;
            this.buttonLoadTemplate.Text = "Load License Template";
            this.buttonLoadTemplate.UseVisualStyleBackColor = true;
            this.buttonLoadTemplate.Click += new System.EventHandler(this.buttonLoadTemplate_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Customer";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "SKU Code";
            // 
            // tbCustomer
            // 
            this.tbCustomer.Location = new System.Drawing.Point(72, 13);
            this.tbCustomer.Name = "tbCustomer";
            this.tbCustomer.Size = new System.Drawing.Size(462, 20);
            this.tbCustomer.TabIndex = 1;
            // 
            // tbSKU
            // 
            this.tbSKU.Location = new System.Drawing.Point(72, 51);
            this.tbSKU.Name = "tbSKU";
            this.tbSKU.Size = new System.Drawing.Size(462, 20);
            this.tbSKU.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 91);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(51, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Template";
            // 
            // tbTemplateText
            // 
            this.tbTemplateText.Location = new System.Drawing.Point(72, 88);
            this.tbTemplateText.Multiline = true;
            this.tbTemplateText.Name = "tbTemplateText";
            this.tbTemplateText.ReadOnly = true;
            this.tbTemplateText.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbTemplateText.Size = new System.Drawing.Size(462, 161);
            this.tbTemplateText.TabIndex = 1;
            // 
            // btnGenerateLicense
            // 
            this.btnGenerateLicense.Enabled = false;
            this.btnGenerateLicense.Location = new System.Drawing.Point(285, 327);
            this.btnGenerateLicense.Name = "btnGenerateLicense";
            this.btnGenerateLicense.Size = new System.Drawing.Size(249, 35);
            this.btnGenerateLicense.TabIndex = 2;
            this.btnGenerateLicense.Text = "Generate License";
            this.btnGenerateLicense.UseVisualStyleBackColor = true;
            this.btnGenerateLicense.Click += new System.EventHandler(this.btnGenerateLicense_Click);
            // 
            // btnSavePublicKey
            // 
            this.btnSavePublicKey.Enabled = false;
            this.btnSavePublicKey.Location = new System.Drawing.Point(15, 94);
            this.btnSavePublicKey.Name = "btnSavePublicKey";
            this.btnSavePublicKey.Size = new System.Drawing.Size(249, 37);
            this.btnSavePublicKey.TabIndex = 0;
            this.btnSavePublicKey.Text = "Save Public Key File";
            this.btnSavePublicKey.UseVisualStyleBackColor = true;
            this.btnSavePublicKey.Click += new System.EventHandler(this.btnSavePublicKey_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.btnValidateLicense);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(551, 419);
            this.tabPage2.TabIndex = 3;
            this.tabPage2.Text = "Check License";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // btnValidateLicense
            // 
            this.btnValidateLicense.Enabled = false;
            this.btnValidateLicense.Location = new System.Drawing.Point(6, 6);
            this.btnValidateLicense.Name = "btnValidateLicense";
            this.btnValidateLicense.Size = new System.Drawing.Size(249, 35);
            this.btnValidateLicense.TabIndex = 1;
            this.btnValidateLicense.Text = "Validate License File";
            this.btnValidateLicense.UseVisualStyleBackColor = true;
            this.btnValidateLicense.Click += new System.EventHandler(this.btnValidateLicense_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(570, 451);
            this.Controls.Add(this.tabControl);
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "License Generator";
            this.tabControl.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPageTemplate.ResumeLayout(false);
            this.tabPageGenerate.ResumeLayout(false);
            this.tabPageGenerate.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPageTemplate;
        private System.Windows.Forms.TabPage tabPageGenerate;
        private System.Windows.Forms.Button buttonGenerateMasterKeys;
        private System.Windows.Forms.Button buttonLoadKeys;
        private System.Windows.Forms.Button buttonLoadTemplate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbSKU;
        private System.Windows.Forms.TextBox tbCustomer;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbTemplateText;
        private System.Windows.Forms.Button btnGenerateLicense;
        private System.Windows.Forms.Button btnSavePublicKey;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button btnValidateLicense;
    }
}

