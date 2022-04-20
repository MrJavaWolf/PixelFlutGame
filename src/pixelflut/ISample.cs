
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


    public interface ISample
    {
        string FullName { get; }
        string Description { get; }
        IReadOnlyCollection<string> ShortNames { get; }
        Task ExecuteAsync(CancellationToken token = default);
    }
