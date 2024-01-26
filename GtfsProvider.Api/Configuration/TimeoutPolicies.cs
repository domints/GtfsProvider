using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Timeouts;

namespace GtfsProvider.Api.Configuration
{
    public static class TimeoutPolicies
    {
        public static RequestTimeoutPolicy DefaultPolicyConfiguration = new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromMilliseconds(5000)
        };

        public static RequestTimeoutPolicy DeparturePolicyConfiguration = new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromMilliseconds(20000)
        };

        public static string DeparturePolicy = nameof(DeparturePolicyConfiguration);
    }
}