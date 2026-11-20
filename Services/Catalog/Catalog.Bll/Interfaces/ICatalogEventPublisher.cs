namespace Catalog.Bll.Interfaces;

public interface ICatalogEventPublisher
{
    Task PublishPriceChangedAsync(int productId, string productName, decimal oldPrice, decimal newPrice);

    Task PublishProductRenamedAsync(int productId, string oldName, string newName);
}