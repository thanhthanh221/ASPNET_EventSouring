using System.Text;
using System.Text.Json;
using BankEs.Domain.BankAccount;
using BankEs.Domain.BankAccount.Events;
using EventStore.Client;
using static EventStore.Client.EventStoreClient;

namespace BankEs.Infrastructure;
public class AccountAggregateRepository
{
    private readonly EventStoreClient eventStoreClient;

    public AccountAggregateRepository(EventStoreClient eventStoreClient)
    {
        this.eventStoreClient = eventStoreClient;
    }

    private static string GetStreamName(AccountId accountId) => $"account-{accountId.Id}";
    private static string GetSnapshotName(AccountId accountId) => $"AccountSnapshot-{accountId.Id}";


    public async Task<Account> GetAsync(AccountId accountId, CancellationToken cancellationToken = default)
    {
        var snapshot = await LoadSnapshotAsync(accountId);
        var account = snapshot == null ? new Account(accountId) : new Account(accountId, snapshot);

        var events = eventStoreClient.ReadStreamAsync(
            Direction.Forwards,
            GetStreamName(accountId),
            snapshot == null ? StreamPosition.Start : StreamPosition.FromInt64(snapshot.Version + 1),
            cancellationToken: cancellationToken
        );

        if (await events.ReadState == ReadState.StreamNotFound)
            return account;

        await foreach (var @event in events)
        {
            var data = Encoding.UTF8.GetString(@event.Event.Data.ToArray());

            account.LoadChanges(
                @event.OriginalEventNumber.ToInt64(),
                @event.Event.EventType switch
                {
                    nameof(AccountCreated) => JsonSerializer.Deserialize<AccountCreated>(data),
                    nameof(MoneyDeposited) => JsonSerializer.Deserialize<MoneyDeposited>(data),
                    nameof(MoneyWithdrawn) => JsonSerializer.Deserialize<MoneyWithdrawn>(data),
                    _ => throw new NotImplementedException()
                });
        }

        return account;
    }
    public async Task SaveAsync(Account account)
    {
        if (!account.GetChanges().Any()) return;

        var changes = account.GetChanges()
            .Select(change => new EventData(
                eventId: Uuid.NewUuid(),
                type: change.GetType().Name,
                data: JsonSerializer.SerializeToUtf8Bytes(change)));
        
        var result = await eventStoreClient.AppendToStreamAsync(
            GetStreamName(account.Id),
            StreamRevision.FromInt64(account.Version),
            changes
        );

        if (result.NextExpectedStreamRevision.ToInt64() % 5 == 0)
            await AppendSnapshotAsync(account, result.NextExpectedStreamRevision.ToInt64());

    }
    private async Task<AccountSnapshot> LoadSnapshotAsync(AccountId accountId)
    {
        ReadStreamResult events = eventStoreClient.ReadStreamAsync(
            Direction.Backwards,
            GetSnapshotName(accountId),
            StreamPosition.End,
            maxCount: 1
        );
        if (await events.ReadState == ReadState.StreamNotFound) return null;

        var lastEvent = await events.ElementAtAsync(0);
        var data = Encoding.UTF8.GetString(lastEvent.Event.Data.ToArray());

        return JsonSerializer.Deserialize<AccountSnapshot>(data);
    }
    private async Task AppendSnapshotAsync(Account account, long version)
    {
        await eventStoreClient.AppendToStreamAsync(
            GetSnapshotName(account.Id),
            StreamState.Any,
            new EventData[] {
                new EventData(
                    Uuid.NewUuid(),"snapshot",JsonSerializer.SerializeToUtf8Bytes(
                        new AccountSnapshot{State =  account.State,Version = version}
                        )
                    )
            }
        );
    }


}
