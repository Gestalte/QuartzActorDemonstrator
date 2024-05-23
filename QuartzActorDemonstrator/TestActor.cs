using Akka.Actor;
using Akka.Event;

namespace QuartzActorDemonstrator
{
    internal sealed class TestActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        internal sealed record TestMessage();
        public TestActor()
        {
            Receive<TestMessage>(_ =>
            {
                logger.Info("Receive {MessageName}", nameof(TestMessage));
            });
        }
    }
}
