using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Core.Config
{
    public class BackgroundQueueSettings
    {
        public int MaxConcurrency { get; set; } = 10; // default if not set in config
    }
     
}
