using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using PVPlus.RULES;
using PVPlus.PVCALCULATOR;
using Squirrel;
using System.Net;
using System.Reflection;
using PVPlus.UI;
using System.Configuration;

namespace PVPlus
{
    public partial class MainPVForm : Form
    {
        public MainPVForm()
        {
            InitializeComponent();
        }

        public MainPVForm(UI.MainForm mainForm) : this()
        {
            this.mainForm = mainForm;
        }

        public void SetConfigure()
        {
            FileInfo excelFileInfo = new FileInfo(textBoxExcelPath.Text);
            if (!excelFileInfo.Exists)
            {
                throw new Exception("엑셀파일이 존재하지 않습니다. - " + excelFileInfo.Name + '\n');
            }

            Configure.WorkingDI = excelFileInfo.Directory
                .GetDirectories()
                .Where(x => x.Name == "Data")
                .SingleOrDefault();
            if (Configure.WorkingDI == null)
            {
                throw new Exception("엑셀 데이터 출력을 실행하십시오." + '\n');
            }

            if (radioButtonP.Checked)
            {
                Configure.PVSTableInfo = new FileInfo(textBoxPPath.Text);
                Configure.TableType = TableType.P;
                Configure.LimitExcessChecked = checkBoxLimitCheck.Checked;
            }
            else if (radioButtonV.Checked)
            {
                Configure.PVSTableInfo = new FileInfo(textBoxVPath.Text);
                Configure.TableType = TableType.V;
            }
            else if (radioButtonS.Checked)
            {
                Configure.PVSTableInfo = new FileInfo(textBoxSPath.Text);
                Configure.TableType = TableType.StdAlpha;
                Configure.LimitExcessChecked = checkBoxLimitCheck.Checked;
            }

            Configure.CompanyRule = GetCompanyRule(comboBoxCompany.Text);

            if (checkBox구분자.Checked)
            {
                Configure.SeperationType = "1";

                if (TextBoxDelimiter.Text.Length == 0)
                {
                    Configure.Delimiter = '\t';
                }
                else if (TextBoxDelimiter.Text.Length > 1)
                {
                    throw new Exception("구분자의 크기를 1로 조정하십시오." + '\n');
                }
                else
                {
                    Configure.Delimiter = Convert.ToChar(TextBoxDelimiter.Text);
                }
            }
            else
            {
                Configure.SeperationType = "2";
                Configure.Delimiter = '\t'; //사용X 
            }

            Configure.ProductCode = textBoxProduct.Text;

            if (!Configure.PVSTableInfo.Exists)
            {
                throw new Exception("파일이 존재하지 않습니다. - " + Configure.PVSTableInfo.Name + '\n');
            }
        }

        public void ReportExeption(Exception ex)
        {
            this.Invoke(new MethodInvoker(
               delegate ()
               {
                   textBoxStatus.AppendText(ex.Message + '\n');
               }));
        }

        public void LoadConfigureData()
        {
            textBoxExcelPath.Text = Properties.Settings.Default.ExcelPath;
            textBoxPPath.Text = Properties.Settings.Default.PTablePath;
            textBoxVPath.Text = Properties.Settings.Default.VTablePath;
            textBoxSPath.Text = Properties.Settings.Default.STablePath;

            textBoxProduct.Text = Properties.Settings.Default.ProductCode;
            comboBoxCompany.SelectedItem = Properties.Settings.Default.CompanyName;
            checkBox구분자.Checked = Properties.Settings.Default.DelimeterChecked;
            TextBoxDelimiter.Text = Properties.Settings.Default.Delimiter;

            radioButtonP.Checked = true;
            if (Properties.Settings.Default.TableSelected == "P") radioButtonP.Checked = true;
            if (Properties.Settings.Default.TableSelected == "V") radioButtonV.Checked = true;
            if (Properties.Settings.Default.TableSelected == "S") radioButtonS.Checked = true;
        }

        public void SaveConfigureData()
        {
            Properties.Settings.Default.ExcelPath = textBoxExcelPath.Text;
            Properties.Settings.Default.PTablePath = textBoxPPath.Text;
            Properties.Settings.Default.VTablePath = textBoxVPath.Text;
            Properties.Settings.Default.STablePath = textBoxSPath.Text;

            Properties.Settings.Default.ProductCode = textBoxProduct.Text;
            Properties.Settings.Default.CompanyName = comboBoxCompany.Text;
            Properties.Settings.Default.DelimeterChecked = checkBox구분자.Checked;
            Properties.Settings.Default.Delimiter = TextBoxDelimiter.Text;
            Properties.Settings.Default.TableSelected = radioButtonP.Checked ? "P" : (radioButtonV.Checked ? "V" : "S");

            Properties.Settings.Default.Save();
        }

        private PV pv;

        private UI.MainForm mainForm;

        private DateTime startTime;

        private ICompanyRule GetCompanyRule(string companyName)
        {
            Type type = Type.GetType($"PVPlus.RULES.{companyName}");
            return (ICompanyRule)Activator.CreateInstance(type, null);
        }

        private void MainPVForm_Load(object sender, EventArgs e)
        {
            lblStatus.Text = "";
            lblTime.Text = "";

            string[] companies = Enum.GetNames(typeof(CompanyNames));
            comboBoxCompany.Items.AddRange(companies);

            string[] openFiles = new string[] { " ", "작업폴더", "검증폴더", "테이블원본", "정상건", "오차건", "오차건원본", "오류건", "SInfo계산값", "SInfo한도계산값" };
            comboBoxFileOpen.Items.AddRange(openFiles);

            CheckVersion();
        }

        private void MainPVForm_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
        
        private void btn시작_Click(object sender, EventArgs e)
        {
            try
            {
                SetConfigure();

                startTime = DateTime.Now;
                lblTime.Text = new TimeSpan(0, 0, 0).ToString(@"hh\:mm\:ss", null);
                timer1.Enabled = true;
                button취소.Enabled = true;
                btn시작.Enabled = false;
                PVBackgroundWorker.RunWorkerAsync();
            }
            catch(Exception ex)
            {
                ReportExeption(ex);
            }
        }

        private void btn취소_Click(object sender, EventArgs e)
        {
            PVBackgroundWorker.CancelAsync();
        }

        private async void btnS산출_Click(object sender, EventArgs e)
        {
            try
            {
                SetConfigure();
                Configure.TableType = TableType.SRatio;

                lblStatus.Text = "S산출중...";

                var SCalTask = Task.Run(() =>
                {
                    new PV(this).EvaluateSInfo();
                });
                await SCalTask;

                lblStatus.Text = "";
                textBoxStatus.AppendText($"{Configure.ProductCode} S산출완료." + '\n');
            }
            catch (Exception ex)
            {
                ReportExeption(ex);
            }
        }

        private void btnOpenPTable_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofdlg = new OpenFileDialog())
            {
                ofdlg.ShowDialog();

                if (ofdlg.FileName != "")
                {
                    textBoxPPath.Text = ofdlg.FileName;
                }
            }
        }

        private void btnOpenVTable_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofdlg = new OpenFileDialog())
            {
                ofdlg.ShowDialog();

                if (ofdlg.FileName != "")
                {
                    textBoxVPath.Text = ofdlg.FileName;
                }
            }
        }

        private void btnOpenSTable_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofdlg = new OpenFileDialog())
            {
                ofdlg.ShowDialog();

                if (ofdlg.FileName != "")
                {
                    textBoxSPath.Text = ofdlg.FileName;
                }
            }
        }

        private void btnOpenExcelPath_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofdlg = new OpenFileDialog())
            {
                ofdlg.ShowDialog();

                if (ofdlg.FileName != "")
                {
                    textBoxExcelPath.Text = ofdlg.FileName;
                }
            }
        }
        
        private void checkBox구분자_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox구분자.Checked)
            {
                TextBoxDelimiter.ReadOnly = false;
            }
            else
            {
                TextBoxDelimiter.Clear();
                TextBoxDelimiter.ReadOnly = true;
            }
        }

        private void PVBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            pv = new PV(this);
            pv.Run();
        }

        private void PVBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            timer1.Enabled = false;
            if (lblStatus.Text != "")
            {
                textBoxStatus.AppendText($"{Configure.TableType.ToString()}결과({textBoxProduct.Text}): {lblStatus.Text}, 걸린시간: {lblTime.Text}\n");
            }
            lblStatus.Text = "";
            lblTime.Text = "";
            button취소.Enabled = false;
            btn시작.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lblTime.Text = (DateTime.Now - startTime).ToString(@"hh\:mm\:ss", null);
            this.Invoke(new Action(() => lblStatus.Text = pv.ProgressMsg));
            this.Invoke(new Action(() => pv.IsCanceled = PVBackgroundWorker.CancellationPending));
        }

        private void comboBoxCompany_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBoxCompany.Text == "Samsung")
            {
                mainForm.AddFuncTab.Text = "추가기능(Samsung)";
                mainForm.AddForm(mainForm.AddFuncTab, new SamsungTextModifierForm());
            }
            else
            {
                mainForm.AddFuncTab.Text = "추가기능";
                mainForm.AddForm(mainForm.AddFuncTab, null);
            }    
        }

        private void radioButtonS_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonS.Checked)
            {
                checkBoxLimitCheck.Visible = true;
                checkBoxLimitCheck.Text = "신계약비";
            }
            else
            {
                checkBoxLimitCheck.Visible = false;
            }
        }

        private void radioButtonP_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonP.Checked)
            {
                checkBoxLimitCheck.Visible = true;
                checkBoxLimitCheck.Text = "부가보험료";
            }
            else
            {
                checkBoxLimitCheck.Visible = false;
            }
        }

        private void comboBoxFileOpen_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (comboBoxFileOpen.Text == "작업폴더")
                {
                    FileInfo excelFileInfo = new FileInfo(textBoxExcelPath.Text);
                    Process.Start($@"{excelFileInfo.Directory.FullName}");
                }
                else if (comboBoxFileOpen.Text == "검증폴더")
                {
                    string selectedPath = "";

                    if (radioButtonP.Checked) selectedPath = textBoxPPath.Text;
                    if (radioButtonV.Checked) selectedPath = textBoxVPath.Text;
                    if (radioButtonS.Checked) selectedPath = textBoxSPath.Text;

                    FileInfo fileInfo = new FileInfo(selectedPath);
                    Process.Start($@"{fileInfo.Directory.FullName}");
                }
                else if (comboBoxFileOpen.Text == "테이블원본" || comboBoxFileOpen.Text == "정상건" || comboBoxFileOpen.Text == "오차건" || comboBoxFileOpen.Text == "오차건원본" || comboBoxFileOpen.Text == "오류건")
                {
                    string selectedPath = "";

                    if (radioButtonP.Checked) selectedPath = textBoxPPath.Text;
                    if (radioButtonV.Checked) selectedPath = textBoxVPath.Text;
                    if (radioButtonS.Checked) selectedPath = textBoxSPath.Text;

                    FileInfo fileInfo = new FileInfo(selectedPath);

                    if (comboBoxFileOpen.Text == "정상건") selectedPath = fileInfo.FullName.Replace(fileInfo.Extension, "") + "_정상건" + fileInfo.Extension;
                    if (comboBoxFileOpen.Text == "오차건") selectedPath = fileInfo.FullName.Replace(fileInfo.Extension, "") + "_오차건" + fileInfo.Extension;
                    if (comboBoxFileOpen.Text == "오차건원본") selectedPath = fileInfo.FullName.Replace(fileInfo.Extension, "") + "_오차건원본" + fileInfo.Extension;
                    if (comboBoxFileOpen.Text == "오류건") selectedPath = fileInfo.FullName.Replace(fileInfo.Extension, "") + "_오류건원본" + fileInfo.Extension;

                    Process.Start($@"{selectedPath}");
                }
                else if (comboBoxFileOpen.Text == "SInfo계산값")
                {
                    FileInfo excelFileInfo = new FileInfo(textBoxExcelPath.Text);
                    Process.Start($@"{excelFileInfo.Directory.FullName}\Data\EvaluatedSInfo.txt");
                }
                else if (comboBoxFileOpen.Text == "SInfo한도계산값")
                {
                    FileInfo excelFileInfo = new FileInfo(textBoxExcelPath.Text);
                    Process.Start($@"{excelFileInfo.Directory.FullName}\Data\AlphaExcessCheck.txt");
                }
            }
            catch(Exception ex)
            {
                ReportExeption(ex);
            }
        }

        private async void ExcessLimitChkBtn_Click(object sender, EventArgs e)
        {
            try
            {
                SetConfigure();
                Configure.TableType = TableType.SRatio;

                lblStatus.Text = "한도 계산중...";

                var SCalTask = Task.Run(() =>
                {
                    new PV(this).CheckExcess();
                });
                await SCalTask;

                lblStatus.Text = "";
                textBoxStatus.AppendText($"{Configure.ProductCode} 한도계산완료." + '\n');
            }
            catch (Exception ex)
            {
                ReportExeption(ex);
            }
        }

        private void textBoxStatus_TextChanged(object sender, EventArgs e)
        {

        }

        #region Patch
        public Version LastestVersion { get; set; }

        public Version CurrentVersion { get; set; }

        private void CheckVersion()
        {
            DirectoryInfo rootPath = new DirectoryInfo(Application.StartupPath).Parent.CreateSubdirectory("PatchFiles");

            if (new FileInfo($@"{Path.GetTempPath()}\userTemp.config").Exists)
            {
                SaveConfigureData();
                mainForm.samplePVForm.SaveConfigureData();
                GetSetting();
                new FileInfo($@"{Path.GetTempPath()}\userTemp.config").Delete();
                Properties.Settings.Default.Upgrade();
                LoadConfigureData();
                mainForm.samplePVForm.LoadConfigureData();
            }

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

                CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                LastestVersion = releaseVersions.Last();


                if (CurrentVersion <= LastestVersion)
                {
                    linkLabel1.Text = $"v{LastestVersion.ToString(3)} 패치";
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void Patch()
        {
            WebClient client = new WebClient();
            DialogResult dialogResult = MessageBox.Show(text: $"새 버전 v{LastestVersion}이 확인 되었습니다. 패치를 진행 하시겠습니까?", buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Information, caption: "패치안내");

            if (dialogResult == DialogResult.Yes)
            {
                DirectoryInfo rootPath = new DirectoryInfo(Application.StartupPath).Parent.CreateSubdirectory("PatchFiles");
                string AppName = Assembly.GetExecutingAssembly().GetName().Name;

                if (new FileInfo($@"{Path.GetTempPath()}\userTemp.config").Exists)
                {
                    SaveConfigureData();
                    mainForm.samplePVForm.SaveConfigureData();
                    GetSetting();
                    new FileInfo($@"{Path.GetTempPath()}\userTemp.config").Delete();
                    Properties.Settings.Default.Upgrade();
                    LoadConfigureData();
                    mainForm.samplePVForm.LoadConfigureData();
                }

                try
                {
                    client.DownloadFile($"https://github.com/k-j-k/PVPlus/raw/master/Releases/{AppName}-{LastestVersion}-delta.nupkg", $@"{rootPath.FullName}\{AppName}-{LastestVersion}-delta.nupkg");
                    client.DownloadFile($"https://github.com/k-j-k/PVPlus/raw/master/Releases/{AppName}-{LastestVersion}-full.nupkg", $@"{rootPath.FullName}\{AppName}-{LastestVersion}-full.nupkg");
                    client.DownloadFile("https://github.com/k-j-k/PVPlus/raw/master/Releases/RELEASES", $@"{rootPath.FullName}\RELEASES");
                    SetSetting();

                    using (var mgr = new UpdateManager(rootPath.FullName))
                    {
                        await mgr.UpdateApp();
                    }

                    MessageBox.Show(text: "패치가 완료되었습니다. 재시작시 변경사항이 적용됩니다.", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information, caption: "패치완료");

                    Close();

                    UpdateManager.RestartApp();
                }
                catch
                {
                    MessageBox.Show(text: "예상치 못한 사유로 패치를 진행하지 못 했습니다.", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information, caption: "패치실패");
                }
            }



        }

        private static void GetSetting()
        {
            try
            {
                if (new FileInfo($@"{Path.GetTempPath()}\userTemp.config").Exists)
                {
                    new FileInfo($@"{Path.GetTempPath()}\userTemp.config").CopyTo(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath, true);
                }
            }
            catch
            {

            }
        }

        private static void SetSetting()
        {
            try
            {
                new FileInfo(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath).CopyTo($@"{Path.GetTempPath()}\userTemp.config", true);
            }
            catch
            {

            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Patch();
        }
        #endregion
    }
}
