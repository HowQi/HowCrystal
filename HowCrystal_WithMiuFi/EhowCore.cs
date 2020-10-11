using System;
using System.Collections.Generic;
using System.Text;
using HowCrystal.LoTool;
using Mirai_CSharp;
using Mirai_CSharp.Models;

namespace HowCrystal
{
    public class EhowCore
    {
        Crystal crystal;

        public readonly HowServerLiveListener liveListener;
        public readonly CPLiveNotification liveNotificationPlg;

        public EhowCore()
        {
            crystal = new Crystal();
            liveListener = new HowServerLiveListener();
            liveNotificationPlg = new CPLiveNotification(liveListener, crystal);
            RegisterPlugin(liveNotificationPlg);
        }

        public void SwitchOn()
        {
            try
            {
                crystal.ConnectStart();
                liveListener.StartListener();
            }
            catch (Exception ex)
            {
                HowLog.LogError("开启bot时遇到问题。异常内容:" + ex);
            }
            HowLog.LogInfo("[How operate]发起连接操作");
        }

        public void SwitchOff()
        {
            liveListener.StopListener();
            crystal.CloseConnect();
            HowLog.LogInfo("[How operate]关闭连接操作");
        }

        private void RegisterPlugin(CrystalPlugin cp)
        {
            cp.EvtSendFriendMessage += crystal.SendFriendMessage;
            cp.EvtSendGroupMessage += crystal.SendGroupMessage;

            crystal.Plugin.EvtFriendMsg += e =>
            {
                cp.ReceiveFriendMessage(e.Sender.Id, e.Chain);
            };
            crystal.Plugin.EvtGroupMsg += e =>
            {
                cp.ReceiveGroupMessage(e.Sender.Id, e.Chain);
            };
        }

        //===========私有方法==============//
        //private void SendGroupMessage(long group, params IMessageBase[] msgs) => crystal.SendGroupMessage(group, msgs);
    }
}
