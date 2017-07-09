using Sitecore.Common;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Foundation.ImageCompression
{
    public class ImageCompressionSwitcher : ProviderSwitcher
    {
        public ImageCompressionSwitcher(string providerName)
            : base(providerName)
        {
            Assert.ArgumentNotNull((object)providerName, "providerName");
        }
    }
}