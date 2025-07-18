using Microsoft.Extensions.Options;
using ieHRMS.Core.Config;
using ieHRMS.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ieHRMS.Core.Repository
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<Func<CancellationToken, Task>> _queue;
        private readonly int _maxConcurrency;

        public BackgroundTaskQueue(IOptions<BackgroundQueueSettings> options)
        {
            _queue = Channel.CreateUnbounded<Func<CancellationToken, Task>>();
            _maxConcurrency = options.Value.MaxConcurrency;
        }

        public async Task QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null)
                throw new ArgumentNullException(nameof(workItem));

            await _queue.Writer.WriteAsync(workItem);
        }

        public async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            var semaphore = new SemaphoreSlim(_maxConcurrency);

            var runningTasks = new List<Task>();

            await foreach (var workItem in _queue.Reader.ReadAllAsync(cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await workItem(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error executing background task: {ex.Message}");
                        // Optional: log to a real logger
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);

                runningTasks.Add(task);
            }

            await Task.WhenAll(runningTasks);
        }
    }
}
