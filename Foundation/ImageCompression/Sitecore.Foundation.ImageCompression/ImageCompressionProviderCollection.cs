using System;
using System.Collections.Generic;
using System.Configuration.Provider;
using System.Linq;
using System.Web;

namespace Sitecore.Foundation.ImageCompression
{
    public class ImageCompressionProviderCollection : ProviderCollection
    {
        public override void Add(ProviderBase provider)
        {
            base.Add(provider);
        }
    }
}