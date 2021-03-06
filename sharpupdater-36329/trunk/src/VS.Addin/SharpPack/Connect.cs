using System;
using System.Collections.Generic;
using System.Linq;
using CnSharp.Delivery.VisualStudio.PackingTool.Commands;
using CnSharp.Windows.Updater.Util;
using EnvDTE;
using EnvDTE80;
using Extensibility;
using Microsoft.VisualStudio.CommandBars;

namespace CnSharp.Delivery.VisualStudio.PackingTool
{
    /// <summary>用于实现外接程序的对象。</summary>
    /// <seealso class='IDTExtensibility2' />
    public class Connect : IDTExtensibility2, IDTCommandTarget
    {
        #region Constants and Fields


        private AddIn _addIn;
        private DTE2 _dte;
        private readonly Logger _logger = Common.MyLogger;
        private readonly List<CommandMenuItem> _menus = new List<CommandMenuItem>(); 
        private readonly Dictionary<string,CommandBarControl> _commands = new Dictionary<string, CommandBarControl>();

        #endregion



        public Connect()
        {
           _menus.Add(new StandardBuildMenuItem());
           _menus.Add(new ZipBuildMenuItem());
        }

        #region Public Methods

        #region IDTCommandTarget Members

        /// <summary>实现 IDTCommandTarget 接口的 Exec 方法。此方法在调用该命令时调用。</summary>
        /// <param name="commandName">要执行的命令的名称。</param>
        /// <param name="executeOption">描述该命令应如何运行。</param>
        /// <param name="varIn">从调用方传递到命令处理程序的参数。</param>
        /// <param name="varOut">从命令处理程序传递到调用方的参数。</param>
        /// <param name="handled">通知调用方此命令是否已被处理。</param>
        public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut,
            ref bool handled)
        {
            handled = false;
            if (executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
            {
                string shortName = commandName.Substring(_addIn.ProgID.Length + 1);
                var menu = _menus.FirstOrDefault(m => m.CommandName == shortName);
                if (menu != null)
                {
                    menu.Execute();
                }
                handled = true;
            }
        }


        /// <summary>实现 IDTCommandTarget 接口的 QueryStatus 方法。此方法在更新该命令的可用性时调用</summary>
        /// <param name="commandName">要确定其状态的命令的名称。</param>
        /// <param name="neededText">该命令所需的文本。</param>
        /// <param name="status">该命令在用户界面中的状态。</param>
        /// <param name="commandText">neededText 参数所要求的文本。</param>
        /// <seealso class='Exec' />
        public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status,
            ref object commandText)
        {
            if (neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
            {
                string shortName = commandName.Substring(_addIn.ProgID.Length + 1);
                //commandName == addInInstance.ProgID + "." + CommandName
                if (_commands.ContainsKey(shortName))
                {
                    status = vsCommandStatus.vsCommandStatusEnabled | vsCommandStatus.vsCommandStatusSupported;
                }
                else
                {
                    status = vsCommandStatus.vsCommandStatusUnsupported;
                }
            }
        }

        #endregion

        #region IDTExtensibility2 Members

        /// <summary>实现 IDTExtensibility2 接口的 OnAddInsUpdate 方法。当外接程序集合已发生更改时接收通知。</summary>
        /// <param name="custom">特定于宿主应用程序的参数数组。</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnAddInsUpdate(ref Array custom)
        {
        }

        /// <summary>实现 IDTExtensibility2 接口的 OnBeginShutdown 方法。接收正在卸载宿主应用程序的通知。</summary>
        /// <param name="custom">特定于宿主应用程序的参数数组。</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnBeginShutdown(ref Array custom)
        {
        }

        /// <summary>实现 IDTExtensibility2 接口的 OnConnection 方法。接收正在加载外接程序的通知。</summary>
        /// <param name="application">宿主应用程序的根对象。</param>
        /// <param name="connectMode">描述外接程序的加载方式。</param>
        /// <param name="addInInst">表示此外接程序的对象。</param>
        /// <param name="custom"></param>
        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            _dte = (DTE2) application;
            _addIn = (AddIn) addInInst;
           
            _menus.ForEach(m =>
            {
                m.Dte = _dte;
                m.AddIn = _addIn;
                var bar = m.Connect();
                _commands.Add(m.CommandName,bar);
            });
        }

        /// <summary>实现 IDTExtensibility2 接口的 OnDisconnection 方法。接收正在卸载外接程序的通知。</summary>
        /// <param name="disconnectMode">描述外接程序的卸载方式。</param>
        /// <param name="custom">特定于宿主应用程序的参数数组。</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
            try
            {
                switch (disconnectMode)
                {
                    case ext_DisconnectMode.ext_dm_HostShutdown:
                    case ext_DisconnectMode.ext_dm_UserClosed:
                        foreach (var command in _commands)
                        {
                            command.Value.Delete(true);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteExceptionLog("Disconnection error:" + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        /// <summary>实现 IDTExtensibility2 接口的 OnStartupComplete 方法。接收宿主应用程序已完成加载的通知。</summary>
        /// <param name="custom">特定于宿主应用程序的参数数组。</param>
        /// <seealso class='IDTExtensibility2' />
        public void OnStartupComplete(ref Array custom)
        {
        }

        #endregion



        #endregion

    }
}