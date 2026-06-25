// Core/Interfaces/IFractalCell.cs
using Microsoft.Extensions.Hosting;

namespace FractalCell02.Core.Interfaces;

public interface IFractalCell : IHostedService
{
    string CellId { get; }
    IInternalBus InternalBus { get; }
    IExternalBus ExternalBus { get; }
    Task InitializeAsync();
}
