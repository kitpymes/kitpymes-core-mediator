// Una petición que vive en un ensamblado que no se escanea: su manejador está en Kitpymes.Core.Mediator.Tests.Mal.
using Kitpymes.Core.Mediator.Abstractions;
using Kitpymes.Core.Results;

namespace Kitpymes.Core.Mediator.Tests.Contratos
{
    public sealed record ContractCommand : ICommand<Result>;
}
