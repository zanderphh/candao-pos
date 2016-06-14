﻿using System;
using System.Windows.Forms;
using WebServiceReference;

namespace Library
{
    public partial class FrmRetry : frmBase
    {
        public FrmRetry(string errMsg)
        {
            InitializeComponent();
            RtbMsg.Text = errMsg;
        }

        private void btnRetry_Click(object sender, EventArgs e)
        {
            btnRetry.Enabled = false;
            Application.DoEvents();
            var result = RestClient.CheckServerConnection();
            btnRetry.Enabled = true;
            if (string.IsNullOrEmpty(result))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            RtbMsg.Text = result;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
