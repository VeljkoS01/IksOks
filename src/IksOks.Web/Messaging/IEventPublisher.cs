using IksOks.Web.Messaging.Contracts;

namespace IksOks.Web.Messaging;

public interface IEventPublisher
{
    Task PublishMatchFinishedAsync(
        MatchFinishedEvent message,
        CancellationToken cancellationToken);
}