using Replate.Domain.Entities;

namespace Replate.Desktop.Data;

internal interface IOrderStore
{
    Task SaveOrderAsync(Order order);
}
