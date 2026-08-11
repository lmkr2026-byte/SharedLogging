using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMKR.Shared.Logging.Configuration
{
    public class SeqOptions
    {
        public const string SectionName = "Seq";
        public string Url { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}
