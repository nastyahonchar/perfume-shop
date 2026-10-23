using AutoMapper;
using Orders.Bll.Dtos;
using Orders.Bll.Interfaces;
using Orders.Dal.Interfaces;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;

namespace Orders.Bll.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CustomerService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct)
    {
        var customers = await _uow.Customers.GetAllAsync(ct);
        return customers.Select(c => _mapper.Map<CustomerDto>(c)).ToList();
    }

    public async Task<CustomerDto> GetByIdAsync(long id, CancellationToken ct)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct)
                       ?? throw new NotFoundException($"Customer {id} not found");
        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto> CreateAsync(SaveCustomerDto input, CancellationToken ct)
    {
        var email = input.Email.Trim();
        if (await _uow.Customers.GetByEmailAsync(email, ct) is not null)
            throw new BusinessConflictException($"Customer with email '{email}' already exists");

        var id = await _uow.Customers.AddAsync(new Customer
        {
            Email = email,
            FullName = input.FullName.Trim(),
            Phone = NormalizePhone(input.Phone)
        }, ct);

        var created = await _uow.Customers.GetByIdAsync(id, ct)
                      ?? throw new NotFoundException($"Customer {id} not found");
        return _mapper.Map<CustomerDto>(created);
    }

    public async Task<CustomerDto> UpdateAsync(long id, SaveCustomerDto input, CancellationToken ct)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct)
                       ?? throw new NotFoundException($"Customer {id} not found");

        var email = input.Email.Trim();
        var sameEmail = await _uow.Customers.GetByEmailAsync(email, ct);
        if (sameEmail is not null && sameEmail.Id != id)
            throw new BusinessConflictException($"Customer with email '{email}' already exists");

        customer.Email = email;
        customer.FullName = input.FullName.Trim();
        customer.Phone = NormalizePhone(input.Phone);

        await _uow.Customers.UpdateAsync(customer, ct);
        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        if (!await _uow.Customers.DeleteAsync(id, ct))
            throw new NotFoundException($"Customer {id} not found");
    }

    private static string? NormalizePhone(string? phone)
        => string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
}