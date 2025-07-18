using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ieHRMS.Core.Interface
{
    public interface IBackgroundTaskQueue
    {
        Task QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);
        Task ProcessQueueAsync(CancellationToken cancellationToken);
    }
}
