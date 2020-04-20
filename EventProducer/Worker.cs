using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Newtonsoft.Json;
using System.Text;

namespace EventProducer
{


    public class Worker : BackgroundService
    {
        // Create a SEND only key under "Shared acces policies" on the root hub namespace
        private string connectionString;
        private string eventHubName = "hub1"; //<-- One of the many hubs created under the root namespace
        private string deviceId = "1001";

        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            connectionString = Environment.GetEnvironmentVariable("ConnectionString");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Create a producer client that you can use to send events to an event hub
                await using (var producerClient = new EventHubProducerClient(connectionString, eventHubName))
                {
                    // Create a batch of events
                    var options = new CreateBatchOptions();
                    //options.PartitionId = "3";//<--- Event Hubs will ensure this batch goes to the same partition (You are specifying the exact id)
                    //options.PartitionKey = "123";//<--- Event Hubs will ensure this batch goes to the same partition based on hash (EH will decide) RECOMMENDED BY MICROSOFT.
                    using EventDataBatch eventBatch = await producerClient.CreateBatchAsync(options);

                    #region Build telemetry payload for this cycle and send to Event Hub

                    Random rnd = new Random();

                    for(int i = 0; i < 20; i++)
                    {
                        var temp = rnd.Next(0, 120); //<-- 0-120 degrees

                        var telemetryDataPoint = new
                        {
                            deviceId = deviceId,
                            temperature = temp,
                            order = i
                        };

                        // Add events to the batch. An event is represented by a collection of bytes and metadata
                        eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telemetryDataPoint))));

                        Console.WriteLine($"Temp: {temp}");
                    }

                    #endregion

                    // Use the producer client to send the batch of events to the event hub
                    await producerClient.SendAsync(eventBatch);

                    Console.WriteLine("Batch of events published");

                }

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(30000, stoppingToken);
            }
        }
    }
}
