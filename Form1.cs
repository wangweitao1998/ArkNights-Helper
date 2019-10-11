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

namespace ArkNights {
    public partial class Form1 : Form {

        private static IntPtr hWnd = IntPtr.Zero;
        private int width;
        private int height;
        private Thread thread1 = null;

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
                string pcresult = "";
                string strReadFilePath = @"./data/tag.data";
                StreamReader srReadFile = new StreamReader(strReadFilePath);
                while (!srReadFile.EndOfStream) {
                    string s = srReadFile.ReadLine();
                    if (!s.Contains("#")) {
                        if (myaction("公招/" + s, -1, 0.9)) {
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
                                if (!myaction("公招/" + s, 1, 0.9)) {
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
            }

        }

        private void arknights() {
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
                label3.Text = "普通作战\r\n进行中 " + (total - count + 1) + "/" + total;
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
            MessageBox.Show(infostring, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            button2.Text = "开始";
            label3.Text = "    准备\r\n    完成";
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
            Mat start = new Mat("./data/" + name + ".jpg");
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
                System.Console.WriteLine(maxval);
                System.Console.WriteLine(name + " detected");
                if (type != -1) {
                    System.Console.WriteLine(name + " click");
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
            if(thread1 != null) {
                thread1.Abort();
            }
        }

        private string PadRightEx(string str, int totalByteCount, char c) {
            Encoding coding = Encoding.GetEncoding("gb2312");
            int dcount = 0;
            foreach (char ch in str.ToCharArray()) {
                if (coding.GetByteCount(ch.ToString()) == 2)
                    dcount++;
            }
            string w = str.PadRight(totalByteCount - dcount, c);
            return w;
        }
    }
}
