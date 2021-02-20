using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading;

namespace FTPClient
{
    public partial class WebLogView : Form
    {
        string ftpPath = "";
        string subURL = "";

        string ftpUser = "root";
        string ftpPass = "Great2016!^";

        string LocalDirectory = "D:\\temp";

        CommonService _common = new CommonService();

        public WebLogView()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;

            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            GetFileList();
        }

        private void getFtpPath()
        {
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    ftpPath = @"10.65.21.177";
                    subURL = "/usr/local/resin/log/license1";
                    break;
                case 1:
                    ftpPath = @"10.65.21.178";
                    subURL = "/usr/local/resin/log/license2";
                    break;
                case 2:
                    ftpPath = @"10.65.21.175";
                    subURL = "/DATA/resin_log/mobileWas1/mobile1";
                    break;
                case 3:
                    ftpPath = @"10.65.21.175";
                    subURL = "/DATA/resin_log/mobileWas2/mobile2";
                    break;
            }
        }

        public void GetFileList()
        {
            getFtpPath();

            listView1.Clear();

            var ci = new ConnectionInfo(ftpPath,
                              ftpUser,
                                new PasswordAuthenticationMethod(ftpUser, ftpPass));

            //List<String> _fileList = new List<string>();
            using (var sftp = new SftpClient(ci))
            {
                // SFTP 서버 연결
                sftp.Connect();

                listView1.View = View.Details;

                listView1.Columns.Add("파일명", 700);

                // 현재 디렉토리 내용 표시
                foreach (SftpFile f in sftp.ListDirectory(subURL).OrderBy(o => o.Name))
                {
                    //Console.WriteLine(f.Name);

                    String[] _filename = f.Name.Split('-');

                    if (_filename[0].Equals("stdout"))
                    {
                        String[] _data = { f.Name };
                        ListViewItem item = new ListViewItem(_data);
                        listView1.Items.Add(item);
                    }
                }
            }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            getFtpPath();

            richTextBox1.Text = "";

            pictureBox1.Visible = true;
            backgroundWorker1.RunWorkerAsync();
        }   

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            pictureBox1.Visible = false;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            this.Invoke(new MethodInvoker(delegate ()
            {
                SftpClient oSftp = new SftpClient(ftpPath, ftpUser, ftpPass);
                // SFTP 서버 연결
                oSftp.Connect();

                using (Stream file1 = File.OpenWrite(LocalDirectory + "/" + listView1.SelectedItems[0].Text))
                {
                    oSftp.DownloadFile(subURL + "/" + listView1.SelectedItems[0].Text, file1);
                }

                using (StreamReader sr = new StreamReader(LocalDirectory + "/" + listView1.SelectedItems[0].Text))
                {
                    Task<string> text = sr.ReadToEndAsync();
                    richTextBox1.Text = text.Result;
                }

            }));

            //Thread.Sleep(10000);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            List<int> found = null;
            
            int cursorPos = richTextBox1.SelectionStart;
            clearHighlights(richTextBox1);
            found = FindAll(richTextBox1, textBox1.Text, 0);

            MessageBox.Show(found.Count() + "건이 검색되었습니다.");

            HighlightAll(richTextBox1, Color.Red, found, textBox1.Text.Length);
            richTextBox1.Select(cursorPos, 0);
        }

        public List<int> FindAll(RichTextBox rtb, string txtToSearch, int searchStart)
        {
            List<int> found = new List<int>();
            if (txtToSearch.Length <= 0) return found;

            int pos= rtb.Find( txtToSearch, searchStart, RichTextBoxFinds.None);
            while (pos >= 0)
            {
                found.Add(pos);
                pos = rtb.Find(txtToSearch, pos + txtToSearch.Length, RichTextBoxFinds.None);
            }
            return found;
        }

        public void HighlightAll(RichTextBox rtb, Color color, List<int> found, int length)
        {
            foreach (int p in found)
            {
                rtb.Select(p, length);
                rtb.SelectionColor = color;
                rtb.SelectionBackColor = Color.Yellow;
            }
        }

        void clearHighlights(RichTextBox rtb)
        {
            int cursorPos = rtb.SelectionStart;    // store cursor
            rtb.Select(0, rtb.TextLength);         // select all
            rtb.SelectionColor = rtb.ForeColor;    // default text color
            rtb.Select(cursorPos, 0);              // reset cursor
        }
    }
}
