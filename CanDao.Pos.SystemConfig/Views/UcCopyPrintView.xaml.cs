﻿using CanDaoCD.Pos.Common.Controls.CSystem;
using CanDaoCD.Pos.Common.PublicValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CanDaoCD.Pos.SystemConfig.ViewModels;
using CanDaoCD.Pos.Common.Models;
using CanDaoCD.Pos.Common.Operates.FileOperate;

namespace CanDaoCD.Pos.SystemConfig.Views
{
    /// <summary>
    /// UcCopyPrintView.xaml 的交互逻辑
    /// </summary>
    public partial class UcCopyPrintView : UserControlBase
    {
        public UcCopyPrintView()
        {
            InitializeComponent();
            var viewModel = new UcCopyPrintViewModel();

            this.DataContext = viewModel;
            viewModel.InitContor(this);
        }

        #region 事件

        public Action EnterAction;

        #endregion

        private void CkBullet_OnUnchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                PvSystemConfig.VSystemConfig.IsEnabledPrint = false;
                OXmlOperate.SerializerFile<MSystemConfig>(PvSystemConfig.VSystemConfig, PvSystemConfig.VSystemConfigFile);
            }
            catch
            {

            }

        }

        private void CkBullet_OnChecked(object sender, RoutedEventArgs e)
        {
            TexNum.Focus();
        }

        private void TexNum_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (EnterAction != null)
                {
                    EnterAction();
                }
            }
        }
    }
}
