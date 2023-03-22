using Azure.Messaging.ServiceBus;
using BH.DIS.Core.Logging;
using BH.DIS.Endpoints.Demo;
using BH.DIS.Events.Demo;
using BH.DIS.SDK;
using BH.DIS.SDK.Logging;
using BH.DIS.Workload.ConsoleApp;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace BH.DIS.Perfomance.ConsoleApp
{
    internal class Program
    {
        private static int defaultSessionCount = 1000;
        private static double defaultMessagePerSession = 4.0;
        private static int defaultfailCount = 0;
        static async Task Main(string[] args)
        {
            // Read and set Configs for the app
            IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", false, true)
               .Build();
            string serviceBusConnectionString = configuration.GetValue<string>("AzureWebJobsServiceBus");
            string publisherAlice = new Alice().Id;
            string bobSubscriberId = new Bob().Id;
            string charlieSubscriberId = new Charlie().Id;

            //string publisherKyb = new KyribaEndpoint().Id;
            //string navtreasury = new NavTreasuryEndpoint().Id;
            //string dbctreasury = new DbcTreasuryEndpoint().Id;

            //string crmPub = new CrmEndpoint().Id;
            //string dbcsub = new DbcEndpoint().Id;

            // Read params or use defaults
            int sessionCount = args.Length > 0 ? (int.TryParse(args[0], out sessionCount) ? sessionCount : defaultSessionCount) : defaultSessionCount;
            double messagePerSession = args.Length > 1 ? (double.TryParse(args[1], out messagePerSession) ? messagePerSession : defaultMessagePerSession) : defaultMessagePerSession;
            int failCount = args.Length > 2 ? (int.TryParse(args[2], out failCount) ? failCount : defaultfailCount) : defaultfailCount;
            int timeoutMinutesMin = args.Length > 3 ? (int.TryParse(args[3], out timeoutMinutesMin) ? timeoutMinutesMin : -1) : -1;
            int timeoutMinutesMax = args.Length > 4 ? (int.TryParse(args[4], out timeoutMinutesMax) ? timeoutMinutesMin : -1) : -1;

            // Create Timer
            Random random = new Random();

            if (timeoutMinutesMin != -1 && timeoutMinutesMax != -1)
            {

                System.Timers.Timer timer = new System.Timers.Timer(1000 * 60 * random.Next((int)timeoutMinutesMin, (int)timeoutMinutesMax));
                timer.Elapsed += OnTimedEvent;
                timer.Enabled = true;
                timer.Start();
            }

            // Create base logger.
            Serilog.ILogger consoleLogger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            ILoggerProvider loggerProvider = new LoggerProvider(consoleLogger);

            ServiceBusClient client = new ServiceBusClient(serviceBusConnectionString);

            // Setup publisher client            
            IPublisherClient publisherClient = new PublisherClient(client, publisherAlice);

            //IPublisherClient KybpublisherClient = new PublisherClient(client, publisherKyb);

            //IPublisherClient crmpublisherClient = new PublisherClient(client, crmPub);

            var cts = new CancellationTokenSource();
            Console.WriteLine("Press any key to exit");
            Console.CancelKeyPress += (a, o) =>
            {
                Console.WriteLine("Closing Service Bus connection...");
                cts.Cancel();
            };

            // Setup and Register subscriber clients

            // Bob
            int expectedCount = (int)(sessionCount * messagePerSession) * 2;
            var bobCounter = new Counter(0, expectedCount);
            ISubscriberClient bobSubscriberClient = new SubscriberClient(client, bobSubscriberId, loggerProvider);
            bobSubscriberClient.RegisterHandler(() => new AliceSaidHelloHandler(bobCounter));
            bobSubscriberClient.RegisterHandler(() => new AliceSaidBonjourHandler(bobCounter));

            // Charlie
            int charlieExpectedCount = (int)(sessionCount * messagePerSession);
            var charlieCounter = new Counter(0, charlieExpectedCount);
            ISubscriberClient charlieSubscriberClient = new SubscriberClient(client, charlieSubscriberId, loggerProvider);
            charlieSubscriberClient.RegisterHandler(() => new AliceSaidHelloHandler(charlieCounter));

            //// Nav-Treasury
            //int expectedNavCount = (int)(sessionCount * messagePerSession);
            //var navCounter = new Counter(0, expectedNavCount);
            //ISubscriberClient navSubscriberClient = new SubscriberClient(client, navtreasury, loggerProvider);
            //navSubscriberClient.RegisterHandler(() => new RefinitivSpotRatesReadyHandler(navCounter));

            //// DBC-Treasury
            //int expectedDBCCount = (int)(sessionCount * messagePerSession);
            //var DBCounter = new Counter(0, expectedDBCCount);
            //ISubscriberClient dbSubscriberClient = new SubscriberClient(client, dbctreasury, loggerProvider);
            //dbSubscriberClient.RegisterHandler(() => new RefinitivSpotRatesReadyHandler(DBCounter));

            //// DBC
            //int expecteddbc = (int)(sessionCount * messagePerSession);
            //var DBCCounter = new Counter(0, expecteddbc);
            //ISubscriberClient dbcSubscriberClient = new SubscriberClient(client, dbcsub, loggerProvider);
            //dbcSubscriberClient.RegisterHandler(() => new CustomerMDApprovedHandler(DBCCounter));

            // Send events with publisher
            Task publisherTask = Task.Run(async () =>
            {
                for (int i = 0; i < sessionCount; i++)
                {
                    var failingNumbers = Enumerable.Range(0, (int)messagePerSession).OrderBy(x => random.Next()).Take(failCount).ToList();
                    //failingNumbers = new List<int>() { 0 };
                    for (int j = 0; j < messagePerSession; j++)
                    {
                        // Publish event.
                        //await KybpublisherClient.Publish(new RefinitivSpotRatesReady() { Date = DateTime.Now });                        
                    }

                }
            });

            Task crmTask = Task.Run(async () =>
            {
                for (int i = 0; i < sessionCount; i++)
                {
                    var failingNumbers = Enumerable.Range(0, (int)messagePerSession).OrderBy(x => random.Next()).Take(failCount).ToList();
                    //failingNumbers = new List<int>() { 0 };
                    for (int j = 0; j < messagePerSession; j++)
                    {
                        // Publish event.
                        //await KybpublisherClient.Publish(new CustomerMDApproved() { CaseId = new Guid(), MDCaseId = "", Comment = $"Count: {i}", MDId = "" });
                    }

                }
            });

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
                    await publisherClient.Publish(new AliceSaidBonjour() { Counter = i, FakeSessionId = $"Alice-{i}-{testIdentifier}", ProvokeError = failingNumbers.Contains(j) });
                }

            }

            Task bob = Task.Run(async () =>
            {
                await HandleMessages(client, bobSubscriberClient, bobSubscriberId, bobCounter, cts);
            });

            Task charlie = Task.Run(async () =>
            {
                await HandleMessages(client, charlieSubscriberClient, charlieSubscriberId, charlieCounter, cts);
            });

            //Task nav = Task.Run(async () =>
            //{
            //    await HandleMessages(client, charlieSubscriberClient, navtreasury, navCounter, cts);
            //});

            //Task dbcTreasury = Task.Run(async () =>
            //{
            //    await HandleMessages(client, dbSubscriberClient, dbctreasury, DBCCounter, cts);
            //});

            //Task dbc = Task.Run(async () =>
            //{
            //    await HandleMessages(client, dbcSubscriberClient, dbcsub, DBCounter, cts);
            //});

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
            //Task taskWaiterA = Task.Run(() =>
            //{
            //    while (!DBCCounter.IsDone())
            //    {
            //        Thread.Sleep(2000);
            //    }
            //});
            //Task taskWaiterE = Task.Run(() =>
            //{
            //    while (!DBCounter.IsDone())
            //    {
            //        Thread.Sleep(2000);
            //    }
            //});
            //Task taskWaiterf = Task.Run(() =>
            //{
            //    while (!navCounter.IsDone())
            //    {
            //        Thread.Sleep(2000);
            //    }
            //});
            Task.Run(() =>
            {
                while (true)
                {
                    Console.WriteLine($"CounterB: {bobCounter.GetCount()} - IsDone = {bobCounter.IsDone()}");
                    Console.WriteLine($"CounterC: {charlieCounter.GetCount()}  - IsDone = {charlieCounter.IsDone()}");
                    //Console.WriteLine($"CounterA: {DBCCounter.GetCount()}  - IsDone = {DBCCounter.IsDone()}");
                    //Console.WriteLine($"CounterE: {DBCounter.GetCount()}  - IsDone = {DBCounter.IsDone()}");
                    //Console.WriteLine($"CounterF: {navCounter.GetCount()}  - IsDone = {navCounter.IsDone()}");
                    Thread.Sleep(2000);
                }
            });
            //await Task.WhenAll(taskWaiterB, taskWaiterC, taskWaiterA, taskWaiterE, taskWaiterf);

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

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine($"-----------------Timeout------------------");
            System.Environment.Exit(1);
        }

    }
}
