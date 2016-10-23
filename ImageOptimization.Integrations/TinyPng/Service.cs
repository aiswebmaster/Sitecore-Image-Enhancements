using System;
using System.Collections.Generic;
using System.Linq;
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
                return Sitecore.Configuration.Settings.GetSetting("");
            }
        }

        public bool OptimizeImage()
        {

        }
    }
}
