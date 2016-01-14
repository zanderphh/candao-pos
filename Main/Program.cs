using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;
using Common;
using WebServiceReference;
using System.Diagnostics;
using DevExpress.UserSkins;
using DevExpress.Skins;

/************************************************************************* 
 * 程序说明: 程序入口
 * 注意初始化程序的代码顺序!!! 
 * 作者：
 **************************************************************************/

namespace Main
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            RestClient.getTicketList("102073024|4900|2015-07-14|会员生日49元购鱼券|1||0.000000,0.000000,false,,");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Globals.ProductVersion = Application.ProductVersion;
            frmStart.ShowStart();
            frmStart.frm.Update();
            Thread.Sleep(50);
            frmStart.frm.setMsg("加载样式...");
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);//捕获系统所产生的异常。
            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
            frmStart.frm.setMsg("检测实例...");
            Process instance = RunningInstance();
            if (instance == null)
            {
                //1.1 没有实例在运行

            }
            else
            {
                //1.2 已经有一个实例在运行
                HandleRunningInstance(instance);
                return;
            }
            //Program.CheckInstance();//检查程序是否运行多实例
            frmStart.frm.setMsg("读取设置...");
            SystemConfig.ReadSettings(); //读取用户自定义设置

            /*if (false == BridgeFactory.InitializeBridge())//初始化桥接功能
            {
                Application.Exit();
                return;
            }*/

            BonusSkins.Register();//注册Dev酷皮肤
            //OfficeSkins.Register();////注册Office样式的皮肤
            SkinManager.EnableFormSkins();//启用窗体支持换肤特性
            RestClient.GetSoapRemoteAddress();
            //如果还没有开业，提示开业授权
            string reinfo="";
            frmStart.frm.setMsg("检查是否开业...");
            try
            {
                if (!RestClient.OpenUp("", "", 0, out reinfo))
                {
                    Thread.Sleep(1000);
                    try
                    { frmStart.frm.Close(); }
                    catch { }
                    if (!frmPermission.ShowPermission())
                    {
                        Application.Exit();
                        return;
                    }
                    else
                    {
                        //经理权限开业
                        if (!frmPermission2.ShowPermission2("开业经理授权", eRightType.right2))
                        {
                            Application.Exit();
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Msg.ShowException(ex);
                Application.Exit();
                return;
            }
            //注意：先打开登陆窗体,登陆成功后正式运行程序(MDI主窗体)
            frmStart.frm.setMsg("开始登录...");
            Thread.Sleep(1000);

            if (frmLogin.Login())
            {
                //如果没有收银权限，那不用输入零找金
                if (Globals.userRight.getSyRigth())
                {
                    if (!frmPosMainV3.checkInputTellerCash())
                    {
                        Application.Exit();
                        return;
                    }
                }
                Program.MainForm.Show();
                Application.Run();
            }
            else//登录失败,退出程序
                Application.Exit();
        }

        static void Application_ApplicationExit(object sender, EventArgs e)
        {

        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Msg.ShowException(e.Exception);//处理系统异常
        }

        private static frmAllTable _mainForm = null;

        /// <summary>
        /// MDI主窗体
        /// </summary>        
        public static frmAllTable MainForm { get { return _mainForm; } set { _mainForm = value; } }

        /// <summary>
        ///检查程序是否运行多实例
        /// </summary>
        private static Process RunningInstance()
        {
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);
            //遍历与当前进程名称相同的进程列表 
            foreach (Process process in processes)
            {
                //如果实例已经存在则忽略当前进程 
                if (process.Id != current.Id)
                {
                    //保证要打开的进程同已经存在的进程来自同一文件路径
                    if (Assembly.GetExecutingAssembly().Location.Replace("/", @"\") == current.MainModule.FileName)
                    {
                        //返回已经存在的进程
                        return process;
                    }
                }
            }
            return null;
        }
        //3.已经有了就把它激活，并将其窗口放置最前端
        private static void HandleRunningInstance(Process instance)
        {
            //MessageBox.Show("已经在运行！", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ShowWindowAsync(instance.MainWindowHandle, 1);  //调用api函数，正常显示窗口
            SetForegroundWindow(instance.MainWindowHandle); //将窗口放置最前端
        }
        [DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(System.IntPtr hWnd, int cmdShow);
        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(System.IntPtr hWnd);


    }
}