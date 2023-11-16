using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PVPlus.UI
{
    public partial class MainForm : Form
    {
        public MainPVForm mainPVForm;
        public SamplePVForm samplePVForm;
        public LTFHelperForm2 ltfHelperForm2;
        public SamsungTextModifierForm samsungCvtForm;
        public TabPage AddFuncTab;
        public Size InitSize;

        public MainForm()
        {
            InitializeComponent();
        }

        public void AddForm(TabPage tp, Form f)
        {
            if(f == null)
            {
                tp.Controls.Clear();
                Refresh();
                return;
            }

            f.TopLevel = false;
            //no border if needed
            f.FormBorderStyle = FormBorderStyle.None;
            f.AutoScaleMode = AutoScaleMode.Dpi;

            if (!tp.Controls.Contains(f))
            {
                tp.Controls.Add(f);
                f.Dock = DockStyle.Fill;
                f.Show();
                Refresh();
            }
            Refresh();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            Text = $"PVPLUS v{version}";

            tabControl1.Dock = DockStyle.Fill;
            tabControl1.TabPages[0].Text = "MainPV";
            tabControl1.TabPages[1].Text = "Sample";
            tabControl1.TabPages[2].Text = "LTFHelper";
            tabControl1.TabPages[3].Text = "추가기능";

            mainPVForm = new MainPVForm(this);
            samplePVForm = new SamplePVForm(mainPVForm);
            ltfHelperForm2 = new LTFHelperForm2();

            AddForm(tabControl1.SelectedTab, mainPVForm);
            AddFuncTab = tabControl1.TabPages[3];

            mainPVForm.LoadConfigureData();
            samplePVForm.LoadConfigureData();

            InitSize = new Size(Size.Width, Size.Height);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mainPVForm.SaveConfigureData();
            samplePVForm.SaveConfigureData();
            Properties.Settings.Default.Save();

            mainPVForm.Close();
            samplePVForm.Close();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab.Text == "LTFHelper")
            {
                this.Size = new Size(InitSize.Width, InitSize.Height + 95);
            }
            else
            {
                this.Size = InitSize;
            }

            if (tabControl1.SelectedTab.Text == "MainPV") AddForm(tabControl1.SelectedTab, mainPVForm);
            if (tabControl1.SelectedTab.Text == "Sample") AddForm(tabControl1.SelectedTab, samplePVForm);
            if (tabControl1.SelectedTab.Text == "LTFHelper") AddForm(tabControl1.SelectedTab, ltfHelperForm2);
        }

        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            //TabPage page = tabControl1.TabPages[e.Index];
            //e.Graphics.FillRectangle(new SolidBrush(page.BackColor), e.Bounds);

            //Rectangle paddedBounds = e.Bounds;
            //int yOffset = (e.State == DrawItemState.Selected) ? -2 : 1;
            //paddedBounds.Offset(1, yOffset);
            //TextRenderer.DrawText(e.Graphics, page.Text, e.Font, paddedBounds, page.ForeColor);
        }
    }
}
