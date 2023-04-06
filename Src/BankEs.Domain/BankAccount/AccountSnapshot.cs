namespace BankEs.Domain.BankAccount;

public class AccountSnapshot
{
    public AccountState State { get; set; }
    public long Version { get; set; }

}
