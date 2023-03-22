using BH.DIS.Events.Demo;
using BH.DIS.SDK.EventHandlers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BH.DIS.Workload.ConsoleApp
{
    public class AliceSaidHelloHandler : IEventHandler<AliceSaidHello>
    {
        private readonly Counter _count;
        public AliceSaidHelloHandler(Counter count)
        {
            _count = count;
        }

        public Task Handle(AliceSaidHello @event, Core.Logging.ILogger logger, IEventHandlerContext context)
        {
            _count.Inc();
            logger.Information($"EventType:{context.EventType}, EventId:{context.EventId}, SessionId:{@event.FakeSessionId}, Counter:{@event.Counter}");

            if (@event.ProvokeError)
                throw new Exception("Message handler failed");

            return Task.CompletedTask;
        }
    }

    public class AliceSaidBonjourHandler : IEventHandler<AliceSaidBonjour>
    {
        private readonly Counter _count;
        public AliceSaidBonjourHandler(Counter count)
        {
            _count = count;
        }

        public Task Handle(AliceSaidBonjour @event, Core.Logging.ILogger logger, IEventHandlerContext context)
        {
            _count.Inc();
            logger.Information($"EventType:{context.EventType}, EventId:{context.EventId}, SessionId:{@event.FakeSessionId}, Counter:{@event.Counter}");

            if (@event.ProvokeError)
                throw new System.Exception("Message handler failed");
            
            return Task.CompletedTask;
        }
    }

    //public class RefinitivSpotRatesReadyHandler : IEventHandler<RefinitivSpotRatesReady>
    //{
    //    private readonly Counter _count;

    //    public RefinitivSpotRatesReadyHandler(Counter count)
    //    {
    //        _count = count;
    //    }

    //    public Task Handle(RefinitivSpotRatesReady @event, Core.Logging.ILogger logger, IEventHandlerContext context)
    //    {
    //        _count.Inc();
    //        logger.Information($"EventType:{context.EventType}, EventId:{context.EventId}, Date:{@event.Date}, Count: {_count}");          

    //        return Task.CompletedTask;
    //    }
    //}

    //public class CustomerMDApprovedHandler : IEventHandler<CustomerMDApproved>
    //{
    //    private readonly Counter _count;
    //    public CustomerMDApprovedHandler(Counter count)
    //    {
    //        _count = count;
    //    }

    //    public Task Handle(CustomerMDApproved @event, Core.Logging.ILogger logger, IEventHandlerContext context)
    //    {                        
    //        _count.Inc();            
    //        logger.Information($"EventType:{context.EventType}, EventId:{context.EventId}, comment:{@event.Comment}, Count: {_count}");

    //        return Task.CompletedTask;
    //    }
    //}

    public class Counter
    {
        int _count;
        readonly int _expected;
        readonly Stopwatch _stopwatch;

        public Counter(int count, int expected)
        {
            _count = count;
            _expected = expected;
            _stopwatch = new Stopwatch();
        }

        public bool IsDone()
        {
            return _count == _expected;
        }

        public int GetCount()
        {
            return _count;
        }

        public TimeSpan GetElapsed()
        {
            return _stopwatch.Elapsed;
        }

        public void Inc()
        {
            if (_count == 0)
            {
                _stopwatch.Start();
            }
            Interlocked.Increment(ref _count);
            if (IsDone())
            {
                _stopwatch.Stop();
            }
        }
    }

}

