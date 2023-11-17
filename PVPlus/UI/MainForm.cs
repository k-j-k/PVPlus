using Squirrel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
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

            Patch();
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

        private async void Patch()
        {
            DirectoryInfo rootPath = new DirectoryInfo(Application.StartupPath).Parent.CreateSubdirectory("PatchFiles");

            try
            {
                WebClient client = new WebClient();
                client.DownloadFile("https://github.com/k-j-k/PVPlus/raw/master/Releases/RELEASES", $@"{rootPath.FullName}\RELEASES");

                List<string> releaseList = new List<string>();

                using (StreamReader sr = new StreamReader($@"{rootPath.FullName}\RELEASES"))
                {
                    while (!sr.EndOfStream)
                    {
                        releaseList.Add(sr.ReadLine());
                    }
                }

                string AppName = Assembly.GetExecutingAssembly().GetName().Name;

                List<Version> releaseVersions = releaseList
                    .Select(x => new Version(x.Split(' ')[1].Split('-')[1]))
                    .Distinct()
                    .OrderBy(x => x).ToList();

                Version currendVersion = Assembly.GetExecutingAssembly().GetName().Version;
                Version lastestVersion = releaseVersions.Last();

                if (currendVersion <= lastestVersion)
                {
                    DialogResult dialogResult = MessageBox.Show(text: $"새 버전 {lastestVersion}이 확인 되었습니다. 패치를 진행 하시겠습니까?", buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Information, caption: "패치안내");
                    if (dialogResult == DialogResult.Yes)
                    {
                        try
                        {
                            client.DownloadFile($"https://github.com/k-j-k/PVPlus/raw/master/Releases/{AppName}-{lastestVersion}-delta.nupkg", $@"{rootPath.FullName}\{AppName}-{lastestVersion}-delta.nupkg");
                            client.DownloadFile($"https://github.com/k-j-k/PVPlus/raw/master/Releases/{AppName}-{lastestVersion}-full.nupkg", $@"{rootPath.FullName}\{AppName}-{lastestVersion}-full.nupkg");
                            client.DownloadFile("https://github.com/k-j-k/PVPlus/raw/master/Releases/RELEASES", $@"{rootPath.FullName}\RELEASES");

                            using (var mgr = new UpdateManager(rootPath.FullName))
                            {
                                await mgr.UpdateApp();
                            }

                            MessageBox.Show(text: "패치가 완료되었습니다. 재시작시 변경사항이 적용됩니다.", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information, caption: "패치완료");
                        }
                        catch
                        {
                            MessageBox.Show(text: "예상치 못한 사유로 패치를 진행하지 못 했습니다.", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information, caption: "패치실패");
                        }
                    }
                }
                else
                {

                }
            }
            catch
            {

            }


        }


    }
}
