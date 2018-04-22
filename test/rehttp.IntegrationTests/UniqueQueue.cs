using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;

namespace Rehttp.IntegrationTests
{
    internal class UniqueQueue : IDisposable
    {
        public CloudQueue Queue { get; set; }

        public CloudQueueClient QueueClient { get; set; }

        public UniqueQueue()
            : this(Guid.NewGuid().ToString())
        {
        }

        public UniqueQueue(string queueName)
        {
            var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            QueueClient = storageAccount.CreateCloudQueueClient();
            Queue = QueueClient.GetQueueReference(queueName);

            Queue.CreateAsync().Wait();
        }

        public void Dispose()
        {
            Queue.DeleteAsync().Wait();
        }
    }
}
