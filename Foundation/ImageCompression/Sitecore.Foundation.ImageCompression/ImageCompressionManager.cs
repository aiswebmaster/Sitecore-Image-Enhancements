using Sitecore.Common;
using Sitecore.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Foundation.ImageCompression
{
    public class ImageCompressionManager
    {
        private static readonly ProviderHelper<ImageCompressionProvider, ImageCompressionProviderCollection> Helper = new ProviderHelper<ImageCompressionProvider, ImageCompressionProviderCollection>("imageCompression");

        public static ImageCompressionProvider Provider
        {
            get
            {
                string currentValue = Switcher<string, string>.CurrentValue;
                if (string.IsNullOrEmpty(currentValue))
                    return ImageCompressionManager.Helper.Provider;
                ImageCompressionProvider provider = (ImageCompressionProvider)ImageCompressionManager.Helper.Providers[currentValue];
                return provider;
            }
        }

        public static ImageCompressionProviderCollection Providers
        {
            get
            {
                return ImageCompressionManager.Helper.Providers;
            }
        }

        public byte[] OptimizeImage(byte[] imageBytes)
        {
            return ImageCompressionManager.Provider.OptimizeImage(imageBytes);
        }
    }

}