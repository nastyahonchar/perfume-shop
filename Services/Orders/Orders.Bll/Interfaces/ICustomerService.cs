using Orders.Bll.Dtos;

namespace Orders.Bll.Interfaces;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct);
    Task<CustomerDto> GetByIdAsync(long id, CancellationToken ct);
    Task<CustomerDto> CreateAsync(SaveCustomerDto input, CancellationToken ct);
    Task<CustomerDto> UpdateAsync(long id, SaveCustomerDto input, CancellationToken ct);
    Task DeleteAsync(long id, CancellationToken ct);
}