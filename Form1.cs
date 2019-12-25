using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using OpenCvSharp;
using System.Threading;
using System.IO;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace ArkNights {
    public partial class Form1 : Form {

        private static IntPtr hWnd = IntPtr.Zero;
        private int width;
        private int height;
        private Thread thread1 = null;
        private static bool active = false;

        #region Const
        const int WM_LBUTTONDOWN = 0x201;       //按下鼠标左键  
        const int WM_LBUTTONUP = 0x202;         //释放鼠标左键  
        const int WM_LBUTTONDBLCLK = 0x203;     //双击鼠标左键
        const int MK_LBUTTON = 0x0001;
        const int HORZRES = 8;
        const int VERTRES = 10;
        const int LOGPIXELSX = 88;
        const int LOGPIXELSY = 90;
        const int DESKTOPVERTRES = 117;
        const int DESKTOPHORZRES = 118;
        #endregion

        #region Win32 API
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr ptr);[DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, // handle to DC                
                                        int nIndex // index of capability                
                                        );
        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("user32.dll", EntryPoint = "PostMessage")]
        private static extern int PostMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT {
            public int Left;                                //最左坐标
            public int Top;                                 //最上坐标
            public int Right;                               //最右坐标
            public int Bottom;                              //最下坐标
        }

        [DllImport("user32.dll")]
        static extern bool SystemParametersInfo(uint uiAction, bool uiParam, ref bool pvParam, uint fWinIni);
        const uint SPI_GETSCREENSAVEACTIVE = 0x0010;
        const uint SPI_SETSCREENSAVEACTIVE = 0x0011;
        const uint SPIF_SENDCHANGE = 0x0002;
        const uint SPIF_SENDWININICHANGE = SPIF_SENDCHANGE;

        [DllImport("user32.dll")]//获取窗口句柄
        public static extern IntPtr FindWindow(
         string lpClassName,
         string lpWindowName
         );
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(
               IntPtr hwnd
               );
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(
               IntPtr hdc, // handle to DC
               int nWidth, // width of bitmap, in pixels
               int nHeight // height of bitmap, in pixels
               );
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(
                IntPtr hdc // handle to DC
                );
        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(
               IntPtr hdc, // handle to DC
               IntPtr hgdiobj // handle to object
               );
        [DllImport("user32.dll")]
        public static extern bool PrintWindow(
               IntPtr hwnd, // Window to copy,Handle to the window that will be copied. 
               IntPtr hdcBlt, // HDC to print into,Handle to the device context. 
               UInt32 nFlags // Optional flags,Specifies the drawing options. It can be one of the following values. 
               );
        [DllImport("gdi32.dll")]
        public static extern int DeleteDC(
               IntPtr hdc // handle to DC
               );
        [DllImport("gdi32.dll")]
        public static extern int DeleteObject(
               IntPtr hdc
               );
        #endregion

        public Form1() {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            SystemParametersInfo(SPI_GETSCREENSAVEACTIVE, false, ref active, SPIF_SENDWININICHANGE);

            string strReadFilePath = @"./data/settings.conf";
            StreamReader srReadFile = new StreamReader(strReadFilePath);

            simname.Text = srReadFile.ReadLine();
            counts.Text = srReadFile.ReadLine();
            autoMed.Checked = Convert.ToBoolean(srReadFile.ReadLine());

            // 关闭读取流文件
            srReadFile.Close();
        }

        private void button1_Click(object sender, EventArgs e) {
            hWnd = FindWindow(null, simname.Text);
            RECT rc = new RECT();
            GetWindowRect(hWnd, ref rc);
            width = rc.Right - rc.Left;                         //窗口的宽度
            height = rc.Bottom - rc.Top;                        //窗口的高度

            IntPtr hdc = GetDC(IntPtr.Zero);
            int t = GetDeviceCaps(hdc, DESKTOPHORZRES);
            int d = GetDeviceCaps(hdc, HORZRES);
            float ScaleX = (float)GetDeviceCaps(hdc, DESKTOPHORZRES) / (float)GetDeviceCaps(hdc, HORZRES);
            ReleaseDC(IntPtr.Zero, hdc);

            pictureBox1.Image = GetImg(hWnd, Convert.ToInt32(width * ScaleX), Convert.ToInt32(height * ScaleX));
            if (myaction("公开招募", -1)) {
                /*var ocr = new Tesseract.TesseractEngine("./data/tessdata", "chi_sim", Tesseract.EngineMode.TesseractAndLstm);
                Bitmap img = GetImg(hWnd, Convert.ToInt32(width * ScaleX), Convert.ToInt32(height * ScaleX));
                Rectangle rClipRect = new Rectangle(img.Width / 4, img.Height / 2, img.Width / 2, img.Height / 5);
                Bitmap newimg = img.Clone(rClipRect, img.PixelFormat);
                Grayscale filter = new Grayscale(0.2125, 0.7154, 0.0721);
                var page = ocr.Process(filter.Apply(newimg));
                pictureBox1.Image = newimg;
                MessageBox.Show(page.GetText(),"OCR结果", MessageBoxButtons.OK, MessageBoxIcon.Information);*/

                string pcresult = "";
                string strReadFilePath = @"./data/tag.data";
                StreamReader srReadFile = new StreamReader(strReadFilePath);
                while (!srReadFile.EndOfStream) {
                    string s = srReadFile.ReadLine();
                    if (!s.Contains("#")) {
                        if (myaction("tags/" + s, -1, 0.9)) {
                            pcresult += (s + ", ");
                        }
                    }
                }
                // 读取文件的源路径及其读取流
                strReadFilePath = @"./data/operator.data";
                srReadFile = new StreamReader(strReadFilePath);
                // 读取流直至文件末尾结束
                List<string> tagList = new List<string>();
                while (!srReadFile.EndOfStream) {
                    string strReadLine = srReadFile.ReadLine(); //读取每行数据
                    string[] opinfo = strReadLine.Split(':');
                    string[] tag = opinfo[0].Split('+');
                    bool ismatched = true;
                    foreach (string s in tag) {
                        if (!pcresult.Contains(s)) {
                            ismatched = false;
                            break;
                        }
                    }
                    if (ismatched) {
                        tagList.Add(strReadLine);
                    }
                }
                // 关闭读取流文件
                srReadFile.Close();
                if(pcresult.Length > 2) {
                    string message = "识别到的Tag为：" + pcresult.Substring(0, pcresult.Length - 2) + "\r\n\r\n";
                    foreach (string s in tagList) {
                        string[] opinfo = s.Split(':');
                        message += (opinfo[2] + "☆ - Tag：" + opinfo[0] + "\r\n        干员：" + opinfo[1] + "\r\n");
                    }
                    if (tagList.Count == 0) {
                        MessageBox.Show(message + "无必出4星以上Tag组合！", "公开招募识别结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else {
                        DialogResult dr = MessageBox.Show(message + "\r\n是否自动选择第一组Tag？", "公开招募识别结果", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if(dr == DialogResult.Yes) {
                            string[] opinfo = tagList[0].Split(':');
                            string[] tag = opinfo[0].Split('+');
                            foreach (string s in tag) {
                                if (!myaction("tags/" + s, 1, 0.9)) {
                                    MessageBox.Show("自动选择出错！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    break;
                                }
                            }
                        }
                    }
                }
                else {
                    MessageBox.Show("未识别到Tag！\r\n调整模拟器窗口大小可能有助于识别。", "公开招募识别结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            Console.WriteLine(width+", "+height);
            //myaction("等级提升", -1);
        }

        private void button2_Click(object sender, EventArgs e) {
            hWnd = FindWindow(null, simname.Text);
            RECT rc = new RECT();
            GetWindowRect(hWnd, ref rc);
            width = rc.Right - rc.Left;                         //窗口的宽度
            height = rc.Bottom - rc.Top;                        //窗口的高度
            if (button2.Text == "开始") {
                if (!myaction("开始行动", -1)) {
                    MessageBox.Show("不在关卡开始界面，将停止运行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!myaction("代理指挥-开", -1)) {
                    DialogResult dr = MessageBox.Show("未开启代理指挥！\r\n是-开启并继续 否-退出", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (dr == DialogResult.Yes) {
                        myaction("代理指挥-关", 1);
                    }
                    else {
                        return;
                    }
                }
                
                //创建无参的线程
                thread1 = new Thread(new ThreadStart(arknights));
                //调用Start方法执行线程
                thread1.Start();
                button2.Text = "停止";
            }
            else if(button2.Text == "停止") {
                thread1.Abort();
                MessageBox.Show("已停止执行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                button2.Text = "开始";
                label3.Text = "    准备\r\n    完成";
                SystemSleepManagement.RestoreSleep();
            }

        }

        private void arknights() {
            SystemSleepManagement.PreventSleep(true);
            hWnd = FindWindow(null, simname.Text);
            RECT rc = new RECT();
            GetWindowRect(hWnd, ref rc);
            width = rc.Right - rc.Left;                         //窗口的宽度
            height = rc.Bottom - rc.Top;                        //窗口的高度

            int upgradecount = 0;
            int automedcount = 0;

            bool isJiaoMie = false;
            int total = Convert.ToInt32(counts.Text);
            if (myaction("剿灭作战", -1)) {
                isJiaoMie = true;
                label3.Text = "剿灭作战\r\n进行中 1/" + total;
            }
            else {
                label3.Text = "普通作战\r\n进行中 1/" + total;
            }

            int count = Convert.ToInt32(counts.Text);
            while (count > 0) {
                if (myaction("开始行动", 1)) {
                    Random rd = new Random();
                    Thread.Sleep(rd.Next(200, 500));
                    DateTime beginTime = DateTime.Now;              //获取开始时间
                    DateTime endTime;                               //获取结束时间
                    TimeSpan oTime;                                 //求时间差
                    while (!myaction("开始行动2", -1)) {
                        if (myaction("理智不足", -1)) {
                            if (!myaction("源石恢复", -1) && autoMed.Checked) {
                                if (!myaction("恢复确认", 1)) {
                                    MessageBox.Show("未检测到指定区域，将停止运行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    button2.Text = "开始";
                                    label3.Text = "    准备\r\n    完成";
                                    return;
                                }
                                automedcount++;
                                while (!myaction("开始行动", -1)) {
                                    endTime = DateTime.Now;              //获取结束时间
                                    oTime = endTime.Subtract(beginTime); //求时间差
                                    if (oTime.TotalSeconds > 30) {
                                        MessageBox.Show("检测指定区域超时，将停止运行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        button2.Text = "开始";
                                        label3.Text = "    准备\r\n    完成";
                                        return;
                                    }
                                }
                                if (!myaction("开始行动", 1)) {
                                    MessageBox.Show("未检测到指定区域，将停止运行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    button2.Text = "开始";
                                    label3.Text = "    准备\r\n    完成";
                                    return;
                                }
                            }
                            else {
                                MessageBox.Show("理智不足，将停止执行\r\n未开启自动使用理智药剂或理智药剂不足", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                button2.Text = "开始";
                                label3.Text = "    准备\r\n    完成";
                                return;
                            }
                        }
                        endTime = DateTime.Now;              //获取结束时间
                        oTime = endTime.Subtract(beginTime); //求时间差
                        if (oTime.TotalSeconds > 20) {
                            MessageBox.Show("检测指定区域超时，将停止运行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            button2.Text = "开始";
                            label3.Text = "    准备\r\n    完成";
                            return;
                        }
                    }
                    Thread.Sleep(rd.Next(500, 1000));
                    if (!myaction("开始行动2", 1)) {
                        MessageBox.Show("未检测到指定区域，将停止运行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        button2.Text = "开始";
                        label3.Text = "    准备\r\n    完成";
                        return;
                    }
                    if (isJiaoMie) {
                        beginTime = DateTime.Now;            //获取开始时间
                        while (!myaction("剿灭作战-结束", -1)) {
                            if (myaction("等级提升", -1)) {
                                while (myaction("等级提升", 1)) {
                                    Thread.Sleep(100);
                                }
                                upgradecount++;
                            }
                            endTime = DateTime.Now;              //获取结束时间
                            oTime = endTime.Subtract(beginTime); //求时间差
                            if (oTime.TotalSeconds > 1800) {
                                MessageBox.Show("检测指定区域超时，将停止运行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                button2.Text = "开始";
                                label3.Text = "    准备\r\n    完成";
                                return;
                            }
                            Thread.Sleep(500);
                        }
                        Thread.Sleep(rd.Next(500, 1000));
                        if (!myaction("剿灭作战-结束", 1)) {
                            MessageBox.Show("未检测到指定区域，将停止运行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            button2.Text = "开始";
                            label3.Text = "    准备\r\n    完成";
                            return;
                        }
                    }
                    else {
                        beginTime = DateTime.Now;            //获取开始时间
                        while (!myaction("行动结束", -1)) {
                            if (myaction("等级提升", -1)) {
                                while (myaction("等级提升", 1)) {
                                    Thread.Sleep(100);
                                }
                                upgradecount++;
                            }
                            endTime = DateTime.Now;              //获取结束时间
                            oTime = endTime.Subtract(beginTime); //求时间差
                            if (oTime.TotalSeconds > 600) {
                                MessageBox.Show("检测指定区域超时，将停止运行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                button2.Text = "开始";
                                label3.Text = "    准备\r\n    完成";
                                return;
                            }
                            Thread.Sleep(500);
                        }
                    }
                    Thread.Sleep(rd.Next(1000, 2000));
                    if (!myaction("行动结束", -1)) {
                        Thread.Sleep(5000);
                        if (myaction("等级提升", -1)) {
                            while (myaction("等级提升", 1)) {
                                Thread.Sleep(100);
                            }
                            upgradecount++;
                        }
                    }
                    Thread.Sleep(rd.Next(1000, 2000));
                    if (myaction("行动结束", 1)) {
                        beginTime = DateTime.Now;            //获取开始时间
                        while (!myaction("开始行动", -1)) {
                            myaction("行动结束", 1);
                            endTime = DateTime.Now;              //获取结束时间
                            oTime = endTime.Subtract(beginTime); //求时间差
                            if (oTime.TotalSeconds > 20) {
                                MessageBox.Show("检测指定区域超时，将停止运行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                button2.Text = "开始";
                                label3.Text = "    准备\r\n    完成";
                                return;
                            }
                        }
                        Thread.Sleep(rd.Next(2000, 3000));
                    }
                    else {
                        MessageBox.Show("未检测到指定区域，将停止运行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        button2.Text = "开始";
                        label3.Text = "    准备\r\n    完成";
                        return;
                    }
                }
                else {
                    MessageBox.Show("未检测到指定区域，将停止运行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    button2.Text = "开始";
                    label3.Text = "    准备\r\n    完成";
                    return;
                }
                count--;
                if (count != 0) {
                    label3.Text = "普通作战\r\n进行中 " + (total - count + 1) + "/" + total;
                }
            }
            string infostring = "已完成 " + Convert.ToInt32(counts.Text) + " 次";
            if(upgradecount != 0 || automedcount != 0) {
                infostring += "\r\n";
            }
            if (automedcount != 0) {
                infostring += "\r\n使用 " + automedcount + " 次理智试剂";
            }
            if (upgradecount != 0) {
                infostring += "\r\n升级 " + upgradecount + " 级";
            }
            button2.Text = "开始";
            label3.Text = "    准备\r\n    完成";
            SystemSleepManagement.RestoreSleep();
            MessageBox.Show(infostring, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool myaction(string name, int type, double standard = 0.8) {
            IntPtr hdc = GetDC(IntPtr.Zero);
            int t = GetDeviceCaps(hdc, DESKTOPHORZRES);
            int d = GetDeviceCaps(hdc, HORZRES);
            float ScaleX = (float)GetDeviceCaps(hdc, DESKTOPHORZRES) / (float)GetDeviceCaps(hdc, HORZRES);
            ReleaseDC(IntPtr.Zero, hdc);
            Bitmap oldmap = GetImg(hWnd, Convert.ToInt32(width * ScaleX), Convert.ToInt32(height * ScaleX));
            Bitmap map = ScaleToSize(oldmap, 880, 527);
            double xscale = width / 880.0;
            double yscale = height / 527.0;
            Mat screen = OpenCvSharp.Extensions.BitmapConverter.ToMat(map);
            Mat start = new Mat("./data/img/" + name + ".jpg");
            Mat res = new Mat(screen.Rows - start.Rows + 1, screen.Cols - start.Cols + 1, MatType.CV_32FC1);
            Mat gref = screen.CvtColor(ColorConversionCodes.BGR2GRAY);
            Mat gtpl = start.CvtColor(ColorConversionCodes.BGR2GRAY);

            Cv2.MatchTemplate(gref, gtpl, res, TemplateMatchModes.CCoeffNormed);
            double minval, maxval = 0.8;
            OpenCvSharp.Point minloc, maxloc;
            Cv2.MinMaxLoc(res, out minval, out maxval, out minloc, out maxloc);
            //Setup the rectangle to draw
            Rect r = new Rect(new OpenCvSharp.Point(maxloc.X, maxloc.Y), new OpenCvSharp.Size(start.Width, start.Height));
            //Draw a rectangle of the matching area
            Cv2.Rectangle(screen, r, Scalar.LimeGreen, 2);

            if (maxval > standard) {
                Console.WriteLine(maxval);
                Console.WriteLine(name + " detected");
                if (type != -1) {
                    Console.WriteLine(name + " click");
                    myclick(Convert.ToInt32(maxloc.X * xscale + 5), Convert.ToInt32(maxloc.Y * yscale + 5));
                    pictureBox1.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(screen);
                }
                map.Dispose();
                oldmap.Dispose();
                screen.Dispose();
                start.Dispose();
                res.Dispose();
                gref.Dispose();
                gtpl.Dispose();
                return true;
            }
            map.Dispose();
            oldmap.Dispose();
            screen.Dispose();
            start.Dispose();
            res.Dispose();
            gref.Dispose();
            gtpl.Dispose();
            return false;
        }

        private void myclick(int x, int y) {
            //SetForegroundWindow(hWnd);
            Random rd = new Random();
            int mX = x + rd.Next(1, 20);
            int mY = y + rd.Next(1, 20);
            PostMessage(hWnd, WM_LBUTTONDOWN, MK_LBUTTON, mX + mY * 65536);
            PostMessage(hWnd, WM_LBUTTONUP, 0, mX + mY * 65536);

        }

        private Bitmap ScaleToSize(Bitmap bitmap, int width, int height) {
            if (bitmap.Width == width && bitmap.Height == height) {
                return bitmap;
            }

            var scaledBitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(scaledBitmap)) {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bitmap, 0, 0, width, height);
            }

            return scaledBitmap;
        }

        public static Bitmap GetImg(IntPtr hWnd, int Width, int Height)//得到窗口截图
        {
            IntPtr hscrdc = GetWindowDC(hWnd);
            IntPtr hbitmap = CreateCompatibleBitmap(hscrdc, Width, Height);
            IntPtr hmemdc = CreateCompatibleDC(hscrdc);
            SelectObject(hmemdc, hbitmap);
            PrintWindow(hWnd, hmemdc, 0);
            Bitmap bmp = Bitmap.FromHbitmap(hbitmap);
            DeleteDC(hscrdc);//删除用过的对象
            DeleteObject(hbitmap);//删除用过的对象
            DeleteDC(hmemdc);//删除用过的对象
            return bmp;
        }

        private void button3_Click(object sender, EventArgs e) {
            string strWriteFilePath = @"./data/settings.conf";
            StreamWriter swWriteFile = File.CreateText(strWriteFilePath);

            swWriteFile.WriteLine(simname.Text);
            swWriteFile.WriteLine(counts.Text);
            swWriteFile.WriteLine(autoMed.Checked);

            swWriteFile.Close();
        }

        private void button4_Click(object sender, EventArgs e) {
            System.Diagnostics.Process.Start("notepad.exe", "./UpdateLog.log");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            SystemSleepManagement.RestoreSleep();
            if (thread1 != null) {
                thread1.Abort();
            }
        }

        class SystemSleepManagement {
            //定义API函数
            [DllImport("kernel32.dll")]
            static extern uint SetThreadExecutionState(ExecutionFlag flags);

            [Flags]
            enum ExecutionFlag : uint {
                System = 0x00000001,
                Display = 0x00000002,
                Continuous = 0x80000000,
            }

            /// <summary>
            ///阻止系统休眠，直到线程结束恢复休眠策略
            /// </summary>
            /// <param name="includeDisplay">是否阻止关闭显示器</param>
            public static void PreventSleep(bool includeDisplay = false) {
                SystemParametersInfo(SPI_GETSCREENSAVEACTIVE, false, ref active, SPIF_SENDWININICHANGE);
                if (includeDisplay)
                    SetThreadExecutionState(ExecutionFlag.System | ExecutionFlag.Display | ExecutionFlag.Continuous);
                else
                    SetThreadExecutionState(ExecutionFlag.System | ExecutionFlag.Continuous);
                Console.WriteLine("Sleep OFF");
            }

            /// <summary>
            ///恢复系统休眠策略
            /// </summary>
            public static void RestoreSleep() {
                SystemParametersInfo(SPI_SETSCREENSAVEACTIVE, active, ref active, SPIF_SENDWININICHANGE);
                SetThreadExecutionState(ExecutionFlag.Continuous);
                Console.WriteLine("Sleep ON");
            }

            /// <summary>
            ///重置系统休眠计时器
            /// </summary>
            /// <param name="includeDisplay">是否阻止关闭显示器</param>
            public static void ResetSleepTimer(bool includeDisplay = false) {
                if (includeDisplay)
                    SetThreadExecutionState(ExecutionFlag.System | ExecutionFlag.Display);
                else
                    SetThreadExecutionState(ExecutionFlag.System);
            }
        }

        class UpdateHelper {
            WebClient webClient = null;
            
            public UpdateHelper() {
                webClient = new WebClient();
                webClient.Encoding = Encoding.UTF8;
            }
            
            /// <summary>
            /// 查看是否有新版本
            /// </summary>
            /// <returns>
            /// 0 无
            /// 1 软件有新版本
            /// 2 数据有新版本
            /// 3 数据有静默更新版本
            /// </returns>
            public int isNewVersionExists() {
                try {
                    string outText = webClient.DownloadString("http://47.93.56.66/arknights/version.info");
                    string text1 = outText.Split('\n')[0];
                    text1 = text1.Split(' ')[1];
                    string strReadFilePath = @"./data/version";
                    StreamReader srReadFile = new StreamReader(strReadFilePath);
                    string text2 = srReadFile.ReadLine();
                    srReadFile.Close();
                    Version newv = new Version(text1);
                    Version oldv = new Version(text2);
                    if (newv > oldv) {
                        return 1;
                    }

                    text1 = outText.Split('\n')[1];
                    string ifsilence = text1.Split(' ')[2];
                    text1 = text1.Split(' ')[1];
                    strReadFilePath = @"./data/version";
                    srReadFile = new StreamReader(strReadFilePath);
                    srReadFile.ReadLine();
                    text2 = srReadFile.ReadLine();
                    srReadFile.Close();
                    newv = new Version(text1);
                    oldv = new Version(text2);
                    if (newv > oldv) {
                        if (ifsilence.Equals("silence")) {
                            return 3;
                        }
                        return 2;
                    }

                    return 0;
                }
                catch (Exception e) {
                    Logger logger = new Logger("./data/Log.log");
                    logger.log(e.Message + "\n" + e.StackTrace);
                    return 0;
                }
            }

            public void updateFiles(int n) {
                try {
                    string strReadFilePath = @"./data/version";
                    StreamReader srReadFile = new StreamReader(strReadFilePath);
                    srReadFile.ReadLine();
                    string text1 = srReadFile.ReadLine();
                    srReadFile.Close();
                    Version oldv = new Version(text1);

                    string outText = webClient.DownloadString("http://47.93.56.66/arknights/dataupdate.conf");
                    string[] files = outText.Split('\n');

                    if(n == 3) {
                        for(int j=0; j<files.Length; j++) {
                            string[] sitem = files[j].Split(' ');
                            if (sitem[0].Equals("v") && !sitem[2].Equals("silence")) {
                                j += 1;
                                while (!files[j].Split(' ')[0].Equals("v") && j<files.Length) {
                                    if(files[j].Replace("\n", "").Replace("\r", "").Equals("")) {
                                        j += 1;
                                        continue;
                                    }
                                    string temp = files[j].Split(' ')[1];
                                    files[j] = "~ " + temp;
                                    j += 1;
                                }
                                j -= 1;
                            }
                        }
                    }

                    int i = 0;
                    for (; i<files.Length; i++) {
                        string[] sitem = files[i].Split(' ');
                        if (sitem[0].Equals("v")) {
                            Version newv = new Version(sitem[1]);
                            if(newv > oldv) {
                                i += 1;
                                break;
                            }
                        }
                    }
                    for (; i<files.Length; i++) {
                        Console.WriteLine(files[i]);
                        string[] sitem = files[i].Split(' ');
                        if (sitem[0].Equals("+")) {
                            webClient.DownloadFile("http://47.93.56.66/arknights/files/" + sitem[1], "./data/" + sitem[1]);
                        }
                        else if (sitem[0].Equals("-")) {
                            File.Delete("./data/" + sitem[1]);
                        }
                    }
                }
                catch (Exception e) {
                    Logger logger = new Logger("./data/Log.log");
                    logger.log(e.Message + "\n" + e.StackTrace);
                    MessageBox.Show("数据文件更新失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); 
                    return;
                }
                try {
                    string strFilePath = @"./data/version";
                    StreamReader srReadFile = new StreamReader(strFilePath);
                    string[] version = srReadFile.ReadToEnd().Split('\n');
                    srReadFile.Close();
                    string t = webClient.DownloadString("http://47.93.56.66/arknights/version.info");
                    version[1] = t.Split('\n')[1].Split(' ')[1];

                    StreamWriter swWriteFile = File.CreateText(strFilePath);
                    foreach (string s in version) {
                        if (!s.Equals(""))
                            swWriteFile.WriteLine(s.Replace("\n", "").Replace("\r", ""));
                    }
                    swWriteFile.Close();
                }
                catch (Exception e) {
                    Logger logger = new Logger("./data/Log.log");
                    logger.log(e.Message + "\n" + e.StackTrace);
                    MessageBox.Show("数据文件版本号更新失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            public void updateApp() {
                Download download = null;
                try {
                    string t = webClient.DownloadString("http://47.93.56.66/arknights/version.info");
                    string version = t.Split('\n')[0].Split(' ')[1].Replace("\n", "").Replace("\r", "");
                    download = new Download("http://47.93.56.66/arknights/installer/ArkNights_Helper_Setup_" + version + ".msi", "ArkNights_Helper_Setup_" + version + ".msi");
                    download.Show();
                    download.Start();
                }
                catch (Exception e) {
                    Logger logger = new Logger("./data/Log.log");
                    logger.log(e.Message + "\n" + e.StackTrace);
                    if(download != null) {
                        download.Closeconnection();
                        download.Dispose();
                    }
                    MessageBox.Show("Arknights Helper更新失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            public string getUpdateInfo() {
                try {
                    return webClient.DownloadString("http://47.93.56.66/arknights/installer/update.info");
                }
                catch (Exception e) {
                    Logger logger = new Logger("./data/Log.log");
                    logger.log(e.Message + "\n" + e.StackTrace);
                    return "获取版本更新内容失败！";
                }
            }

            public void Dispose() {
                webClient.Dispose();
            }

        }

        private void Form1_Shown(object sender, EventArgs e) {     
            if (File.Exists(@"./data/custom.data")) {
                try {
                    string strReadFilePath = @"./data/custom.data";
                    StreamReader srReadFile = new StreamReader(strReadFilePath);
                    label5.Text = srReadFile.ReadLine().Replace("\n", "").Replace("\r", "");
                    srReadFile.Close();
                }
                catch (Exception err) {
                    Logger logger = new Logger("./data/Log.log");
                    logger.log(err.Message + "\n" + err.StackTrace);
                    return;
                }
            }

            UpdateHelper update = new UpdateHelper();
            int updatestate = update.isNewVersionExists();

            if (updatestate == 1) {
                string updateinfo = update.getUpdateInfo();
                DialogResult dr = MessageBox.Show("Arknights Helper检测到新版本！\r\n是否下载？\r\n\r\n更新内容：\r\n" + updateinfo, "自动更新", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (dr == DialogResult.Yes) {
                    update.updateApp();
                }
            }
            else if(updatestate == 2) {
                DialogResult dr = MessageBox.Show("数据文件检测到新版本！\r\n是否下载？", "自动更新", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (dr == DialogResult.Yes) {
                    update.updateFiles(2);
                }
            }
            else if (updatestate == 3) {
                update.updateFiles(3);
            }

            if (File.Exists(@"./data/custom.data")) {
                try {
                    string strReadFilePath = @"./data/custom.data";
                    StreamReader srReadFile = new StreamReader(strReadFilePath);
                    label5.Text = srReadFile.ReadLine().Replace("\n", "").Replace("\r", "");
                    srReadFile.Close();
                }
                catch (Exception err) {
                    Logger logger = new Logger("./data/Log.log");
                    logger.log(err.Message + "\n" + err.StackTrace);
                    return;
                }
            }
        }
    }
}
