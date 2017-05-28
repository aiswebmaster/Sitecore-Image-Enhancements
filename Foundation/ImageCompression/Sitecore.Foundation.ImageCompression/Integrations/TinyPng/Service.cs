using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Web;

namespace Sitecore.Foundation.ImageCompression.Integrations.TinyPng
{
    public class Service : IService
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

        public byte[] OptimizeImage(byte[] imageBytes)
        {
            WebClient client = new WebClient();
            string auth = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format($"api:{Key}")));

            client.Headers.Add(HttpRequestHeader.Authorization, $"Basic{auth}");

            try
            {
                client.UploadData($"{Url}/shrink", imageBytes);

                var outputBytes = client.DownloadData(client.ResponseHeaders["Location"]);

                Log.Info("TinyPng - Image Compressed Successfully", this);

                return outputBytes;
            }
            catch (Exception ex)
            {
                Log.Error("TinyPng - Error While Optimizing Image Upload", ex, this);
            }

            return null;
        }
    }
}