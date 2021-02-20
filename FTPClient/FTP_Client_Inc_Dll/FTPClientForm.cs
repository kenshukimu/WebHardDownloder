using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Permissions;
using System.IO;
using System.Net;
using System.Diagnostics;


namespace FTP_Client_Inc_Dll
{

    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class FTPClientForm : Form
    {
        CommonService _common = new CommonService();
        String strSpeed = "";
        String strLeftTime = "";

        protected static String ftpPath = @"ftp://webdisk.korcham.net";
        protected static String ftpUser = "webFTP";
        protected static String ftpPass = "111111";

        public FTPClientForm()
        {
            InitializeComponent();

            webBrowser1.ObjectForScripting = this;

        }

        public void ftpFileDownload(String files)
        {
            String[] _files = files.Split('^');

            if (textBox1.Text.IndexOf("\\") < 1)
            {
                DialogResult result = folderBrowserDialog1.ShowDialog();
                String path = folderBrowserDialog1.SelectedPath;
                textBox1.Text = path;
            }

            if (_files.Length == 1 && _files[0].Length < 2)
            {
                MessageBox.Show("저장할 파일을 선택하여 주시기 바랍니다.");
                return;
            }

            panel1.Visible = true;
            label20.Text = "다운로드파일 : -";

            progressBar1.Style = ProgressBarStyle.Blocks;
            label1.Text = files;

            listView1.View = View.Details;

            listView1.Columns.Add("파일명",700);
            listView1.Columns.Add("상태", 200);
            
            foreach (String _file in _files)
            {
                String[] filePath = _file.Split('/');
                String file = filePath[filePath.Length - 1];

                Boolean _hangul = extendtionYN(file);
               
                if (Path.GetExtension(file) != null && !Path.GetExtension(file).Equals("") && !_hangul)
                {
                    String[] _data = { file, "대기" };
                    ListViewItem item = new ListViewItem(_data);
                    listView1.Items.Add(item);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();

            String path = folderBrowserDialog1.SelectedPath;

            textBox1.Text = path;
        }

        void downloadFileFtp(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            String[] _downloadFIles = label1.Text.Split('^');
           
            ftpFileDownloadProcess(textBox1.Text, _downloadFIles);
        }

        public void ftpFileDownloadProcess(String path, String[] fileNames)
        {
            int fileCnt = 0;
            foreach (String fileName in fileNames)
            {
                String[] filePath = fileName.Split('/');
                String file = filePath[filePath.Length - 1];

                Boolean _hangul = extendtionYN(file);
               
                if (Path.GetExtension(file) == null || Path.GetExtension(file).Equals("") || _hangul)
                {
                    continue;
                }

                long fws = 0;
                // 파일을 다운로드한다.
                using (var res = _common.Connect(fileName, System.Net.WebRequestMethods.Ftp.DownloadFile, ref fws, ftpPath, ftpUser, ftpPass))
                {
                    using (var stream = res.GetResponseStream())
                    {
                        // stream을 통해 파일을 작성한다.
                        using (var fs = System.IO.File.Create(path + "\\" + file))
                        {
                            try
                            {
                                if (this.InvokeRequired)
                                {
                                    this.Invoke(new MethodInvoker(delegate()
                                        {
                                            label20.Text = "다운로드파일 : " + file;
                                        }));
                                }

                                byte[] buffer = new byte[10 * 1024 * 1024];
                                int read;
                                long total = 0;
                                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    fs.Write(buffer, 0, read);
                                    total += read;

                                    int percents = (int)(total * 100 / fws);

                                    strSpeed = "다운로드 진행률 : " + string.Format("{0:#,##0}kb", total) + " / " + string.Format("{0:#,##0}kb", fws) + "(" + percents + "%)";
                                        
                                    backgroundWorker1.ReportProgress(percents);
                                }                                    
                                
                                res.Close();
                                stream.Flush();
                                stream.Close();
                                stream.Dispose();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                                res.Close();
                                throw ex;
                            }
                        }

                        listView1.Invoke(new MethodInvoker(delegate
                        {
                            listView1.Items[fileCnt].Remove();

                            String[] _data = { file, "완료" };
                            ListViewItem item = new ListViewItem(_data);
                            listView1.Items.Insert(fileCnt,item);

                        }));

                        Console.WriteLine("Upload File Complete, status {0}", res.StatusDescription);
                    }
                }
                ++fileCnt;
            }
        }

    
        void worker_ProgressChanged_Ftp(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            label21.Text = strSpeed;
            label22.Text = strLeftTime;
        }

        void worker_RunWorkerCompleted_Ftp(object sender, RunWorkerCompletedEventArgs e)
        {
            backgroundWorker1.DoWork -= new DoWorkEventHandler(downloadFileFtp);
            backgroundWorker1.ProgressChanged -= new ProgressChangedEventHandler(worker_ProgressChanged_Ftp);
            backgroundWorker1.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted_Ftp);

            if (e.Error == null)
            {
                cheFtp.ForeColor = Color.Blue;
                //panel1.Visible = false;

                button2.Text = "다운로드";
                //MessageBox.Show("파일이 모두 다운로드 되었습니다.");

                if(MessageBox.Show("다운받은 폴더를 여시겠습니까?","", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                    Process.Start(textBox1.Text);
                }
            }
            else
            {
                panel1.Visible = false;
                cheFtp.ForeColor = Color.Red;

                MessageBox.Show(e.Error.Message + "  관리자(02-6050-3675)로 문의바랍니다.");
            }

            button2.Enabled = true;
        }

        public static MemoryStream ToByteArray(Stream stream)
        {
            MemoryStream ms = new MemoryStream();
            byte[] chunk = new byte[4096];
            int bytesRead;
            while ((bytesRead = stream.Read(chunk, 0, chunk.Length)) > 0)
            {
                ms.Write(chunk, 0, bytesRead);
            }

            return ms;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            String[] _files = label1.Text.Split('^');
            button2.Enabled = false;

            button2.Text = "실행중";

            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.DoWork += new DoWorkEventHandler(downloadFileFtp);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged_Ftp);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted_Ftp);

            cheFtp.ForeColor = Color.Green;
            backgroundWorker1.RunWorkerAsync();

        }

        private Boolean extendtionYN(String _file)
        {
            Boolean _hangul = false;
            char[] charArr = Path.GetExtension(_file).ToCharArray();
            foreach (char c in charArr)
            {
                if (char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.OtherLetter)
                {
                    _hangul = true;
                    break;
                }
            }
            return _hangul;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            listView1.Clear();
        }

        private void textBox1_MouseClick(object sender, MouseEventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();

            String path = folderBrowserDialog1.SelectedPath;

            textBox1.Text = path;
        }

        private void FTPClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            backgroundWorker1.DoWork -= new DoWorkEventHandler(downloadFileFtp);
            backgroundWorker1.ProgressChanged -= new ProgressChangedEventHandler(worker_ProgressChanged_Ftp);
            backgroundWorker1.RunWorkerCompleted -= new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted_Ftp);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            WebLogView _wlv = new WebLogView();
            _wlv.ShowDialog();
        }
    }
}
