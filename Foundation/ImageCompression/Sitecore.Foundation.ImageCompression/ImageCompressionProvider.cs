using Sitecore.Common;
using System;
using System.Collections.Generic;
using System.Configuration.Provider;
using System.Linq;
using System.Web;
using System.Collections.Specialized;

namespace Sitecore.Foundation.ImageCompression
{
    public abstract class ImageCompressionProvider : ProviderBase
    {
        public ImageCompressionProvider()
        {

        }

        public abstract byte[] OptimizeImage(byte[] imageBytes);

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);
        }
    }
}