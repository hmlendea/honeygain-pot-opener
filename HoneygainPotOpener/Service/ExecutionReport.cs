using System.Collections.Generic;
using System.Linq;

using NuciLog.Core;

namespace HoneygainPotOpener.Service
{
    public sealed class ExecutionReport
    {
        public IEnumerable<Operation> FailedOperations { get; set; } = [];

        public bool IsSuccessful => !FailedOperations.Any();
    }
}
