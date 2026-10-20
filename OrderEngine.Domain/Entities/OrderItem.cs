namespace OrderEngine.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; private set; }
    public string Product { get; private set; }
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }

    public decimal Subtotal => Quantity * Price;

    private OrderItem()
    {
        Product = null!;
    }

    internal OrderItem(string product, int quantity, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(product);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(price);

        Id = Guid.NewGuid();
        Product = product;
        Quantity = quantity;
        Price = price;
    }
}
