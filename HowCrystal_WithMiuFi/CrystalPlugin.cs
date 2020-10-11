using Mirai_CSharp;
using Mirai_CSharp.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.Xml;
using System.Text;

namespace HowCrystal
{
    public abstract class CrystalPlugin
    {
        protected MiraiHttpSession Session
        {
            get { return crystal.Session; }
        }
        protected Crystal crystal;
        public CrystalPlugin(Crystal crystal_)
        {
            crystal = crystal_;
        }

        public event FunSendMessage EvtSendGroupMessage;
        protected void TriggerSendGroupMessage(long id, IMessageBase[] msgs)
        {
            EvtSendGroupMessage(id, msgs);
        }

        public event FunSendMessage EvtSendFriendMessage;

        protected void TriggerSendFriendMessage(long id, IMessageBase[] msgs)
        {
            EvtSendFriendMessage(id, msgs);
        }

        public virtual void ReceiveGroupMessage(long groupId, IMessageBase[] msgs) { }

        public virtual void ReceiveFriendMessage(long groupId, IMessageBase[] msgs) { }

        public delegate void FunSendMessage(long id, IMessageBase[] msgs);
    }
}
