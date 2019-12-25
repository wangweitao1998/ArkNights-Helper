using System;
using System.IO;
using System.Windows.Forms;

namespace ArkNights {
    public partial class Download : Form {
        private string downloadurl;
        private string savepath;
        private Stream st = null;
        private Stream so = null;
        private System.Net.HttpWebRequest Myrq = null;
        private System.Net.HttpWebResponse myrp = null;

        public Download(string _downloadurl, string _savepath) {
            InitializeComponent();
            downloadurl = _downloadurl;
            savepath = _savepath;
        }

        public void Start() {
            if (DownLoadFile(downloadurl, savepath, UpdateProgressBar)) {
                MessageBox.Show("下载完成", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                System.Diagnostics.Process.Start(savepath);
                Dispose();
            }
            else {
                MessageBox.Show("Arknights Helper更新失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Dispose();
            }
        }

        private void UpdateProgressBar(int Maximum, int Value) {
            progressBar1.Maximum = Maximum;
            progressBar1.Value = Value;
        }

        /// <summary>  
        /// 下载带进度条  
        /// </summary>  
        /// <param name="URL">网址</param>  
        /// <param name="Filename">下载后文件名为</param>  
        /// <param name="Prog">报告进度的处理(第一个参数：总大小，第二个参数：当前进度)</param>  
        /// <returns>True/False是否下载成功</returns>  
        private bool DownLoadFile(string URL, string Filename, Action<int, int> updateProgress = null) {
            bool flag = false;
            try {
                Myrq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(URL); //从URL地址得到一个WEB请求     
                myrp = (System.Net.HttpWebResponse)Myrq.GetResponse(); //从WEB请求得到WEB响应     
                long totalBytes = myrp.ContentLength; //从WEB响应得到总字节数
                                                      //更新进度
                if (updateProgress != null) {
                    updateProgress((int)totalBytes, 0);//从总字节数得到进度条的最大值  
                }
                st = myrp.GetResponseStream(); //从WEB请求创建流（读）     
                so = new System.IO.FileStream(Filename, System.IO.FileMode.Create); //创建文件流（写）     
                long totalDownloadedByte = 0; //下载文件大小     
                byte[] by = new byte[1024];
                int osize = st.Read(by, 0, (int)by.Length); //读流     
                while (osize > 0) {
                    totalDownloadedByte = osize + totalDownloadedByte; //更新文件大小     
                    Application.DoEvents();
                    so.Write(by, 0, osize); //写流     
                    updateProgress?.Invoke((int)totalBytes, (int)totalDownloadedByte);//更新进度条 
                    osize = st.Read(by, 0, (int)by.Length); //读流     
                }
                //更新进度
                updateProgress?.Invoke((int)totalBytes, (int)totalBytes);
                flag = true;
            }
            catch (Exception e) {
                Logger logger = new Logger("./data/Log.log");
                logger.log(e.Message + "\n" + e.StackTrace);
                flag = false;
                throw;
            }
            finally {
                Closeconnection();
            }
            return flag;
        }

        private void button1_Click(object sender, EventArgs e) {
            try {
                Closeconnection();
                if (File.Exists(savepath)) {
                    File.Delete(savepath);
                }
            }
            catch (Exception err) {
                Logger logger = new Logger("./data/Log.log");
                logger.log(err.Message + "\n" + err.StackTrace);
            }
            finally {
                Dispose();
            }
        }

        private void Download_FormClosing(object sender, FormClosingEventArgs e) {
            try {
                Closeconnection();
                if (File.Exists(savepath)) {
                    File.Delete(savepath);
                }
            }
            catch (Exception err) {
                Logger logger = new Logger("./data/Log.log");
                logger.log(err.Message + "\n" + err.StackTrace);
            }
            finally {
                Dispose();
            }
        }

        public void Closeconnection() {
            if (Myrq != null) {
                Myrq.Abort();//销毁关闭连接
            }
            if (myrp != null) {
                myrp.Close();//销毁关闭响应
            }
            if (so != null) {
                so.Close(); //关闭流 
            }
            if (st != null) {
                st.Close(); //关闭流  
            }
        }
    }
}
