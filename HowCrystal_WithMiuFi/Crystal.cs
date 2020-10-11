using System;
using System.Collections.Generic;
using System.Text;
using HowCrystal.LoTool;
using Mirai_CSharp;
using Mirai_CSharp.Models;
using System.Threading;
using log4net.Util;
using System.Threading.Tasks;
using HowSimpleObjectTool;

using static HowCrystal.LoTool.HowLog;
using System.IO;

namespace HowCrystal
{

    public partial class Crystal
    {

        bool closeHandle = false;

        bool isConnect = false;

        readonly object stateEntrance = new object();

        MsgEvtPlugin plugin;

        MiraiHttpSession session = null;

        public MiraiHttpSession Session { get => session; }
        public MsgEvtPlugin Plugin { get => plugin; }

        public Crystal()
        {
            plugin = new MsgEvtPlugin();
        }

        private async void MainWorking()
        {
            string stateDeclar = "连接中";
            try
            {
                closeHandle = false;
                if (isConnect)
                {
                    HowLog.LogError("非法操作:已经连接,不可再连");
                    return;
                }

                HowLog.LogInfo("开始连接。");

                //配置会话
                string host, authK;
                int port;
                long botQQ;
                using (var sr = new StreamReader(@"HowCfg\服务器设置.hso"))
                {
                    var obj = HowSimpleObjectBuilder.ReadObj(sr);
                    host = obj["host"].Value;
                    authK = obj["authKey"].Value;
                    port = int.Parse(obj["port"].Value);
                    botQQ = long.Parse(obj["qq"].Value);
                    if (host == null || authK == null) throw new Exception("读取服务器配置文件失败");
                }
                MiraiHttpSessionOptions opts = new MiraiHttpSessionOptions(host, 7456, authK);
                await using MiraiHttpSession session_ = new MiraiHttpSession();
                session = session_;
                session_.DisconnectedEvt += Session_DisconnectedEvt;

                //配置插件

                session_.AddPlugin(plugin);
                await session_.ConnectAsync(opts, botQQ);
                stateDeclar = "工作状态";

                isConnect = true;
                LogInfo("连接成功!");

                while (!closeHandle && isConnect)
                {
                    Thread.Sleep(100);
                }
                session = null;
                string chState = closeHandle ? "是" : "否";
                string icState = isConnect ? "否" : "是";
                HowLog.LogInfo($"退出维持循环,引发退出的变量为:终止拉杆?{chState};连接断开?{icState}");
            }
            catch (Exception ex)
            {
                LogError(stateDeclar + "遇到问题。详情:" + ex.ToString());
            }
            HowLog.LogInfo("工作线程结束,清理工作将由异步释放功能自动进行……");
            isConnect = false;
        }

        private async System.Threading.Tasks.Task<bool> Session_DisconnectedEvt(MiraiHttpSession sender, Exception e)
        {
            isConnect = false;
            HowLog.LogInfo("连接断开了。");
            return false;
        }

        public void ConnectStart()
        {
            Task.Run(MainWorking);
        }

        public void CloseConnect()
        {
            closeHandle = true;
            HowLog.LogInfo("断开指令已发放。");
        }

        public void SendFriendMessage(long id, params IMessageBase[] msgs)
        {
            try
            {
                var tSession = session;
                tSession?.SendFriendMessageAsync(id, msgs);
                if (tSession == null)
                {
                    LogWarn("[How Warn]服务为关闭状态,但有模块在尝试发送好友消息");
                }
            }
            catch (Exception ex)
            {
                string msgstr = "";
                foreach (var item in msgs)
                {
                    msgstr += item.ToString();
                }
                LogError(string.Format("发送好友消息失败。参数group={0},msg={1},Exception:{2}", id.ToString(), msgstr, ex.ToString()));
            }
        }

        public void SendGroupMessage(long id, params IMessageBase[] msgs)
        {
            try
            {
                var tSession = session;
                tSession?.SendGroupMessageAsync(id, msgs);
                if (tSession == null)
                {
                    LogWarn("[How Warn]服务为关闭状态,但有模块在尝试发送群消息");
                }
            }
            catch (Exception ex)
            {
                string msgstr = "";
                foreach (var item in msgs)
                {
                    msgstr += item.ToString();
                }
                LogError(string.Format("发送群消息失败。参数group={0},msg={1},Exception:{2}", id.ToString(), msgstr, ex.ToString()));
            }
        }


    }
}
