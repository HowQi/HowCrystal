using log4net;
using log4net.Config;
using log4net.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HowCrystal.LoTool
{
    public class HowLog
    {
        static HowLog()
        {
            instance = new HowLog();
        }

        private static HowLog instance = null;
        public static HowLog Instance
        {
            get
            {
                return instance;
            }
        }

        private ILog log;

        private HowLog()
        {
            ILoggerRepository repository = LogManager.CreateRepository("MiraiCSharpEhowRep");
            XmlConfigurator.Configure(repository, new FileInfo("LoTool\\LogCfg.config"));
            log = LogManager.GetLogger(repository.Name, "HowLog");
        }


        public static void LogDebug(string msg)
        {
            Instance.log.Debug(msg);
            TriggetEvtLog(DEBUG, msg);
        }
        public static void LogInfo(string msg)
        {
            Instance.log.Info(msg);
            TriggetEvtLog(INFO, msg);
        }
        public static void LogWarn(string msg)
        {
            Instance.log.Warn(msg);
            TriggetEvtLog(WARN, msg);
        }
        public static void LogError(string msg)
        {
            Instance.log.Error(msg);
            TriggetEvtLog(ERROR, msg);
        }
        public static void LogFatal(string msg)
        {
            Instance.log.Fatal(msg);
            TriggetEvtLog(FATAL, msg);
        }


        const string DEBUG = "debug";
        const string INFO = "info";
        const string WARN = "warn";
        const string ERROR = "error";
        const string FATAL = "fatal";

        public static event Action<string> EvtLog;
        private static void TriggetEvtLog(string ty, string s)
        {
            try
            {
                EvtLog?.Invoke($"[{ty}]{s}\r\n");
            }
            catch (Exception)
            {
            }

        }

    }
}
