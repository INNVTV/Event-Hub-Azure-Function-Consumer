# Event-Hub-Azure-Function-Consumer
EventHubs with Azure Function

## Architecture
![Architecture](https://github.com/INNVTV/IoT-Simulated-Device/blob/master/images/architecture.jpg)

## Offsetts/Checkpointing
Azure Functions using EventHub triggers leverage the underlying storage account to store checkpoint data. Unlilke the **Azure.Messaging.EventHubs.Processor** nuget package the data is stored as a json string in the blob file for each partition (0-32). In the nuget package the SDK stores the checkpoint as metadata on the file.

![Checkpoint](https://github.com/INNVTV/IoT-Simulated-Device/blob/master/images/checkpoint.jpg)


## Scaling
Each instance of an event triggered function is backed by a single EventProcessorHost instance. The trigger (powered by Event Hubs) ensures that only one EventProcessorHost instance can get a lease on a given partition.

For example, consider an Event Hub as follows:

 * 10 partitions
 * 1,000 events distributed evenly across all partitions, with 100 messages in each partition
 
When your function is first enabled, there is only one instance of the function. Let's call the first function instance Function_0. The Function_0 function has a single instance of EventProcessorHost that holds a lease on all ten partitions. This instance is reading events from partitions 0-9. From this point forward, one of the following happens:

 * **New function instances are not needed:** Function_0 is able to process all 1,000 events before the Functions scaling logic take effect. In this case, all 1,000 messages are processed by Function_0.

 * **An additional function instance is added:** If the Functions scaling logic determines that Function_0 has more messages than it can process, a new function app instance (Function_1) is created. This new function also has an associated instance of EventProcessorHost. As the underlying Event Hubs detect that a new host instance is trying read messages, it load balances the partitions across the host instances. For example, partitions 0-4 may be assigned to Function_0 and partitions 5-9 to Function_1.

 * **N more function instances are added:** If the Functions scaling logic determines that both Function_0 and Function_1 have more messages than they can process, new Functions_N function app instances are created. Apps are created to the point where N is greater than the number of event hub partitions. In our example, Event Hubs again load balances the partitions, in this case across the instances Function_0...Functions_9.

As scaling occurs, N instances is a number greater than the number of event hub partitions. This pattern is used to ensure EventProcessorHost instances are available to obtain locks on partitions as they become available from other instances. You are only charged for the resources used when the function instance executes. In other words, you are not charged for this over-provisioning.

When all function execution completes (with or without errors), checkpoints are added to the associated storage account. When check-pointing succeeds, all 1,000 messages are never retrieved again.