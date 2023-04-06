namespace BankEs.Domain.BankAccount;
public class AccountId
{
    public Guid Id { get; }
    public AccountId(Guid id)
    {
        Id = id;
    }

}
