using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMKR.Shared.Logging.Configuration
{
    public class LoggingContext
    {
        public string ServiceName { get; set; } = string.Empty;
        public string TargetTable { get; set; } = string.Empty;

        public Func<HttpContext, string?>? ClientIdResolver { get; set; }
        public Func<HttpContext, string>? CorrelationIdResolver { get; set; }
    }
}
