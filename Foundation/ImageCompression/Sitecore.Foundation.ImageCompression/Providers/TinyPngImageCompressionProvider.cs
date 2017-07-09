using Sitecore.Diagnostics;
using System;
using System.Net;
using System.Text;

namespace Sitecore.Foundation.ImageCompression.Providers
{
    public class TinyPngImageCompressionProvider : ImageCompressionProvider
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

        public override byte[] OptimizeImage(byte[] imageBytes)
        {
            WebClient client = new WebClient();
            string auth = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Format($"api:{Key}")));

            client.Headers.Add(HttpRequestHeader.Authorization, $"Basic {auth}");

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