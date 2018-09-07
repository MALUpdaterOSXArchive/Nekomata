namespace Nekomata
{
    partial class Settingsdlg
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.planned = new System.Windows.Forms.CheckBox();
            this.dropped = new System.Windows.Forms.CheckBox();
            this.onhold = new System.Windows.Forms.CheckBox();
            this.completed = new System.Windows.Forms.CheckBox();
            this.current = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.planned);
            this.groupBox1.Controls.Add(this.dropped);
            this.groupBox1.Controls.Add(this.onhold);
            this.groupBox1.Controls.Add(this.completed);
            this.groupBox1.Controls.Add(this.current);
            this.groupBox1.Location = new System.Drawing.Point(18, 46);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 171);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Set Update on Import";
            // 
            // planned
            // 
            this.planned.AutoSize = true;
            this.planned.Checked = global::Nekomata.Properties.Settings.Default.updateonimportplanned;
            this.planned.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::Nekomata.Properties.Settings.Default, "updateonimportplanned", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.planned.Location = new System.Drawing.Point(19, 113);
            this.planned.Name = "planned";
            this.planned.Size = new System.Drawing.Size(123, 17);
            this.planned.TabIndex = 4;
            this.planned.Text = "Plan to watch/read";
            this.planned.UseVisualStyleBackColor = true;
            this.planned.CheckedChanged += new System.EventHandler(this.planned_CheckedChanged);
            // 
            // dropped
            // 
            this.dropped.AutoSize = true;
            this.dropped.Checked = global::Nekomata.Properties.Settings.Default.updateonimportdropped;
            this.dropped.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::Nekomata.Properties.Settings.Default, "updateonimportdropped", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.dropped.Location = new System.Drawing.Point(19, 91);
            this.dropped.Name = "dropped";
            this.dropped.Size = new System.Drawing.Size(72, 17);
            this.dropped.TabIndex = 3;
            this.dropped.Text = "Dropped";
            this.dropped.UseVisualStyleBackColor = true;
            this.dropped.CheckedChanged += new System.EventHandler(this.dropped_CheckedChanged);
            // 
            // onhold
            // 
            this.onhold.AutoSize = true;
            this.onhold.Checked = global::Nekomata.Properties.Settings.Default.updateonimportonhold;
            this.onhold.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::Nekomata.Properties.Settings.Default, "updateonimportonhold", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.onhold.Location = new System.Drawing.Point(19, 67);
            this.onhold.Name = "onhold";
            this.onhold.Size = new System.Drawing.Size(70, 17);
            this.onhold.TabIndex = 2;
            this.onhold.Text = "On-hold";
            this.onhold.UseVisualStyleBackColor = true;
            this.onhold.CheckedChanged += new System.EventHandler(this.onhold_CheckedChanged);
            // 
            // completed
            // 
            this.completed.AutoSize = true;
            this.completed.Checked = global::Nekomata.Properties.Settings.Default.updateonimportcompleted;
            this.completed.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::Nekomata.Properties.Settings.Default, "updateonimportcompleted", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.completed.Location = new System.Drawing.Point(19, 45);
            this.completed.Name = "completed";
            this.completed.Size = new System.Drawing.Size(82, 17);
            this.completed.TabIndex = 1;
            this.completed.Text = "Completed";
            this.completed.UseVisualStyleBackColor = true;
            this.completed.CheckedChanged += new System.EventHandler(this.completed_CheckedChanged);
            // 
            // current
            // 
            this.current.AutoSize = true;
            this.current.Checked = global::Nekomata.Properties.Settings.Default.updateonimportcurrent;
            this.current.CheckState = System.Windows.Forms.CheckState.Checked;
            this.current.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::Nekomata.Properties.Settings.Default, "updateonimportcurrent", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.current.Location = new System.Drawing.Point(19, 22);
            this.current.Name = "current";
            this.current.Size = new System.Drawing.Size(123, 17);
            this.current.TabIndex = 0;
            this.current.Text = "Watching/Reading";
            this.current.UseVisualStyleBackColor = true;
            this.current.CheckedChanged += new System.EventHandler(this.current_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Exporting";
            // 
            // Settingsdlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(289, 244);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Settingsdlg";
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.Settings_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox planned;
        private System.Windows.Forms.CheckBox dropped;
        private System.Windows.Forms.CheckBox onhold;
        private System.Windows.Forms.CheckBox completed;
        private System.Windows.Forms.CheckBox current;
        private System.Windows.Forms.Label label1;
    }
}