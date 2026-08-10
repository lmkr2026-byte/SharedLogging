using LMKR.Shared.Logging.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMKR.Shared.Logging.Extensions
{
    public static class EndpointExtensions
    {
        public static bool IsApiLoggingDisabled(this Endpoint? endpoint)
        {
            var actionDescriptor =
                endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();

            if (actionDescriptor == null)
                return false;

            return actionDescriptor.MethodInfo
                       .GetCustomAttributes(typeof(DisableApiLoggingAttribute), true)
                       .Any()
                   ||
                   actionDescriptor.ControllerTypeInfo
                       .GetCustomAttributes(typeof(DisableApiLoggingAttribute), true)
                       .Any();
        }
    }
}
