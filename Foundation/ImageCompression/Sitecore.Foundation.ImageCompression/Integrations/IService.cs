using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.Foundation.ImageCompression.Integrations
{
    public interface IService
    {
        byte[] OptimizeImage(byte[] imageBytes);
    }
}