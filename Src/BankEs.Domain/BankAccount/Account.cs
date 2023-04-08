using BankEs.Domain.BankAccount.Events;
using BankEs.Domain.BankAccount.Exceptions;
using BankEs.Domain.BankCustomer;
using BankEs.Domain.BankMoney;

namespace BankEs.Domain.BankAccount;
public class Account
{
    public AccountId Id { get; }
    public long Version { get; private set; } = -1;
    public AccountState State { get; } = new();

    private readonly List<object> _changes = new();
    public IReadOnlyCollection<object> GetChanges() => _changes.AsReadOnly();

    public Account(AccountId id)
    {
        Id = id;
    }

    public Account(AccountId id, AccountSnapshot snapshot)
    {
        Id = id;
        State = snapshot.State;
        Version = snapshot.Version;
    }

    private void AddEvent(object @event)
    {
        ApplyEvent(@event);

        _changes.Add(@event);
    }
    public void CreateAccount(CustomerId customerId, Currency currency)
    {
        AddEvent(new AccountCreated(customerId, currency));
    }
    public void DepositMoney(Money money)
    {
        AddEvent(new MoneyDeposited(money));
    }
    public void WithdrawMoney(Money money)
    {
        AddEvent(new MoneyWithdrawn(money));
    }

    private void ApplyEvent(object @event)
    {
        switch (@event)
        {
            case AccountCreated accountCreated:
                Apply(accountCreated);
                break;
            case MoneyDeposited moneyDeposited:
                Apply(moneyDeposited);
                break;
            case MoneyWithdrawn moneyWithdrawn:
                Apply(moneyWithdrawn);
                break;
        }
    }

    private void Apply(AccountCreated accountCreated)
    {
        State.CustomerId = accountCreated.CustomerId;
        State.Balance = Money.Zero(accountCreated.Currency);
    }
    private void Apply(MoneyDeposited moneyDeposited)
    {
        State.Balance += moneyDeposited.Money;
    }
    private void Apply(MoneyWithdrawn moneyWithdrawn)
    {
        if (State.Balance - moneyWithdrawn.Money < Money.Zero(State.Balance.Currency)) throw new BalanceIsInsufficient();
        State.Balance -= moneyWithdrawn.Money;
    }
    public void LoadChanges(long version, object change)
    {
        Version = version;

        ApplyEvent(change);
    }
}

