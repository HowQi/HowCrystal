using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Mirai_CSharp;
using Mirai_CSharp.Models;
using Mirai_CSharp.Plugin;
using Mirai_CSharp.Plugin.Interfaces;

using static HowCrystal.LoTool.HowLog;

namespace HowCrystal
{
    public partial class Crystal
    {
        public class MsgEvtPlugin : IPlugin, IGroupMessage, IFriendMessage
        {

            public event Action<IFriendMessageEventArgs> EvtFriendMsg;

            public event Action<IGroupMessageEventArgs> EvtGroupMsg;

            public async Task<bool> FriendMessage(MiraiHttpSession session, IFriendMessageEventArgs e)
            {
                if (EvtFriendMsg != null)
                {
                    EvtFriendMsg(e);
                }
                return false;
            }

            public async Task<bool> GroupMessage(MiraiHttpSession session, IGroupMessageEventArgs e)
            {

                if (e.Chain.Length >= 2)
                {
                    var msgChain = e.Chain;
                    if (msgChain[1] is PlainMessage)
                    {
                        var msg = (msgChain[1] as PlainMessage).ToString();
                        if (msg.StartsWith("+about"))
                        {
                            var rspChain = new IMessageBase[]
                            {
                                new PlainMessage("你好,这里是Miu-Fi bot。Miu-Fi bot前身为ehow bot,祭奠天国的ehow bot。Miu-Fi全称为Miu-Fi世界同步状态机,负责对不同时空和次元的规律因子进行同步,从而实现跨次元通信的机器。\r\n"
                                +"Miu-Fi bot是Miu-Fi的一部分,本来是作为通信模块的部件之一,后来换修时遇到了强买强卖,不得已附加了聊天bot功能。\r\n"
                                +"Miu-Fi bot的功能离不开开源框架Mirai和MiraiCSharp"),
                            };

                            await session.SendGroupMessageAsync(e.Sender.Group.Id, rspChain);
                        }
                        else if (msg.StartsWith("+help"))
                        {
                            var rspChain = new IMessageBase[]
                            {
                                new PlainMessage("Miu-Fi bot是一个可扩展性不行的Bot,Help信息还得写好插件后手动在这里写帮助。下面是已经有的功能"
                                    +"HowCrystal自带的——基本功能:{关于信息[+about]}、{帮助信息[+help]}"
                                    +"配置的时候还得手操的——直播信息收听通知功能:{直播通知服务}。"
                                ),
                            };

                            await session.SendGroupMessageAsync(e.Sender.Group.Id, rspChain);
                        }
                    }
                }


                if (EvtGroupMsg != null)
                {
                    EvtGroupMsg(e);
                }


                return false;
            }


        }
    }
}
