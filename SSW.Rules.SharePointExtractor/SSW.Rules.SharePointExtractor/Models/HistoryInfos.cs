using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Models
{
    public class HistoryInfos
    {
        public string file { get; set; }
        public DateTimeOffset lastUpdated { get; set; }
        public string lastUpdatedBy { get; set; }
        public string lastUpdatedByEmail { get; set; }
        public DateTimeOffset created { get; set; }
        public string createdBy { get; set; }
        public string createdByEmail { get; set; }
    }
}
