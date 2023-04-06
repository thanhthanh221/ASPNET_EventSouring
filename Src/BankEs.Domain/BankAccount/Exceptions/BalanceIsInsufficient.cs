namespace BankEs.Domain.BankAccount.Exceptions;
public class BalanceIsInsufficient : Exception
{
    public BalanceIsInsufficient() : base("Balance is insufficient")
    {
    }
}
