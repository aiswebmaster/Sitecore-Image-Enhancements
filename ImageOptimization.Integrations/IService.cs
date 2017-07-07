using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageOptimization.Integrations
{
    public interface IService
    {
        byte[] OptimizeImage(byte[] originalImage);
    }
}
