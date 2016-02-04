﻿using System.Linq;
using System.Windows;
using WebServiceReference;
using WebServiceReference.IService;
using WebServiceReference.ServiceImpl;

namespace Library
{
    /// <summary>
    /// 其他机器未清机提示信息窗口。
    /// </summary>
    public partial class OtherMachineNoClearnWarningWindow
    {
        public OtherMachineNoClearnWarningWindow()
        {
            InitializeComponent();
        }

        private void ButtonCancel_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnRetry_OnClick(object sender, RoutedEventArgs e)
        {
            IRestaurantService service = new RestaurantServiceImpl();
            var result = service.GetUnclearnPosInfo();
            if (!string.IsNullOrEmpty(result.Item1))
            {
                frmWarning.ShowWarning(result.Item1);
                return;
            }

            var noClearnMachineList = result.Item2;
            if (noClearnMachineList.Any()) //这里只需要判断有未清机的就不关闭窗口。
                return;

            DialogResult = true;
            Close();
        }
    }
}
