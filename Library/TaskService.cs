﻿using System;
using Common;

namespace Library
{
    /// <summary>
    /// 后台线程服务。
    /// </summary>
    public class TaskService
    {
        /// <summary>
        /// 开始执行后台任务。
        /// </summary>
        /// <param name="param">处理参数。</param>
        /// <param name="processHandler">处理过程委托。</param>
        /// <param name="complateHandler">完成后执行委托。</param>
        /// <param name="infoMsg">提示信息，如果纯后台执行则传null即可。</param>
        public static void Start(object param, Func<object, object> processHandler, Action<object> complateHandler = null, string infoMsg = "处理中...")
        {
            TaskBase taskBase = new TaskBase();
            frmWarning frmMsg = null;

            if (!string.IsNullOrEmpty(infoMsg))
            {
                taskBase.BeforeProcessAction = delegate
                {
                    frmMsg = new frmWarning(infoMsg);
                    frmMsg.ShowDialog();
                };

                taskBase.AfterProcessAction = delegate
                {
                    if (frmMsg != null)
                        frmMsg.Dispose();
                };
            }
            taskBase.Start(param, processHandler, complateHandler);
        }
    }
}