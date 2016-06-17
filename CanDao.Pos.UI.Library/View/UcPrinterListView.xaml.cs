﻿using System.Windows;
using CanDao.Pos.UI.Library.ViewModel;
using Common;

namespace CanDao.Pos.UI.Library.View
{
    /// <summary>
    /// 打印机状态列表显示控件。
    /// </summary>
    public partial class UcPrinterListView
    {
        public UcPrinterListView()
        {
            InitializeComponent();
            ((UcPrinterListViewModel) DataContext).OwnerCtrl = this;
        }
    }
}
