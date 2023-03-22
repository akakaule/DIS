using BH.DIS.Core.Events;
using System.ComponentModel;

namespace BH.DIS.Events.Demo
{
    public class AliceSaidHello : Event
    {
        //sds dsdssdssd sds d
        public static AliceSaidHello Example = new AliceSaidHello()
        {
            ProvokeError = false,
            Counter = 3,
            FakeSessionId = "1234"
        };

        public override string GetSessionId() => string.IsNullOrEmpty(FakeSessionId) ? "hello" : FakeSessionId;

        [Description("If 'ProvokeError' is true, then Bob should throw an exception when handling the event.")]
        public bool ProvokeError { get; set; }

        public int Counter { get; set; }

        public string FakeSessionId { get; set; }
    }

    public class AliceSaidHelloWithRetry : Event
    {
        public static AliceSaidHelloWithRetry Example = new AliceSaidHelloWithRetry()
        {
            ProvokeError = false,
            Counter = 3,
            FakeSessionId = "123"
        };
        public override string GetSessionId() => string.IsNullOrEmpty(FakeSessionId) ? "hello" : FakeSessionId;

        [Description("If 'ProvokeError' is true, then Bob should throw an exception when handling the event.")]
        public bool ProvokeError { get; set; }

        public int Counter { get; set; }

        public string FakeSessionId { get; set; }
    }

    public class AliceSaidBonjour : Event
    {
        public static AliceSaidBonjour Example = new AliceSaidBonjour()
        {
            ProvokeError = false,
            Counter = 3,
            FakeSessionId = "123"
        };
        public override string GetSessionId() => string.IsNullOrEmpty(FakeSessionId) ? "hello" : FakeSessionId;

        [Description("If 'ProvokeError' is true, then Bob should throw an exception when handling the event.")]
        public bool ProvokeError { get; set; }

        public int Counter { get; set; }

        public string FakeSessionId { get; set; }
    }
}
