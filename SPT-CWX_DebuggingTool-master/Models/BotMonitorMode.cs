using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CWX_DebuggingTool.Models
{
    public enum BotMonitorMode
    {
        [Description("Disabled")]
        None = 0,
        [Description("Only Total")]
        Total = 1,
        [Description("Total and Per Zone Total")]
        PerZoneTotal = 2,
        [Description("Total, Per Zone Total, and Bot List")]
        FullList = 3
    }
}
