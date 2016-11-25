﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using CanDao.Pos.Common;
using CanDao.Pos.IService;
using CanDao.Pos.Model;
using CanDao.Pos.Model.Enum;
using CanDao.Pos.UI.MainView.View;
using CanDao.Pos.UI.Utility;
using CanDao.Pos.UI.Utility.View;
using CanDao.Pos.UI.Utility.ViewModel;
using CanDao.Pos.VIPManage.ViewModels;
using Timer = System.Timers.Timer;

namespace CanDao.Pos.UI.MainView.ViewModel
{
    public class MainWndVm : NormalWindowViewModel<MainWindow>
    {
        #region Filed

        /// <summary>
        /// 所有餐桌分区信息。
        /// </summary>
        private List<AreaInfo> _allAreaInfos;

        /// <summary>
        /// 正在过滤操作中。
        /// </summary>
        private bool _isInFilter;

        /// <summary>
        /// 是否已经释放了资源。
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// 刷新时间间隔（秒）
        /// </summary>
        private const int RefreshTimerInterval = 5;

        /// <summary>
        /// 刷新计时器。
        /// </summary>
        private readonly Timer _refreshTimer;

        /// <summary>
        /// 打印机检测间隔（秒）。
        /// </summary>
        private const int PrinterCheckTimerInterval = 10;

        /// <summary>
        /// 打印机检测定时器。
        /// </summary>
        private readonly Timer _printerCheckTimer;

        /// <summary>
        /// 下一次警告间隔时间（分）。
        /// </summary>
        private const int NextMaxWarningInterval = 10;

        /// <summary>
        /// 下一次警告时间。
        /// </summary>
        private DateTime _nextWarningTime = DateTime.MinValue;

        #endregion

        #region Constructor

        /// <summary>
        /// 实例化一个POS主界面窗口。
        /// </summary>
        /// <param name="isForcedEndWorkModel">是否是强制结业模式。</param>
        public MainWndVm(bool isForcedEndWorkModel)
        {
            IsForcedEndWorkModel = isForcedEndWorkModel;
            AreaInfos = new ObservableCollection<AreaInfo>();
            TableInfos = new ObservableCollection<TableInfo>();

            RefreshRemainSecond = RefreshTimerInterval;
            _refreshTimer = new Timer(1000) { Enabled = true };
            _refreshTimer.Elapsed += RefreshTimer_Elapsed;

            _printerCheckTimer = new Timer(PrinterCheckTimerInterval * 1000);
            _printerCheckTimer.Elapsed += PrinterCheckTimerOnElapsed;
        }

        #endregion

        #region Properties

        /// <summary>
        /// 选中的界面餐台类型。
        /// </summary>
        private EnumViewTableType _viewTableType;
        /// <summary>
        /// 选中的界面餐台类型。
        /// </summary>
        public EnumViewTableType ViewTableType
        {
            get { return _viewTableType; }
            set
            {
                _viewTableType = value;
                RaisePropertyChanged("ViewTableType");

                FilterAreaInfo();
            }
        }

        /// <summary>
        /// 分区信息集合。
        /// </summary>
        public ObservableCollection<AreaInfo> AreaInfos { get; set; }

        /// <summary>
        /// 餐台信息集合。
        /// </summary>
        public ObservableCollection<TableInfo> TableInfos { get; set; }

        /// <summary>
        /// 选择的餐桌分区。
        /// </summary>
        private AreaInfo _selectedAreaInfo;
        /// <summary>
        /// 选择的餐桌分区。
        /// </summary>
        public AreaInfo SelectedAreaInfo
        {
            get { return _selectedAreaInfo; }
            set
            {
                _selectedAreaInfo = value;
                RaisePropertyChanged("SelectedAreaInfo");

                if (_selectedAreaInfo != null)
                    FilterTableInfos();
            }
        }

        /// <summary>
        /// 视图上餐桌状态枚举。
        /// </summary>
        private EnumViewTableStatus _viewTableStatus;
        /// <summary>
        /// 视图上餐桌状态枚举。
        /// </summary>
        public EnumViewTableStatus ViewTableStatus
        {
            get { return _viewTableStatus; }
            set
            {
                _viewTableStatus = value;
                RaisePropertyChanged("ViewTableStatus");

                FilterTableInfos();
            }
        }

        /// <summary>
        /// 所有餐台个数。
        /// </summary>
        private int _allTableCount;
        /// <summary>
        /// 所有餐台个数。
        /// </summary>
        public int AllTableCount
        {
            get { return _allTableCount; }
            set
            {
                _allTableCount = value;
                RaisePropertyChanged("AllTableCount");
            }
        }

        /// <summary>
        /// 空闲餐台数。
        /// </summary>
        private int _idleCount;
        /// <summary>
        /// 获取或设置空闲餐台数。
        /// </summary>
        public int IdleCount
        {
            get { return _idleCount; }
            set
            {
                _idleCount = value;
                RaisePropertiesChanged("IdleCount");
            }
        }

        /// <summary>
        /// 就餐餐台数。
        /// </summary>
        private int _dinnerCount;
        /// <summary>
        /// 获取或设置就餐餐台数。
        /// </summary>
        public int DinnerCount
        {
            get { return _dinnerCount; }
            set
            {
                _dinnerCount = value;
                RaisePropertiesChanged("DinnerCount");
            }
        }

        /// <summary>
        /// 刷新剩余时间。
        /// </summary>
        private int _refreshRemainSecond;
        /// <summary>
        /// 刷新剩余时间。
        /// </summary>
        public int RefreshRemainSecond
        {
            get { return _refreshRemainSecond; }
            set
            {
                _refreshRemainSecond = value;
                RaisePropertiesChanged("RefreshRemainSecond");
            }
        }

        /// <summary>
        /// 是否是强制结业模式。
        /// </summary>
        public bool IsForcedEndWorkModel { get; set; }

        /// <summary>
        /// 是否有打印机错误。
        /// </summary>
        private bool _hasPrinterError;
        /// <summary>
        /// 是否有打印机错误。
        /// </summary>
        public bool HasPrinterError
        {
            get { return _hasPrinterError; }
            set
            {
                _hasPrinterError = value;
                RaisePropertyChanged("HasPrinterError");
            }
        }

        /// <summary>
        /// 是否展开会员面板。
        /// </summary>
        private bool _isMemberOpened;

        /// <summary>
        /// 是否展开会员面板。
        /// </summary>
        public bool IsMemberOpened
        {
            get { return _isMemberOpened; }
            set
            {
                _isMemberOpened = value;
                RaisePropertyChanged("IsMemberOpened");
            }
        }

        /// <summary>
        /// 店铺简易信息。
        /// </summary>
        private BusinessSimpleInfo _businessoSimpleInfo;
        /// <summary>
        /// 店铺简易信息。
        /// </summary>
        public BusinessSimpleInfo BusinessSimpleInfo
        {
            get { return _businessoSimpleInfo; }
            set
            {
                _businessoSimpleInfo = value;
                RaisePropertyChanged("BusinessSimpleInfo");
            }
        }

        /// <summary>
        /// 是否包含咖啡台。
        /// </summary>
        private bool _hasCoffeeTables;
        /// <summary>
        /// 是否包含咖啡台。
        /// </summary>
        public bool HasCoffeeTables
        {
            get { return _hasCoffeeTables; }
            set
            {
                _hasCoffeeTables = value;
                RaisePropertyChanged("HasCoffeeTables");
            }
        }

        #endregion

        #region Command

        /// <summary>
        /// 选择餐桌命令。
        /// </summary>
        public ICommand SelectTableCmd { get; private set; }

        #endregion

        #region Command Methods

        /// <summary>
        /// 选择某个餐桌命令执行方法。
        /// </summary>
        /// <param name="param"></param>
        private void SelectTable(object param)
        {
            var tableInfo = param as TableInfo;
            if (tableInfo == null)
                return;

            if (!Globals.UserRight.AllowCash)
            {
                MessageDialog.Warning("您没有收银权限。", OwnerWindow);
                return;
            }

            WindowHelper.ShowDialog(new TableOperWindow(tableInfo), OwnerWindow);
            GetAllTableInfoesAsync();
        }

        #endregion

        #region Proptected Methods

        protected override void InitCommand()
        {
            base.InitCommand();
            SelectTableCmd = CreateDelegateCommand(SelectTable);
        }

        protected override void OnWindowLoaded(object param)
        {
            if (!IsInDesignMode)
            {
                GetAllTableInfoesAsync();
                ThreadPool.QueueUserWorkItem(t => { CheckPrinterStatus(); });

                if (Globals.IsDinnerWareEnable)
                {
                    ThreadPool.QueueUserWorkItem(t =>
                    {
                        InfoLog.Instance.I("启用了餐具收费设置，开始获取餐具信息...");
                        var service = ServiceManager.Instance.GetServiceIntance<IRestaurantService>();
                        if (service == null)
                        {
                            ErrLog.Instance.E("创建IRestaurantService服务失败。");
                            return;
                        }

                        var result2 = service.GetDinnerWareInfo(Globals.UserInfo.UserName);
                        if (!string.IsNullOrEmpty(result2.Item1))
                            ErrLog.Instance.E(result2.Item1);

                        InfoLog.Instance.I("获取餐具信息完成。");
                        Globals.DinnerWareInfo = result2.Item2;
                    });
                }
            }
        }

        protected override void OnWindowClosing(CancelEventArgs arg)
        {
            if (!MessageDialog.Quest("确定要退出系统吗？"))
                arg.Cancel = true;
        }

        protected override void OnWindowClosed(object param)
        {
            InfoLog.Instance.I("窗口关闭了....");
            if (_refreshTimer != null)//释放刷新定时器。
            {
                _refreshTimer.Stop();
                _refreshTimer.Dispose();
                InfoLog.Instance.I("释放了刷新定时器。");
            }

            if (_printerCheckTimer != null) //释放打印机状态定时器。
            {
                _printerCheckTimer.Stop();
                _printerCheckTimer.Dispose();
                InfoLog.Instance.I("释放了打印机状态定时器。");
            }

            _isDisposed = true;
            Application.Current.Shutdown();//尝试解决有时候退出主窗口程序依然在运行的问题。
        }

        protected override void OperMethod(object param)
        {
            switch ((string)param)
            {
                case "Takeout"://外卖
                    TakeoutTable();
                    break;
                case "QueryOrderHistory"://账单历史
                    WindowHelper.ShowDialog(new QueryOrderHistoryWindow(), OwnerWindow);
                    break;
                case "Clearner"://清机
                    ClearnMachine();
                    break;
                case "Report"://报表
                    WindowHelper.ShowDialog(new ReportViewWindow(), OwnerWindow);
                    break;
                case "System"://系统
                    WindowHelper.ShowDialog(new SystemSettingWindow(), OwnerWindow);
                    CheckPrinterStatus();//从系统退出以后马上检测一下打印机状态，不然可能出现状态在一定时间内不同步的现象。#9264
                    break;
                case "Member":
                    IsMemberOpened = !IsMemberOpened;
                    break;
                case "MemberQuery":
                    if (Globals.IsCanDaoMember)
                    {
                        var query = new UcVipSelectViewModel();
                        WindowHelper.ShowDialog(query.GetUserCtl());
                    }
                    else if (Globals.IsYazuoMember)
                    {
                        WindowHelper.ShowDialog(new MemberYaZuoQueryWndVm(), OwnerWindow);
                    }
                    break;
                case "MemberStore":
                    if (Globals.IsCanDaoMember)
                    {
                        var recharge = new UcVipRechargeViewModel();
                        WindowHelper.ShowDialog(recharge.GetUserCtl());
                    }
                    else if (Globals.IsYazuoMember)
                    {
                        WindowHelper.ShowDialog(new MemberYaZuoStoredWndVm(), OwnerWindow);
                    }
                    break;
                case "MemberRegist":
                    var regist = new UcVipRegViewModel();
                    WindowHelper.ShowDialog(regist.GetUserCtl());
                    break;
                case "MemberCardActive":
                    WindowHelper.ShowDialog(new MemberYaZuoCardActiveWndVm(), OwnerWindow);
                    break;
                case "TableStatusAll":
                    ViewTableStatus = EnumViewTableStatus.All;
                    break;
                case "TableStatusDinner":
                    ViewTableStatus = EnumViewTableStatus.Dinner;
                    break;
                case "TableStatusIdel":
                    ViewTableStatus = EnumViewTableStatus.Idel;
                    break;
                case "TableTypeNormal":
                    ViewTableType = EnumViewTableType.Normal;
                    break;
                case "TableTypeCoffee":
                    ViewTableType = EnumViewTableType.Coffee;
                    break;
            }
        }

        protected override void GroupMethod(object param)
        {
            switch ((string)param)
            {
                case "TablesPreGroup":
                    OwnerWnd.GsTables.PreviousGroup();
                    break;
                case "TablesNextGroup":
                    OwnerWnd.GsTables.NextGroup();
                    break;
                case "PreAreaGroup":
                    OwnerWnd.GsArea.PreviousGroup();
                    break;
                case "NextAreaGroup":
                    OwnerWnd.GsArea.NextGroup();
                    break;
            }
        }

        protected override bool CanGroupMethod(object param)
        {
            switch ((string)param)
            {
                case "TablesPreGroup":
                    return OwnerWnd.GsTables.CanPreviousGroup;
                case "TablesNextGroup":
                    return OwnerWnd.GsTables.CanNextGruop;
                case "PreAreaGroup":
                    return OwnerWnd.GsArea.CanPreviousGroup;
                case "NextAreaGroup":
                    return OwnerWnd.GsArea.CanNextGruop;
                default:
                    return true;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 点击外卖按钮时处理外卖。
        /// </summary>
        private void TakeoutTable()
        {
            InfoLog.Instance.I("开始外卖...");
            if (!Globals.UserRight.AllowCash)
            {
                InfoLog.Instance.I("当前用户没有收银权限：{0}", Globals.UserInfo.UserName);
                MessageDialog.Warning("您没有收银权限！");
                return;
            }

            var param = new List<EnumTableType> { EnumTableType.CFTakeout };
            TaskService.Start(param, GetTableInfoByTableTypeProcess, GetCfTakeoutTableInfoComplete, null);
        }

        /// <summary>
        /// 检测打印机状态。
        /// </summary>
        private void CheckPrinterStatus()
        {
            SetPrinterCheckTimerStatus(false);
            InfoLog.Instance.I("开始检测打印机状态...");
            var service = ServiceManager.Instance.GetServiceIntance<IRestaurantService>();
            if (service == null)
            {
                ErrLog.Instance.E("创建IRestaurantService服务失败。");
                OwnerWindow.Dispatcher.Invoke((Action)delegate { MessageDialog.Warning("创建IRestaurantService服务失败。"); });
                SetPrinterCheckTimerStatus(true);
                return;
            }

            var result = service.GetPrinterStatusInfo();
            if (!string.IsNullOrEmpty(result.Item1))
            {
                ErrLog.Instance.E("检测打印机状态错误：{0}", result.Item1);
                OwnerWindow.Dispatcher.Invoke((Action)delegate { MessageDialog.Warning(result.Item1); });
                SetPrinterCheckTimerStatus(true);
                return;
            }

            var errPrintCount = result.Item2.Count(t => t.PrintStatus != EnumPrintStatus.Normal);
            HasPrinterError = errPrintCount > 0;
            if (HasPrinterError && DateTime.Now >= _nextWarningTime)
            {
                var errMsg = string.Format("检测到{0}个打印机异常，请到\"系统\">\"打印机列表\"查看并修复。", errPrintCount);
                InfoLog.Instance.I(errMsg);
                OwnerWindow.Dispatcher.Invoke((Action)delegate
                {
                    var wnd = new PrinterErrorInfoWindow(errMsg, NextMaxWarningInterval);
                    if (WindowHelper.ShowDialog(wnd, OwnerWindow))
                    {
                        _nextWarningTime = wnd.IsCheckedNoWarning ? DateTime.Now.AddMinutes(NextMaxWarningInterval) : DateTime.Now.AddSeconds(PrinterCheckTimerInterval);
                    }
                });
            }

            SetPrinterCheckTimerStatus(true);
        }

        /// <summary>
        /// 定时刷新执行方法。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (DateTime.Now > Globals.TradeTime.BeginTime)//当前时间大于开业时间，强制结业
                {
                    Globals.TradeTime.BeginTime = Globals.TradeTime.BeginTime.AddDays(1);
                    InfoLog.Instance.I("检测到当前时间大于开业时间，触发强制结业流程...");
                    OwnerWindow.Dispatcher.BeginInvoke((Action)delegate { MessageDialog.Warning("昨天还未结业，请先结业。", OwnerWindow); });

                    IsForcedEndWorkModel = true;
                }
                else if (DateTime.Now > Globals.TradeTime.EndTime.AddSeconds(10))//当前时间大于结业时间，提示结业，与真实的时间提前10秒。
                {
                    Globals.TradeTime.EndTime = Globals.TradeTime.EndTime.AddDays(1);
                    InfoLog.Instance.I("检测到当前时间大于结业时间，提示结业...");
                    OwnerWindow.Dispatcher.BeginInvoke((Action)delegate { MessageDialog.Warning("结业时间到了，请及时结业。", OwnerWindow); });
                }

                if (RefreshRemainSecond == 0)
                    OwnerWindow.Dispatcher.BeginInvoke((Action)GetAllTableInfoesAsync);
                else
                    RefreshRemainSecond--;

                //更新餐桌开台持续时间
                foreach (var areaInfo in _allAreaInfos)
                {
                    areaInfo.TableInfos.Where(t => t.TableStatus == EnumTableStatus.Dinner).ToList().ForEach(t => t.UpdateDinnerDuration());
                }
            }
            catch (Exception ex)
            {
                ErrLog.Instance.E(ex);
            }
        }

        /// <summary>
        /// 打印机检测定时器触发时执行。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="elapsedEventArgs"></param>
        private void PrinterCheckTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            CheckPrinterStatus();
        }

        /// <summary>
        /// 异步获取所有餐台信息。
        /// </summary>
        private void GetAllTableInfoesAsync()
        {
            SetRefreshTimerStatus(false);
            RefreshRemainSecond = RefreshTimerInterval;
            var info = _allAreaInfos != null ? "" : "加载所有餐桌信息...";//这里处理是为了第一次显示提示信息，后续定时刷新时候不显示提示信息，防止阻塞其他业务
            TaskService.Start(null, GetAllAreaInfoProcess, GetAllAreaInfoComplete, info);
            TaskService.Start(null, GetBusinessSimpleInfoProcess, GetBusinessSimpleInfoComplete, "");
        }

        /// <summary>
        /// 获取店铺简易信息执行方法。
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private object GetBusinessSimpleInfoProcess(object param)
        {
            var service = ServiceManager.Instance.GetServiceIntance<IRestaurantService>();
            if (service == null)
                return new Tuple<string, BusinessSimpleInfo>("创建IRestaurantService服务失败。", null);

            return service.GetBusinessSimpleInfo();
        }

        /// <summary>
        /// 获取店铺简易信息执行完成。
        /// </summary>
        /// <param name="param"></param>
        private void GetBusinessSimpleInfoComplete(object param)
        {
            var result = (Tuple<string, BusinessSimpleInfo>)param;
            if (!string.IsNullOrEmpty(result.Item1))
            {
                BusinessSimpleInfo = new BusinessSimpleInfo();
                var msg = string.Format("获取店铺基本交易信息失败：{0}", result.Item1);
                ErrLog.Instance.E(msg);
                MessageDialog.Warning(msg);
            }

            BusinessSimpleInfo = result.Item2;
        }

        /// <summary>
        /// 根据餐台类型获取餐台列表的执行类。
        /// </summary>
        /// <param name="param">要获取的餐台类型集合。</param>
        /// <returns></returns>
        private object GetTableInfoByTableTypeProcess(object param)
        {
            var service = ServiceManager.Instance.GetServiceIntance<IRestaurantService>();
            if (service == null)
                return new Tuple<string, List<AreaInfo>>("创建IRestaurantService服务失败。", null);

            return service.GetTableInfoByType((List<EnumTableType>)param);
        }

        /// <summary>
        /// 获取所有餐台信息包含分区信息的执行方法。
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private object GetAllAreaInfoProcess(object param)
        {
            var service = ServiceManager.Instance.GetServiceIntance<IRestaurantService>();
            if (service == null)
                return new Tuple<string, List<AreaInfo>>("创建IRestaurantService服务失败。", null);

            return service.GetAllAreaInfoes();
        }

        /// <summary>
        /// 获取所有餐台信息执行完成。
        /// </summary>
        /// <param name="param"></param>
        private void GetAllAreaInfoComplete(object param)
        {
            var result = (Tuple<string, List<AreaInfo>>)param;
            if (!string.IsNullOrEmpty(result.Item1))
            {
                var errMsg = string.Format("获取所有餐桌分区信息失败：{0}", result.Item1);
                ErrLog.Instance.E(errMsg);
                MessageDialog.Warning(errMsg, OwnerWindow);
                SetRefreshTimerStatus(true);
                return;
            }

            SetRefreshTimerStatus(true);
            if (result.Item2 != null && result.Item2.Any())
            {
                var tableInfos = new List<TableInfo>();
                result.Item2.ForEach(t => tableInfos.AddRange(t.TableInfos.Where(x => !x.IsTakeoutTable)));
                tableInfos.ForEach(SetTableEnableStatus);//设置餐台可用状态。

                _allAreaInfos = result.Item2;
                HasCoffeeTables = tableInfos.Any(t => t.IsCoffeeTable);
                FilterAreaInfo();
            }
        }

        /// <summary>
        /// 获取咖啡外卖信息完成时执行。
        /// </summary>
        /// <param name="param">返回结果。</param>
        private void GetCfTakeoutTableInfoComplete(object param)
        {
            var result = (Tuple<string, List<TableInfo>>)param;
            if (!string.IsNullOrEmpty(result.Item1))
            {
                var errMsg = string.Format("获取咖啡外卖台信息失败：{0}", result.Item1);
                ErrLog.Instance.E(errMsg);
                MessageDialog.Warning(errMsg, OwnerWindow);
                return;
            }

            InfoLog.Instance.I("获取咖啡外卖台结束。");
            if (result.Item2 != null && result.Item2.Any())
            {
                InfoLog.Instance.I("咖啡外卖台个数：{0}", result.Item2.Count);
                var selectTableWnd = new SelectCoffeeTakeoutTableWndVm(result.Item2);
                if (WindowHelper.ShowDialog(selectTableWnd, OwnerWindow))
                {
                    if (!selectTableWnd.IsSelectNormalTakeout)//选择了咖啡外卖。
                    {
                        InfoLog.Instance.I("选择了\"{0}\"咖啡外卖台", selectTableWnd.SelectedTable.TableName);
                        selectTableWnd.SelectedTable.OrderId = null; //清空订单号才会在进入结账页面时开台。
                        WindowHelper.ShowDialog(new TableOperWindow(selectTableWnd.SelectedTable), OwnerWindow);
                        return;
                    }
                }
                else//点击了“取消”或窗口的“X”
                {
                    return;
                }
            }

            InfoLog.Instance.I("开始获取普通外卖台...");
            var request = new List<EnumTableType> { EnumTableType.Takeout };
            TaskService.Start(request, GetTableInfoByTableTypeProcess, GetTakeoutTableInfoComplete, "获取普通外卖台信息中...");
        }

        /// <summary>
        /// 获取普通外卖信息执行完成。
        /// </summary>
        /// <param name="param"></param>
        private void GetTakeoutTableInfoComplete(object param)
        {
            var result = (Tuple<string, List<TableInfo>>)param;
            if (!string.IsNullOrEmpty(result.Item1))
            {
                var errMsg = string.Format("获取普通外卖台信息失败：{0}", result.Item1);
                ErrLog.Instance.E(errMsg);
                MessageDialog.Warning(errMsg, OwnerWindow);
                return;
            }

            if (result.Item2 == null || !result.Item2.Any())
            {
                InfoLog.Instance.I("没有获取到普通外卖台，可能后台未配置。");
                MessageDialog.Warning("后台没有配置普通外卖台，请联系管理员。");
                return;
            }

            var takeoutTable = result.Item2.First();
            takeoutTable.OrderId = null;//外卖这里不能有订单号，不然进入结账页面不得开台。
            InfoLog.Instance.I("获取到普通外卖台：{0}", takeoutTable.TableName);
            WindowHelper.ShowDialog(new TableOperWindow(takeoutTable), OwnerWindow);
        }

        /// <summary>
        /// 设定餐台可用状态。
        /// </summary>
        /// <param name="item"></param>
        private void SetTableEnableStatus(TableInfo item)
        {
            if (item.IsCoffeeTable)
                item.TableEnable = item.TableStatus == EnumTableStatus.Dinner;//咖啡台不允许开台，所以只有当就餐时才允许点击。
            else
                item.TableEnable = !IsForcedEndWorkModel || (item.TableStatus == EnumTableStatus.Dinner);//只有当是强制结业模式且餐台空闲时不可用，其他时候都可用。
        }

        /// <summary>
        /// 清机。
        /// </summary>
        private void ClearnMachine()
        {
            InfoLog.Instance.I("点击清机按钮，选择清机或结业...");
            TaskService.Start(null, GetAllOrderInfosProcess, GetAllOrderInfoComplete, "查询所有账单状态中...");
        }

        /// <summary>
        /// 加载账单信息的执行方法。
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private object GetAllOrderInfosProcess(object arg)
        {
            InfoLog.Instance.I("开始获取账单数据...");
            var service = ServiceManager.Instance.GetServiceIntance<IRestaurantService>();
            if (service == null)
                return new Tuple<string, List<QueryOrderInfo>>("创建IRestaurantService服务失败。", null);

            return service.QueryOrderInfos(Globals.UserInfo.UserName);
        }

        /// <summary>
        /// 加载账单执行完成。
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private void GetAllOrderInfoComplete(object arg)
        {
            var result = (Tuple<string, List<QueryOrderInfo>>)arg;
            if (!string.IsNullOrEmpty(result.Item1))
            {
                ErrLog.Instance.E(result.Item1);
                MessageDialog.Warning(result.Item1, OwnerWindow);
                return;
            }

            InfoLog.Instance.I("账单查询完成。");
            var hasDinnerTable = result.Item2.Any(t => t.OrderStatus == EnumOrderStatus.Ordered);
            var allowCash = !IsForcedEndWorkModel && Globals.UserRight.AllowClearn;//只有当不是强制结业且有清机权限时才允许清机。
            var wnd = new SelectClearnStepWndVm(!hasDinnerTable, allowCash);
            if (!WindowHelper.ShowDialog(wnd, OwnerWindow))
                return;

            if (wnd.IsEndWork)
            {
                InfoLog.Instance.I("选择的是结业，开始判断所有机器是否都清机。 ");
                if (!CommonHelper.ClearAllPos(false))
                    return;

                InfoLog.Instance.I("所有机器都清机完成，开始结业...");
                EndWork();
            }
            else
            {
                if (CommonHelper.ClearPos(Globals.UserInfo.UserName))
                    CommonHelper.ForcedLogin();
            }
        }

        /// <summary>
        /// 结业。
        /// </summary>
        private void EndWork()
        {
            InfoLog.Instance.I("结业授权...");
            if (!WindowHelper.ShowDialog(new AuthorizationWndVm(EnumRightType.EndWork), OwnerWindow))
            {
                InfoLog.Instance.I("结业授权失败，终止结业。");
                return;
            }

            InfoLog.Instance.I("结业授权成功，开始调用结业接口...");
            TaskService.Start(null, EndWorkProcess, EndWorkComplete, "结业中...");
        }

        /// <summary>
        /// 结业执行方法。
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private object EndWorkProcess(object param)
        {
            var service = ServiceManager.Instance.GetServiceIntance<IRestaurantService>();
            if (service == null)
                return "创建IRestaurantService服务失败。";

            return service.EndWork();
        }

        /// <summary>
        /// 结业执行完成。
        /// </summary>
        /// <param name="param"></param>
        private void EndWorkComplete(object param)
        {
            var result = (string)param;
            if (!string.IsNullOrEmpty(result))
            {
                ErrLog.Instance.E("结业接口调用失败：{0}", result);
                MessageDialog.Warning(result, OwnerWindow);
                return;
            }

            InfoLog.Instance.I("结业成功，开始调用结业上传数据接口...");
            EndWorkSyncData();
        }

        /// <summary>
        /// 结业后JDE同步数据方法的异步执行。
        /// </summary>
        private void EndWorkSyncData()
        {
            TaskService.Start(null, EndWorkSyncDataProcess, EndWorkSyncDataComplete, "通知结业同步数据...");
        }

        /// <summary>
        /// 结业后JDE同步数据的执行方法。
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private object EndWorkSyncDataProcess(object param)
        {
            var service = ServiceManager.Instance.GetServiceIntance<IRestaurantService>();
            if (service == null)
                return "创建IRestaurantService服务失败。";

            return service.EndWorkSyncData();
        }

        /// <summary>
        /// 结业后JDE同步数据的执行完成。
        /// </summary>
        /// <param name="param"></param>
        private void EndWorkSyncDataComplete(object param)
        {
            var result = (string)param;
            if (!string.IsNullOrEmpty(result))
            {
                ErrLog.Instance.E("结业后上传数据接口调用失败：{0}", result);
                if (WindowHelper.ShowDialog(new JdeReloadWindow()))
                {
                    InfoLog.Instance.I("选择了重新上传结业数据...");
                    EndWorkSyncData();
                }

                InfoLog.Instance.I("选择取消重新上传结业数据。");
            }
            else
            {
                InfoLog.Instance.I("结业数据上传成功。");
                MessageDialog.Warning("结业成功，即将退出程序。", OwnerWindow);
            }

            Application.Current.Shutdown();
        }

        /// <summary>
        /// 设置刷新定时器的状态。
        /// </summary>
        /// <param name="enable">启动定时器时为true，停止则为false。</param>
        private void SetRefreshTimerStatus(bool enable)
        {
            if (_isDisposed)
                return;

            _refreshTimer.Enabled = enable;
        }

        /// <summary>
        /// 设置打印机检测定时器的状态。
        /// </summary>
        /// <param name="enable"></param>
        private void SetPrinterCheckTimerStatus(bool enable)
        {
            if (_isDisposed)
                return;

            _printerCheckTimer.Enabled = enable;
        }

        #region 餐台过滤分组

        /// <summary>
        /// 过滤餐桌分区信息。
        /// </summary>
        private void FilterAreaInfo()
        {
            if (_isInFilter)
                return;

            if (_allAreaInfos == null)
                return;

            _isInFilter = true;
            var areaInfos = new List<AreaInfo>();

            //添加全部分区。
            var allAreaInfo = new AreaInfo { AreaName = "全部" };
            areaInfos.Add(allAreaInfo);

            var allTableInfos = new List<TableInfo>();
            foreach (var areaInfo in _allAreaInfos)
            {
                var tableInfos = areaInfo.TableInfos.Where(t => !t.IsTakeoutTable && ViewTableType == EnumViewTableType.Normal ? !t.IsCoffeeTable : t.IsCoffeeTable).ToList();
                if (tableInfos.Any())//有餐台的分区才显示。
                {
                    var areaTemp = areaInfo.SimpleClone();
                    tableInfos.ForEach(areaTemp.TableInfos.Add);
                    areaInfos.Add(areaTemp);
                    allTableInfos.AddRange(tableInfos);//把所有餐桌的都加入一个集合，赋值给全部分组。
                }
            }
            foreach (var tableInfo in allTableInfos)
            {
                tableInfo.UpdateDinnerDuration();
                allAreaInfo.TableInfos.Add(tableInfo);
            }
            AllTableCount = allTableInfos.Count;
            IdleCount = allTableInfos.Count(t => !IsForcedEndWorkModel && t.TableStatus == EnumTableStatus.Idle);
            DinnerCount = allTableInfos.Count(t => t.TableStatus == EnumTableStatus.Dinner);

            UpdateAreaInfos(areaInfos);

            FilterTableInfos();

            _isInFilter = false;
        }

        /// <summary>
        /// 过滤餐台集合。
        /// </summary>
        private void FilterTableInfos()
        {
            var curSelectedTableIndex = OwnerWnd.GsTables.SelectedGroupIndex;
            TableInfos.Clear();
            foreach (var tableInfo in SelectedAreaInfo.TableInfos)
            {
                switch (ViewTableStatus)
                {
                    case EnumViewTableStatus.All:
                        TableInfos.Add(tableInfo);
                        break;
                    case EnumViewTableStatus.Dinner:
                        if (tableInfo.IsInDinner)
                            TableInfos.Add(tableInfo);
                        break;
                    case EnumViewTableStatus.Idel:
                        if (!tableInfo.IsInDinner)
                            TableInfos.Add(tableInfo);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            OwnerWnd.GsTables.SelectedGroupIndex = curSelectedTableIndex;
        }

        private void UpdateAreaInfos(List<AreaInfo> src)
        {
            var selectedItem = SelectedAreaInfo;
            AreaInfos.Clear();
            src.ForEach(t => AreaInfos.Add(t.Clone()));

            foreach (var areaInfo in AreaInfos)
            {
                areaInfo.TableInfos.Clear();
                var newItem = src.FirstOrDefault(t => t.AreaName.Equals(areaInfo.AreaName));
                if (newItem != null)
                    newItem.TableInfos.ToList().ForEach(areaInfo.TableInfos.Add);
            }

            if (selectedItem == null)
                return;

            var tempItem = AreaInfos.FirstOrDefault(t => t.AreaName.Equals(selectedItem.AreaName));
            if (tempItem == null)
                return;

            var index = AreaInfos.IndexOf(tempItem);
            OwnerWnd.GsArea.SelectedGroupIndex = index / OwnerWnd.GsArea.ItemCountEachGroup;
            SelectedAreaInfo = tempItem;
        }

        #endregion

        #endregion
    }
}