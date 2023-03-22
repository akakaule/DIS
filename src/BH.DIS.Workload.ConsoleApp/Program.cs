using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using BH.DIS.Core.Logging;
using BH.DIS.Endpoints.Demo;
using BH.DIS.Events.Demo;
using BH.DIS.SDK;
using BH.DIS.SDK.Logging;
using Serilog;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.ServiceBus;

namespace BH.DIS.Workload.ConsoleApp
{
    class Program
    {
        private static int defaultSessionCount = 1;
        private static double defaultMessagePerSession = 2.0;
        private static int defaultfailCount = 0;

        private bool timeOut = false;

        static async Task Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", false, true)
               .Build();
            // Read and set Configs for the app
            string serviceBusConnectionString =configuration.GetValue<string>("AzureWebJobsServiceBus");
            string publisherId = new Alice().Id;
            string bobSubscriberId = new Bob().Id;
            string charlieSubscriberId = new Charlie().Id;

            // Read params or use defaults
            int sessionCount = args.Length > 0 ? (int.TryParse(args[0], out sessionCount) ? sessionCount : defaultSessionCount) : defaultSessionCount;
            double messagePerSession = args.Length > 1 ? (double.TryParse(args[1], out messagePerSession) ? messagePerSession : defaultMessagePerSession) : defaultMessagePerSession;
            int failCount = args.Length > 2 ? (int.TryParse(args[2], out failCount) ? failCount : defaultfailCount) : defaultfailCount;
            int timeoutMinutesMin = args.Length > 3 ?  (int.TryParse(args[3], out timeoutMinutesMin) ? timeoutMinutesMin : -1) : -1;
            int timeoutMinutesMax = args.Length > 4 ?  (int.TryParse(args[4], out timeoutMinutesMax) ? timeoutMinutesMin : -1) : -1;

            // Create Timer
            Random random = new Random();

            if (timeoutMinutesMin!=-1 && timeoutMinutesMax != -1) {

                System.Timers.Timer timer = new System.Timers.Timer(1000 * 60 * random.Next((int)timeoutMinutesMin, (int)timeoutMinutesMax));
                timer.Elapsed += OnTimedEvent;
                timer.Enabled = true;
                timer.Start();
            }

            // Create base logger.
            Serilog.ILogger consoleLogger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            // Setup publisher client            
            ServiceBusClient client = new ServiceBusClient(serviceBusConnectionString);
            IPublisherClient publisherClient = new PublisherClient(client, publisherId);

            // Setup and Register subscriber clients

            var cts = new CancellationTokenSource();
            Console.WriteLine("Press any key to exit");
            Console.CancelKeyPress += (a, o) =>
            {
                Console.WriteLine("Closing Service Bus connection...");
                cts.Cancel();
            };

            // Bob
            int expectedCount = (int)(sessionCount * messagePerSession) * 2;
            var bobCounter = new Counter(0, expectedCount);
            ISubscriberClient bobSubscriberClient = CreateSubscriberClient(client, bobSubscriberId, consoleLogger, bobSubscriberId, bobCounter, true);

            // Charlie
            int charlieExpectedCount = (int)(sessionCount * messagePerSession);
            var charlieCounter = new Counter(0, charlieExpectedCount);
            ISubscriberClient charlieSubscriberClient = CreateSubscriberClient(client, charlieSubscriberId, consoleLogger, charlieSubscriberId, charlieCounter, false);

            // Send events with publisher
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string testIdentifier = new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray());

            for (int i = 0; i < sessionCount; i++)
            {
                var failingNumbers = Enumerable.Range(0, (int)messagePerSession).OrderBy(x => random.Next()).Take(failCount).ToList();
                //failingNumbers = new List<int>() { 0 };
                for (int j = 0; j < messagePerSession; j++)
                {
                    // Publish event.
                    await publisherClient.Publish(new AliceSaidHello() { Counter = i, FakeSessionId = $"Alice-{i}-{testIdentifier}" });
                    await publisherClient.Publish(new AliceSaidBonjour() { Counter = i, FakeSessionId = $"Alice-{i}-{testIdentifier}" , ProvokeError = failingNumbers.Contains(j) });
                }
            }

            Task taskBobSubscriber = Task.Run(async () =>
            {
                await HandleMessages(client, bobSubscriberClient, bobSubscriberId, bobCounter, cts);
            });

            Task taskCharlieSubscriber = Task.Run(async () =>
            {
                await HandleMessages(client, charlieSubscriberClient, charlieSubscriberId, charlieCounter, cts);
            });

            // Start awaiters to check when we are done.
            Task taskWaiterB = Task.Run(() =>
            {
                while (!bobCounter.IsDone())
                {
                    Thread.Sleep(2000);
                }
            });
            Task taskWaiterC = Task.Run(() =>
            {
                while (!charlieCounter.IsDone())
                {
                    Thread.Sleep(2000);
                }
            });
            Task.Run(() =>
            {
                while (true)
                {
                    Console.WriteLine($"CounterB: {bobCounter.GetCount()} - IsDone = {bobCounter.IsDone()}");
                    Console.WriteLine($"CounterC: {charlieCounter.GetCount()}  - IsDone = {charlieCounter.IsDone()}");
                    Thread.Sleep(2000);
                }
            });
            await Task.WhenAll(taskWaiterB, taskWaiterC);

            await Task.Delay(2000);
            Console.WriteLine("Done with all messages---------------------");
            Console.WriteLine("Status for run was:");
            Console.WriteLine($"{sessionCount} session(s) was used, with {messagePerSession * 2} message(s) per session");
            Console.WriteLine($"Failcount set to: {failCount} failing message(s) per session");
            Console.WriteLine($"Bob completed the messages in {bobCounter.GetElapsed().TotalSeconds} second(s)");
            Console.WriteLine($"Charlie completed the messages in {charlieCounter.GetElapsed().TotalSeconds} second(s)");
        }

        private static async Task HandleMessages(ServiceBusClient client, ISubscriberClient subscriberClient, string endpoint, Counter counter, CancellationTokenSource cts)
        {
            do
            {
                var receiver = await client.AcceptNextSessionAsync(endpoint, endpoint);

                ServiceBusReceivedMessage message;
                do
                {
                    message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1), cancellationToken: cts.Token);
                    if (message != null)
                    {
                        await subscriberClient.Handle(message, receiver);
                        counter.Inc();
                    }
                }
                while (message != null);
                await receiver.CloseAsync();
            }
            while (true);
        }
      

        private static ISubscriberClient CreateSubscriberClient(ServiceBusClient client, string endpoint, Serilog.ILogger baseLogger, string subscriberId, Counter counter, bool isBob)
        {
            // Create logger provider from base logger.
            ILoggerProvider loggerProvider = new LoggerProvider(baseLogger);

            // Create subscriber client (from BH.DIS.SDK).
            ISubscriberClient subscriberClient = new SubscriberClient(client, endpoint, loggerProvider);


            // Register event handler factory for event type AliceSaidHello.
            subscriberClient.RegisterHandler(() => new AliceSaidHelloHandler(counter));
            if (isBob)
            {
                subscriberClient.RegisterHandler(() => new AliceSaidBonjourHandler(counter));
            }

            return subscriberClient;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine($"-----------------Timeout------------------");
            System.Environment.Exit(1);
        }

    }
}
