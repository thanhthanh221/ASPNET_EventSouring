namespace BankEs.Domain.BankCustomer;

public class CustomerId
{
    public CustomerId(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; set; }
}
