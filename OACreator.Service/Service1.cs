using System;
using System.ServiceProcess;
using System.Timers;
using System.Data;

namespace OACreator.Service
{
    public partial class Service1 : ServiceBase
    {
        Timer t;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            DateTime nowTime = DateTime.Now;
            DateTime scheduledTime = new DateTime(nowTime.Year, nowTime.Month, nowTime.Day, 18, 0, 0, 0);
            if (nowTime > scheduledTime)
                scheduledTime = scheduledTime.AddDays(1);

            t = new Timer
            {
                Interval = (scheduledTime - DateTime.Now).TotalMilliseconds
            };

            t.Elapsed += (sender, e) => OnTimer(sender, e, args);
            t.Start();
        }

        protected void OnTimer(object sender, ElapsedEventArgs e, string[] args)
        {
            t.Enabled = false;
            t.Interval = 24 * 60 * 60 * 1000;
            t.Enabled = true;

            OACreator.Application OACreator = new OACreator.Application();

            DataTable dt = OACreator.GetDeNIdWithNullStatusList();
            foreach (DataRow row in dt.Rows)
            {
                try
                {
                    OACreator.Generate(Int32.Parse(row["DeN_DeNId"].ToString()));
                }
                catch (Exception) { }
            }
        }

        protected override void OnStop()
        {
        }
    }
}
