namespace RealEstate.App_Start
{
    using System;
    using log4net;
    using MongoDB.Driver.Core.Events;

    public class Log4NetMongoEvents : IEventSubscriber
    {
        public static ILog CommandStartedLog = LogManager.GetLogger("CommandStarted");

        private readonly ReflectionEventSubscriber _Subscriber;

        public Log4NetMongoEvents()
        {
            _Subscriber = new ReflectionEventSubscriber(this);
        }

        public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
        {
            return _Subscriber.TryGetEventHandler(out handler);
        }

        public void Handle(CommandStartedEvent started)
        {
            CommandStartedLog.Info(new
            {
                started.Command,
                started.CommandName,
                started.ConnectionId,
                started.DatabaseNamespace,
                started.OperationId,
                started.RequestId
            });
        }

        public void Handle(CommandSucceededEvent succeeded)
        {
        }
    }
}