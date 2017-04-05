using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace LibHeatmap
{
    public class HeatMap
    {
        public struct IPResponseData
        {
            public string ip;
            public string country_code;
            public string country_name;
            public string region_code;
            public string region_name;
            public string city;
            public string zip_code;
            public string time_zone;
            public double latitude;
            public double longitude;
            public int metro_code;
        }

        public class HeatMapIP : System.Net.IPAddress
        {
            private bool m_IsDataReceived;
            private IPResponseData m_ResponseData;

            public IPResponseData IPData
            {
                get
                {
                    return m_ResponseData;
                }
            }

            public bool IsReady
            {
                get
                {
                    return m_IsDataReceived;
                }
            }

            public void OnIPDataReceived(object sender, System.Net.DownloadStringCompletedEventArgs e)
            {
                if (e.Error != null)
                {
                    throw new ArgumentException();
                }

                // Deserialize json response
                m_ResponseData = Newtonsoft.Json.JsonConvert.DeserializeObject<IPResponseData>(e.Result);
                m_IsDataReceived = true;
            }

            // Constructors
            public HeatMapIP(byte[] address)
    : base(address)
            {
                m_IsDataReceived = false;
            }
            public HeatMapIP(Int64 address)
    : base(address)
            {
                m_IsDataReceived = false;
            }
            public HeatMapIP(byte[] address, Int64 scopeid)
    : base(address, scopeid)
            {
                m_IsDataReceived = false;
            }
        }


        private string m_GoogleAPIKey;
        private string m_FreeGeoIPUrl;
        private string m_Template;

        private System.Drawing.Size m_Size;

        private List<HeatMapIP> m_IPs;
        private List<Func<System.Drawing.Image, bool>> m_Callbacks;

        public HeatMap(string GoogleAPIKey, System.Drawing.Size Size, string Template, string FreeGeoIPUrl = "https://freegeoip.net/")
        {
            this.m_IPs = new List<HeatMapIP>();
            this.m_Callbacks = new List<Func<System.Drawing.Image, bool>>();

            this.m_GoogleAPIKey = GoogleAPIKey;
            this.m_Size = Size;
            this.m_Template = Template;
            this.m_FreeGeoIPUrl = FreeGeoIPUrl;

            if (!this.m_FreeGeoIPUrl.EndsWith("/"))
            {
                this.m_FreeGeoIPUrl += "/";
            }
        }

        public void AddIP(HeatMapIP IP)
        {
            // Adds a specified IP
            m_IPs.Add(IP);
        }

        public void RemoveIP(HeatMapIP IP)
        {
            // Removes a specified IP
            m_IPs.Remove(IP);
        }

        public void ClearIPs()
        {
            // Clears all IPs
            m_IPs.Clear();
        }

        public void RegisterCallback(Func<System.Drawing.Image, bool> Callback)
        {
            // Adds a callback to the callback list
            m_Callbacks.Add(Callback);
        }

        private bool DidFetchIPs()
        {
            foreach (var IP in m_IPs)
            {
                // If IP is not ready, return false.
                if (!IP.IsReady)
                {
                    return false;
                }
            }

            return true;
        }

        public string GenerateHTML()
        {
            string HtmlOutput = this.m_Template;

            // Get country code for every IP
            foreach (var IP in m_IPs)
            {
                var webclient = new System.Net.WebClient();
                webclient.DownloadStringCompleted += new System.Net.DownloadStringCompletedEventHandler(IP.OnIPDataReceived);
                webclient.DownloadStringAsync(new Uri(this.m_FreeGeoIPUrl += "json/" + IP.ToString()));
            }

            // Wait until all IPs are fetched
            while (!this.DidFetchIPs())
            {
                System.Threading.Thread.Sleep(100);
            }

            // Generate HTML page
            HtmlOutput = HtmlOutput.Replace("{$API_KEY}", this.m_GoogleAPIKey);

            // Add IPs to HTML page
            string IPDefBuffer = "var heatmapData = [";
            foreach (var IP in m_IPs)
            {
                // Format IP data
                string CurIPDef = String.Format("new google.maps.LatLng({0}, {1})", IP.IPData.latitude, IP.IPData.longitude);

                // Check if there is a next IP in array
                if (m_IPs.IndexOf(IP) != m_IPs.Count - 1)
                {
                    CurIPDef += ",";
                }

                // Add IP into buffer
                IPDefBuffer += CurIPDef + "\n";
            }
            IPDefBuffer += "];";

            // Add IP Data into template
            HtmlOutput = HtmlOutput.Replace("{$IP_DATA}", IPDefBuffer);

            return HtmlOutput;
        }

        public System.Drawing.Image Generate()
        {
            string HtmlOutput = this.GenerateHTML();

            // Generate bitmap
            System.Drawing.Image image = HtmlRender.RenderToImageGdiPlus(HtmlOutput, this.m_Size);

            // Run callbacks
            foreach (var cb in this.m_Callbacks)
            {
                // run callback
                cb(image);
            }

            // Return image
            return image;
        }

        private void GenerateAsyncInternal()
        {
            this.Generate();
        }

        public void GenerateAsync()
        {
            // Start the heatmap thread
            var HeatMapThread = new System.Threading.Thread(this.GenerateAsyncInternal);
            HeatMapThread.Start();
        }
    }
}
