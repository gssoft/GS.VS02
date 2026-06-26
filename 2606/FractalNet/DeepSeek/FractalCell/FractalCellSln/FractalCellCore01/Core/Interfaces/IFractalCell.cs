// Core/Interfaces/IFractalCell.cs

using FractalCellCore.Core.Interfaces;
using Microsoft.Extensions.Hosting;

namespace FractalCellCore.Core.Interfaces;

public interface IFractalCell : IHostedService
{
    string CellId { get; }
    IInternalBus InternalBus { get; }
    IExternalBus ExternalBus { get; }
    Task InitializeAsync();
}
