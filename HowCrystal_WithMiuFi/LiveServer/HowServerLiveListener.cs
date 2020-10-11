using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using HowSimpleObjectTool;
using HowCrystal.LoTool;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace HowCrystal
{
    public class HowServerLiveListener : IDisposable
    {
        private AnchorInfo[] infos = new AnchorInfo[0];
        public AnchorInfo[] Infos { get => infos.Clone() as AnchorInfo[]; }

        private bool keepRun = true;

        private bool running = false;

        private Thread thd;

        /// <summary>
        /// 观测间隔
        /// </summary>
        private readonly int waitGapInSecond = 60;

        public bool Running
        {
            get
            {
                lock (stateEntrance)
                {
                    return running;
                }
            }
        }


        object stateEntrance = new object();

        public HowServerLiveListener()
        {
            ;
        }


        public void StartListener()
        {
            lock (stateEntrance)
            {
                StartListener_Raw();
            }
        }

        private void StartListener_Raw()
        {
            if (running) throw new Exception("状态错误!现在线程正在执行,不能再开");
            running = true;
            thd = new Thread(ThdBehvListener);
            keepRun = true;
            thd.Start();
            HowLog.LogInfo("[How operate]发起启动直播收听服务操作");
        }

        public void StopListener()
        {
            lock (stateEntrance)
            {
                StopListener_Raw();
            }
        }

        public AnchorInfo GetAnchorInfo(long roomId)
        {
            var tinfo = infos;
            for (int i = 0; i < tinfo.Length; i++)
            {
                if (roomId == tinfo[i].roomId)
                {
                    return tinfo[i];
                }
            }
            throw new Exception("未找到主播信息");
        }

        private void StopListener_Raw()
        {
            keepRun = false;
            HowLog.LogInfo("[How operate]发起关闭直播收听服务操作");
        }

        public void ConfigurateListenTarget(AnchorInfo[] infos_)
        {
            infos = infos_;
        }

        /// <summary>
        /// 工作线程
        /// </summary>
        private void ThdBehvListener()
        {
            HowLog.LogInfo("[How State]直播收听线程启动");
            bool aborted = false;

            try
            {
                while (keepRun)
                {
                    var curInfos = infos;
                    foreach (var aInfo in curInfos)
                    {
                        LiveRoomInfo roomInfo;
                        LiveRoomInfo_Simple roomInfo_Simple;
                        if (aInfo.enable)
                        {
                            GetLiveInfo(aInfo.roomId, out roomInfo, out roomInfo_Simple);
                            EvtLiveNotification?.Invoke(roomInfo, roomInfo_Simple);
                        }
                    }
                    var r = new Random();

                    int sleepRest = waitGapInSecond * 1000;

                    //判定当前是否是不经常直播的时间,比如超过1点的深夜,早于9点的早晨
                    var timeNow = DateTime.Now;
                    if (timeNow.Hour >= 1 && timeNow.Hour <= 9)
                    {
                        //如果是,采集速度变慢
                        sleepRest *= 5;
                    }

                    while (keepRun)
                    {
                        Thread.Sleep(500);
                        sleepRest -= 500;
                        if (sleepRest <= 0) break;
                    }
                }
            }
            catch (Exception ex)
            {
                aborted = true;
                HowLog.LogError("工作线程遇到问题,已试图结束之。问题详情:" + ex);
            }

            lock (stateEntrance)
            {
                running = false;
            }

            HowLog.LogInfo("[How State]直播收听线程结束。"
                + string.Format("线程指示状态:保持执行={0};存在异常退出否={1}", keepRun, aborted));
        }

        private void GetLiveInfo(long roomId, out LiveRoomInfo roomInfo, out LiveRoomInfo_Simple roomInfo_Simple)
        {
            string apiPath = "https://api.live.bilibili.com/room/v1/Room/get_info?id="
                            + roomId.ToString();

            HttpWebRequest request = WebRequest.Create(apiPath) as HttpWebRequest;
            request.Method = "GET";                            //请求方法
            request.ProtocolVersion = new Version(1, 1);   //Http/1.1版本
            request.UserAgent = InternetConst.UserAgent;
            var wresp = request.GetResponse();
            using (wresp)
            using (var stm = new StreamReader(wresp.GetResponseStream()))
            {
                var res = stm.ReadToEnd();

                bool isSimpleInfoSuccess = false;
                //bool isFullInfoSuccess = false;

                roomInfo = null;
                roomInfo_Simple = null;

                try
                {
                    var res2 = JsonConvert.DeserializeObject(res, typeof(LiveRoomInfo_Simple)) as LiveRoomInfo_Simple;
                    roomInfo_Simple = res2;
                    isSimpleInfoSuccess = true;
                }
                catch (Exception ex)
                {
                    HowLog.LogError("获取了直播间信息,但是尝试解析成最简信息时失败。收到的json:\r\n" + res
                        + "\r\n异常信息" + ex
                        );
                }

                if (isSimpleInfoSuccess)
                {
                    try
                    {
                        var res2 = JsonConvert.DeserializeObject(res, typeof(LiveRoomInfo)) as LiveRoomInfo;
                        roomInfo = res2;
                        //isFullInfoSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        HowLog.LogError("获取了直播间信息,尝试解析成完整信息时失败。收到的json:\r\n" + res
                            + "\r\n异常信息" + ex
                            );
                    }
                }
            }
        }

        public event FunLiveNotiAction EvtLiveNotification;

        #region === 实现Dispose内容 ===

        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                infos = null;
            }
            StopListener();
            disposed = true;
        }
        ~HowServerLiveListener()
        {
            Dispose(false);
        }

        #endregion

        public delegate void FunLiveNotiAction(LiveRoomInfo roomInfo, LiveRoomInfo_Simple roomInfo_Simple);

        /// <summary>
        /// 主播信息(文件存储)
        /// </summary>
        public struct AnchorInfo
        {
            public long roomId;
            public string crystalId;
            public string displayName;
            public bool enable;
            public AnchorInfo(long roomId_, string crystalId_, string displayName_, bool enable_)
            {
                roomId = roomId_;
                crystalId = crystalId_;
                displayName = displayName_;
                enable = enable_;
            }
            /// <summary>
            /// 从HowSimpleObject中映射数据。如果不能解析,会抛出异常。
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static AnchorInfo FromHSO(HowSimpleObject obj)
            {
                AnchorInfo info = new AnchorInfo();
                info.roomId = Convert.ToInt64(obj["room_id"].Value);
                info.crystalId = obj["crystal_id"].Value;
                info.displayName = obj["display_name"].Value;
                info.enable = Convert.ToInt32(obj["enable"].Value) == 1;
                return info;
            }
        }

        /// <summary>
        /// 直播间信息(网络数据)
        /// </summary>
        [Serializable]
        public class LiveRoomInfo
        {
            public int code;
            public string msg;
            public string message;
            public InnerData data;
            [Serializable]
            public class InnerData
            {
                //主播
                public long uid;
                //房间
                public long room_id;
                public string title;
                public string description;
                public string tags;
                public string background;
                public string user_cover;
                public string keyframe;
                //观众
                public long attention;
                public long online;
                //直播状态
                public long live_status;
                public string live_time;
                //分区信息
                public long old_area_id;
                public long area_id;
                public string area_name;
                public long parent_area_id;
                public string parent_area_name;
                //其他内容
                public bool is_portrait;
                public bool is_strict_room;
                public int is_anchor;
                public string room_silent_type;
                public int room_silent_level;
                public long room_silent_second;
                public string pendants;
                public string area_pendants;
                public string verify;
                public string up_session;
                public long allow_change_area_time;
                public long allow_upload_cover_time;
            }
        }

        /// <summary>
        /// 最简的直播间信息(网络数据)
        /// </summary>
        [Serializable]
        public class LiveRoomInfo_Simple
        {
            public int code;
            public string msg;
            public string message;
            public InnerData data;
            [Serializable]
            public class InnerData
            {
                //主播
                public long uid;
                //房间
                public long room_id;
                public string title;
                public string description;
                public string user_cover;
                public string keyframe;
                //直播状态
                public long live_status;
                public string live_time;
                //分区信息
                public long area_id;
                public string area_name;
            }
        }

    }
}
