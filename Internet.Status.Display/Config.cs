using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Internet.Status.Display
{
    public class Config
    {
        private static string filename = "Configuration.config";

        private static ConfigHolder holder = null;
        private static ConfigHolder Holder
        {
            get
            {
                if (holder == null) LoadConfig();
                return holder;
            }
            set
            {
                holder = value;
            }
        }
        
        [Serializable]
        public class ConfigHolder
        {
            public ScreenCorner corner = ScreenCorner.BottomRight;
            public int screenEdgePadding = 10;
            public List<string> hosts = new List<string>();
            public int pingInterval = 1000;
            public int pingGreen = 100;
            public int pingYellow = 700;
            public double overlayOpacityWhilePermanent = 0.6;
            public bool useLockMode = false;
        }

        #region Public Fields
        public static ScreenCorner Corner
        {
            get
            {
                return Holder.corner;
            }
            set
            {
                Holder.corner = value;
            }
        }
        public static int ScreenEdgePadding
        {
            get
            {
                return Holder.screenEdgePadding;
            }
            set
            {
                Holder.screenEdgePadding = value;
            }
        }
        public static List<string> Hosts
        {
            get
            {
                return Holder.hosts;
            }
            set
            {
                Holder.hosts = value;
            }
        }
        public static int PingInterval
        {
            get
            {
                return Holder.pingInterval;
            }
            set
            {
                Holder.pingInterval = value;
            }
        }
        public static int PingGreen
        {
            get
            {
                return Holder.pingGreen;
            }
            set
            {
                Holder.pingGreen = value;
            }
        }
        public static int PingYellow
        {
            get
            {
                return Holder.pingYellow;
            }
            set
            {
                Holder.pingYellow = value;
            }
        }
        public static double OverlayOpacityWhilePermanent
        {
            get
            {
                return Holder.overlayOpacityWhilePermanent;
            }
            set
            {
                Holder.overlayOpacityWhilePermanent = value;
            }
        }
        public static bool UseLockMode
        {
            get
            {
                return Holder.useLockMode;
            }
            set
            {
                Holder.useLockMode = value;
            }
        }
        #endregion

        #region Internal Methods
        public static void LoadConfig()
        {
            try
            {
                using (FileStream f = new FileStream(filename, FileMode.OpenOrCreate))
                {
                    holder = (ConfigHolder) new XmlSerializer(typeof(ConfigHolder)).Deserialize(f);
                }
            }
            catch (Exception) { holder = new ConfigHolder(); }
        }
        public static void SaveConfig()
        {
            using (FileStream f = new FileStream(filename, FileMode.OpenOrCreate))
            {
                new XmlSerializer(typeof(ConfigHolder)).Serialize(f, holder);
            }
        }
        #endregion
    }
}
