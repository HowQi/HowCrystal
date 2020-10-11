using HowCrystal.LoTool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace HowCrystal
{
    public partial class FormMain : Form
    {
        const int LogMaxStringLen = 4096;

        EhowCore core = new EhowCore();

        public FormMain()
        {
            InitializeComponent();
            this.Load += FormMain_Load;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            HowLog.EvtLog += HowLog_EvtLog;
        }

        void HowLog_EvtLog(string s)
        {
            var mthod = new MethodInvoker(() =>
            {
                var tstr = s + rtxtLog.Text;
                if (tstr.Length > LogMaxStringLen * 1.25)
                {
                    tstr = tstr.Substring(0, LogMaxStringLen);
                }
                rtxtLog.Text = tstr;
            });
            if (this.InvokeRequired)
            {
                this.Invoke(mthod);
            }
            else
            {
                mthod.Invoke();
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            core.SwitchOn();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            core.SwitchOff();
        }

        private void btnUpdateData_Click(object sender, EventArgs e)
        {
            try
            {
                core.liveNotificationPlg.RefreshListenTargetCfg();
                core.liveNotificationPlg.RefreshNotifyTargetCfg();
            }
            catch (Exception ex)
            {
                HowLog.LogError("刷新数据时遇到问题:" + ex.ToString());
            }
        }
    }
}
