using Mirai_CSharp.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using HowSimpleObjectTool;
using System.IO;
using HowCrystal.LoTool;
using System.Drawing;
using System.Net;
using TheToolKit;

using static HowCrystal.InternetConst;
using System.Drawing.Text;
using Mirai_CSharp;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Threading;

namespace HowCrystal
{
    public class CPLiveNotification : CrystalPlugin
    {
        HowServerLiveListener liveListener;

        /// <summary>
        /// 字典以主播房间号为键,储存注册的群or用户的信息
        /// </summary>
        Dictionary<long, List<EntityInfo>> liveNotifyDict = new Dictionary<long, List<EntityInfo>>();

        /// <summary>
        /// 以主播房间号为键,储存上次取得的直播间播放状态
        /// </summary>
        Dictionary<long, PlayingState> playingStateHis = new Dictionary<long, PlayingState>();
        /// <summary>
        /// 以主播房间号为键,储存上次取得的直播间完整状态
        /// </summary>
        Dictionary<long, HowServerLiveListener.LiveRoomInfo_Simple> liveStateHis = new Dictionary<long, HowServerLiveListener.LiveRoomInfo_Simple>();

        object listenArgEntrance = new object();

        public CPLiveNotification(HowServerLiveListener liveListener_, Crystal crystal_)
            : base(crystal_)
        {
            liveListener = liveListener_;
            liveListener.EvtLiveNotification += LiveListener_EvtLiveNotification;
            InitDrawer();
            RefreshListenTargetCfg();
            RefreshNotifyTargetCfg();
        }

        //============事件处理=============
        private async void LiveListener_EvtLiveNotification(HowServerLiveListener.LiveRoomInfo roomInfo, HowServerLiveListener.LiveRoomInfo_Simple roomInfo_Simple)
        {
            try
            {
                if (roomInfo_Simple != null)
                {
                    var curSinfo = roomInfo_Simple;
                    PlayingState curPState = (PlayingState)(curSinfo.data.live_status);
                    long roomId = curSinfo.data.room_id;

                    var oldSinfo = liveStateHis[roomId];
                    var oldPState = playingStateHis[roomId];

                    //判断状态是否更新了
                    bool shoudlSendMsg = false;
                    IMessageBase[] msgsGroup = null;
                    IMessageBase[] msgsFriend = null;
                    if (curPState == PlayingState.Living && oldPState.IsNoLiving())
                    {
                        //开播。
                        shoudlSendMsg = true;
                        msgsGroup = await GenLiveCCardsAsync(null,
                                curSinfo.data.live_time,
                                liveListener.GetAnchorInfo(curSinfo.data.room_id).displayName,
                                "开始直播",
                                curSinfo.data.title,
                                curSinfo.data.user_cover,
                                EntityType.Group
                             );
                        msgsFriend = await GenLiveCCardsAsync(null,
                                curSinfo.data.live_time,
                                liveListener.GetAnchorInfo(curSinfo.data.room_id).displayName,
                                "开始直播",
                                curSinfo.data.title,
                                curSinfo.data.user_cover,
                                EntityType.Friend
                             );
                    }
                    else if (curPState.IsNoLiving() && oldPState == PlayingState.Living)
                    {
                        //下播。
                        shoudlSendMsg = true;
                        msgsFriend = msgsGroup = new IMessageBase[] {
                            new PlainMessage(liveListener.GetAnchorInfo(curSinfo.data.room_id).displayName
                            + "下播了(" + DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss") + ")"),
                        };
                    }
                    else if (curPState == PlayingState.Living && oldPState == PlayingState.Living
                       && curSinfo.data.title != oldSinfo.data.title)
                    {
                        //标题更换。
                        shoudlSendMsg = true;
                        msgsGroup = await GenLiveCCardsAsync(null,
                                curSinfo.data.live_time,
                                liveListener.GetAnchorInfo(curSinfo.data.room_id).displayName,
                                "更换标题",
                                curSinfo.data.title,
                                curSinfo.data.user_cover,
                                EntityType.Group
                             );
                        msgsFriend = await GenLiveCCardsAsync(null,
                               curSinfo.data.live_time,
                               liveListener.GetAnchorInfo(curSinfo.data.room_id).displayName,
                               "更换标题",
                               curSinfo.data.title,
                               curSinfo.data.user_cover,
                               EntityType.Friend
                            );
                    }

                    //将旧的状态储存起来
                    liveStateHis[roomId] = curSinfo;
                    playingStateHis[roomId] = curPState;

                    //分发消息
                    if (shoudlSendMsg)
                    {
                        Dictionary<long, List<EntityInfo>> notifyDict;
                        lock (listenArgEntrance)
                        {
                            notifyDict = liveNotifyDict;
                        }
                        var list = notifyDict[roomInfo.data.room_id];
                        if (list != null)
                        {
                            foreach (var entity in list)
                            {
                                if (entity.type == EntityType.Group)
                                {
                                    TriggerSendGroupMessage(entity.id, msgsGroup);
                                }
                                else if (entity.type == EntityType.Friend)
                                {
                                    TriggerSendFriendMessage(entity.id, msgsFriend);
                                }
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                HowLog.LogError("处理直播消息状态时遇到错误!" + ex);
            }
        }

        //============消息绘制=============

        async Task<IMessageBase[]> GenLiveCCardsAsync(string firstMsg, string strarTime, string anchorName, string behavior, string title, string coverUrl, EntityType entityType)
        {
            Image notiImg = GenNotifyImg(strarTime, anchorName, behavior, title, coverUrl);

            /*
            if (entityType == EntityType.Group)
            {
                Image notiImgSmall = new Bitmap(350, 350);
                using (var g = Graphics.FromImage(notiImgSmall))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.DrawImage(notiImg, new Rectangle(0, 0, notiImg.Width, notiImg.Height));
                }
                notiImg = notiImgSmall;
            }
            */

            MemoryStream imgStm = new MemoryStream();
            notiImg.Save(imgStm, ImageFormat.Png);

            ImageMessage imgMsg;
            using (imgStm)
            {
                if (entityType == EntityType.Group)
                {
                    imgMsg = await Session.UploadPictureAsync(UploadTarget.Group, imgStm);
                    Thread.Sleep(500);
                }
                else
                {
                    imgMsg = await Session.UploadPictureAsync(UploadTarget.Friend, imgStm);
                    Thread.Sleep(500);
                }
            }
            if (string.IsNullOrEmpty(firstMsg))
            {
                return new IMessageBase[] { imgMsg };
            }
            else
            {
                return new IMessageBase[] {
                    new PlainMessage(firstMsg),
                    imgMsg,
                };
            }
        }

        #region == 绘图便利函数 ==

        /// <summary>
        /// 以期望的行高 计算字体应该的尺寸(Pt)
        /// </summary>
        /// <param name="fontName"></param>
        /// <param name="expHeight"></param>
        /// <returns></returns>
        float CalcuFontPt(FontFamily fontFamily, int expHeight, float dpi)
        {
            var ff = fontFamily;

            var ascent = ff.GetCellAscent(FontStyle.Regular);
            var dscent = ff.GetCellDescent(FontStyle.Regular);
            float emHi = ff.GetEmHeight(FontStyle.Regular);

            float fontGrow = (ascent + dscent) / emHi;
            float res = (float)expHeight / dpi * 72f / fontGrow;
            return res;
        }

        float CalcuFontPt(string fontName, int expHeight, float dpi)
        {
            return CalcuFontPt(new FontFamily(fontName), expHeight, dpi);
        }

        #endregion


        #region == 绘图工作 ==

        PrivateFontCollection fontColl;
        FontFamily cardFont;
        void InitDrawer()
        {
            fontColl = new PrivateFontCollection();
            fontColl.AddFontFile("SourceHanMonoSC-Regular.otf");
            cardFont = fontColl.Families[0];
        }

        Image GenNotifyImg(string occuredTime, string anchorName, string behavior, string title, string coverUrl)
        {
            Image cover = null;
            if (!string.IsNullOrEmpty(coverUrl.Trim()))
            {
                try
                {
                    HttpWebRequest req = WebRequest.CreateHttp(coverUrl);
                    req.Method = "GET";
                    req.UserAgent = UserAgent;
                    var resp = req.GetResponse() as HttpWebResponse;
                    using (var stm = resp.GetResponseStream())
                    {
                        var timg = Image.FromStream(stm);
                        cover = timg;
                    }
                }
                catch (Exception ex)
                {
                    HowLog.LogWarn("获取图像遇到错误:" + ex);
                }
            }
            if (cover == null)
            {
                cover = CrystalRes.Miao;
            }

            Bitmap btmp = new Bitmap(500, 500);
            Rectangle rectBgLay2 = new Rectangle(25, 0, 450, 400);
            Rectangle rectCover = new Rectangle(40, 25, 420, 265);
            Rectangle rectTitleDouble = new Rectangle(40, 300, 420, 90);
            Rectangle rectTitleSingle = new Rectangle(40, 315, 420, 70);
            Rectangle rectTime = new Rectangle(25, 405, 450, 25);
            Rectangle rectActionDescribe = new Rectangle(25, 435, 450, 60);

            SolidBrush bshBgLay2 = new SolidBrush(Color.FromArgb(255, 254, 226));
            SolidBrush bshStr = new SolidBrush(Color.FromArgb(175, 64, 0));

            StringFormat fmtAllCen = new StringFormat();
            fmtAllCen.Alignment = StringAlignment.Center;
            fmtAllCen.LineAlignment = StringAlignment.Center;
            StringFormat fmtJustCen = new StringFormat();
            fmtJustCen.Alignment = StringAlignment.Center;


            using (var g = Graphics.FromImage(btmp))
            using (bshBgLay2)
            using (bshStr)
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                float dpiX = g.DpiX;

                //背景
                g.Clear(Color.White);

                //背景2层
                g.FillRectangle(bshBgLay2, rectBgLay2);

                //绘制封面图
                var realCoverRect = rectCover.GetInnerRect(cover.Size);
                g.DrawImage(cover, realCoverRect);

                //日期字符
                float timeStrPt = CalcuFontPt(cardFont, rectTime.Height, dpiX);
                g.DrawString(CaidanGenerator_Gongyuan() + occuredTime, new Font(cardFont, timeStrPt), bshStr, rectTime, fmtAllCen);

                //描述区域
                float descStrPtMin = CalcuFontPt(cardFont, (int)(rectActionDescribe.Height * 0.65f), dpiX);
                float descStrPtRaw = CalcuFontPt(cardFont, rectActionDescribe.Height, dpiX);
                float descStrPt = descStrPtRaw;
                if (descStrPtRaw < descStrPtMin) descStrPt = descStrPtMin;
                g.DrawString(anchorName + " " + behavior, new Font(cardFont, descStrPt), bshStr, rectActionDescribe, fmtAllCen);

                //主标题
                float titleStrPtMin = CalcuFontPt(cardFont, rectTitleDouble.Height / 2, dpiX);
                float titleStrPtMax = CalcuFontPt(cardFont, rectTitleSingle.Height, dpiX);
                var sz = g.MeasureString(title, new Font(cardFont, 60f));
                float titleStrPtRaw = CalcuFontPt(cardFont, (int)(sz.Height / (sz.Width * 1.1f) * rectTitleDouble.Width), dpiX);
                float titleStrPt = titleStrPtRaw;
                if (titleStrPtRaw < titleStrPtMin)
                {
                    titleStrPt = titleStrPtMin;
                }
                else if (titleStrPtRaw > titleStrPtMax)
                {
                    titleStrPt = titleStrPtMax;
                }
                int meLineCount;
                Font tFont = new Font(cardFont, titleStrPt);
                g.MeasureString(title, tFont, new Size(rectTitleDouble.Width, 5000), fmtJustCen, out _, out meLineCount);
                if (meLineCount == 1)
                {
                    g.DrawString(title, tFont, bshStr, rectTitleDouble, fmtAllCen);
                }
                else
                {
                    g.DrawString(title, tFont, bshStr, rectTitleDouble, fmtJustCen);
                }
            }
            return btmp;
        }

        string CaidanGenerator_Gongyuan()
        {
            Random r = new Random();
            var ranres = r.NextDouble();
            if (ranres < 0.1f)
            {
                return "公元前 ";
            }
            return "";
        }
        #endregion

        //============操作入口=============
        public void RefreshListenTargetCfg()
        {
            try
            {
                var infos = GetAnchorInfo("HowCfg\\直播观测目标.hso");
                Dictionary<long, PlayingState> tPlayingState = new Dictionary<long, PlayingState>();
                Dictionary<long, HowServerLiveListener.LiveRoomInfo_Simple> tState = new Dictionary<long, HowServerLiveListener.LiveRoomInfo_Simple>();
                foreach (var item in infos)
                {
                    tPlayingState[item.roomId] = PlayingState.Unknow;
                    tState[item.roomId] = new HowServerLiveListener.LiveRoomInfo_Simple();
                }
                lock (listenArgEntrance)
                {
                    liveListener.ConfigurateListenTarget(infos);
                    playingStateHis = tPlayingState;
                    liveStateHis = tState;
                }
            }
            catch (Exception ex)
            {
                HowLog.LogError("[Error]从文件中获取注册的主播信息失败。详情:" + ex);
            }
        }

        HowServerLiveListener.AnchorInfo[] GetAnchorInfo(string path)
        {
            using (var sr = new StreamReader(path, Encoding.UTF8))
            {
                var cfgs = HowSimpleObjectBuilder.ReadObjAry(sr);
                var infos = new HowServerLiveListener.AnchorInfo[cfgs.Length];
                for (int i = 0; i < cfgs.Length; i++)
                {
                    infos[i] = HowServerLiveListener.AnchorInfo.FromHSO(cfgs[i]);
                }
                return infos;
            }
        }

        public void RefreshNotifyTargetCfg()
        {
            try
            {
                //提供主播信息以允许主播简称(crystal id)
                HowServerLiveListener.AnchorInfo[] aInfos = null;
                try
                {
                    aInfos = GetAnchorInfo("HowCfg\\直播观测目标.hso");
                }
                catch (Exception) { }

                var tempLiveNotiDict = new Dictionary<long, List<EntityInfo>>();

                using (var sr = new StreamReader("HowCfg\\直播服务注册者.hso"))
                {
                    var cfgs = HowSimpleObjectBuilder.ReadObjAry(sr);
                    for (int i = 0; i < cfgs.Length; i++)
                    {
                        var cfg = cfgs[i];
                        var rInfo = RegistrantInfo.FromHSO(cfg, aInfos);
                        foreach (var roomId in rInfo.roomIds)
                        {
                            if (tempLiveNotiDict.ContainsKey(roomId))
                            {
                                //将群号添加到特定主播的通知列表中
                                tempLiveNotiDict[roomId].Add(new EntityInfo(rInfo.type, rInfo.id));
                            }
                            else
                            {
                                //没列表时建立列表
                                tempLiveNotiDict[roomId] = new List<EntityInfo>();
                                //将群号添加到特定主播的通知列表中
                                tempLiveNotiDict[roomId].Add(new EntityInfo(rInfo.type, rInfo.id));
                            }
                        }
                    }
                }
                //将准备好的通知字典覆盖旧的通知字典
                lock (listenArgEntrance)
                {
                    liveNotifyDict = tempLiveNotiDict;
                }
            }
            catch (Exception ex)
            {
                HowLog.LogError("[Error]从文件中获取注册的群信息失败。详情:" + ex);
            }
        }


        //============类型=============

        public struct RegistrantInfo
        {
            public EntityType type;
            public long id;
            public long[] roomIds;
            public static RegistrantInfo FromHSO(HowSimpleObject hso, HowServerLiveListener.AnchorInfo[] aInfos = null)
            {
                if (aInfos == null)
                {
                    aInfos = new HowServerLiveListener.AnchorInfo[0];
                }
                RegistrantInfo rInfo = new RegistrantInfo();
                if (hso["type"].Value == "group")
                {
                    rInfo.type = EntityType.Group;
                }
                else if (hso["type"].Value == "friend")
                {
                    rInfo.type = EntityType.Friend;
                }
                else
                {
                    throw new Exception("读取到的数据无法映射到枚举。待解析的文本:{" + hso["type"].ToString() + "}");
                }
                rInfo.id = Convert.ToInt64(hso["id"].Value);
                var roomAry = hso["rooms"] as HowSimpleArrayItem;
                rInfo.roomIds = new long[roomAry.Count];
                //遍历群注册的每个主播
                for (int i = 0; i < roomAry.Count; i++)
                {
                    if (roomAry[i].StartsWith('@'))
                    {
                        long? id = null;
                        var crId = roomAry[i].Substring(1);
                        //从文件查询主播房间号
                        for (int i2 = 0; i2 < aInfos.Length; i2++)
                        {
                            if (aInfos[i2].crystalId == crId)
                            {
                                //查到的房间号
                                id = aInfos[i2].roomId;
                                break;
                            }
                        }
                        if (!id.HasValue) throw new Exception("查询的crystal id(" + crId + ")不在收听的主播列表。");
                        //将查到的房间号进行赋值
                        rInfo.roomIds[i] = id.Value;
                    }
                    else
                    {
                        rInfo.roomIds[i] = Convert.ToInt64(roomAry[i]);
                    }
                }
                return rInfo;
            }
        }

        public struct EntityInfo
        {
            public EntityType type;
            public long id;
            public EntityInfo(EntityType type_, long id_)
            {
                type = type_;
                id = id_;
            }
        }

        //Note: LiveStatue枚举
        // 1==直播中 0=Sleeping 2=Rolling

        /// <summary>
        /// 直播状态(内容播放状态)
        /// </summary>
        public enum PlayingState
        {
            /// <summary>
            /// 初始状态,未知
            /// </summary>
            Unknow = -1,
            /// <summary>
            /// 未直播
            /// </summary>
            Sleeping = 0,
            /// <summary>
            /// 直播
            /// </summary>
            Living = 1,
            /// <summary>
            /// 轮播
            /// </summary>
            Rolling = 2
        }



    }

    public static class StaticHelp_CPLiveNotification
    {
        public static bool IsNoLiving(this CPLiveNotification.PlayingState state)
        {
            return state == CPLiveNotification.PlayingState.Sleeping
                || state == CPLiveNotification.PlayingState.Rolling;
        }
    }
}
