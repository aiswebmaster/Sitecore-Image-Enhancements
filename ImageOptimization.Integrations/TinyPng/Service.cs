using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ImageOptimization.Integrations.TinyPng
{
    public class Service
    {
        public string Key
        {
            get
            {
                return Sitecore.Configuration.Settings.GetSetting("TinyPng.Service.Key");
            }
        }

        public string Url
        {
            get
            {
                return Sitecore.Configuration.Settings.GetSetting("TinyPng.Service.Url");
            }
        }

        public byte[] OptimizeImage(byte[] inputBytes)
        {
            WebClient client = new WebClient();
            string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format("api:{0}", Key)));
            client.Headers.Add(HttpRequestHeader.Authorization, "Basic " + auth);

            try
            {
                client.UploadData(Url + "/shrink", inputBytes);

                var outputBytes = client.DownloadData(client.ResponseHeaders["Location"]);

                Log.Info("Image Compressed Successfully", this);

                return outputBytes;
            }
            catch (Exception ex)
            {
                Log.Error("Error While Optimizing File Upload", ex, this);
            }

            return null;
        }
    }
}
