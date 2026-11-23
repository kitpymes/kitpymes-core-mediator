# Kitpymes.Core.Mediator

**Mediador para comandos, consultas y notificaciones, siempre con resultados `Result`.**

- **Kitpymes.Core.Mediator.Abstractions**: sólo contratos. Es lo que referencian los proyectos de dominio y de aplicación.
- **Kitpymes.Core.Mediator**: la implementación y el registro.

Depende de [Kitpymes.Core.Results](https://github.com/kitpymes/kitpymes-core-results) (`Result`, `Result<T>`, `Error`).

## Lo esencial

- **Comandos y consultas son tipos distintos** (`ICommand<TResult>`, `IQuery<TResult>`). Así un comportamiento puede aplicarse sólo a comandos, como la transacción, y el compilador impide confundirlos.
- **Siempre se devuelve un `Result`** (salvo una petición nula, que lanza `ArgumentNullException` porque es un error de programación). Lo inesperado (una excepción de un manejador o de un comportamiento, una tarea o un resultado nulos) lo captura el mediador. Lo registra con `ILogger` con el **tipo** de la petición y la excepción completa, nunca con el contenido de la petición, y lo devuelve como `Result` de error `Core.Unexpected`.
  - **Excepción:** si quien llama cancela su `CancellationToken`, la `OperationCanceledException` se relanza, porque nadie espera respuesta.
- **Se comprueba la configuración antes de desplegar.** `MediatorVerifier.VerifyAsync` se ejecuta al arrancar el host (la aplicación no arranca si falla) y se usa en los tests de arquitectura:
  - todo comando y consulta tiene exactamente un manejador;
  - el manejador y sus comportamientos se pueden crear;
  - ninguna petición es a la vez comando y consulta;
  - si la validación está activa, existe su comprobación.
- **Sin API síncrona.** El analizador KPY0003 de Results, que llega con el paquete, da error si se tira el `Result` devuelto.

## Registro

```cs
services.LoadMediator(o => o
    .WithAssemblies(typeof(CreateUserCommand).Assembly)
    .WithValidation(false)             // sólo en tests o librerías: por defecto se exige la comprobación de validación
    .WithStartupVerification(false));  // sólo en tests de integración (WebApplicationFactory), que llaman ellos a VerifyAsync; avisa en el log al arrancar
```

- `ISender` e `IPublisher` se registran con ámbito (scoped). Los manejadores, como transient.
- Llamar a `LoadMediator` dos veces no duplica nada. La validación y la comprobación al arrancar quedan activas si cualquiera de las llamadas las pide: una librería que las quite no las apaga para toda la aplicación.
- No se puede inyectar `ISender` en un singleton (middleware por convención, `BackgroundService`): crea un ámbito y pídelo dentro.
- Los comportamientos los registra quien los usa:

```cs
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LogBehavior<,>));         // todos los comandos y consultas
services.AddTransient(typeof(ICommandBehavior<,>), typeof(TransactionBehavior<,>));  // sólo comandos
services.AddTransient(typeof(IQueryBehavior<,>), typeof(CacheBehavior<,>));          // sólo consultas
```

**Orden de la tubería:** la captura de lo inesperado va por fuera de todo; después los comportamientos generales y, por dentro, los de comando o consulta, cada grupo en orden de registro. Un comportamiento llama a `next()` una sola vez.

## Uso

```cs
public sealed record CreateUserCommand(string Email) : ICommand<Result<Guid>>;

public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (await _users.ExistsAsync(command.Email, cancellationToken))
        {
            return Error.Conflict("Users.EmailDuplicado", "Ya existe un usuario con ese email.");
        }

        // ...
        return Result.Success(user.Id);
    }
}

var result = await _sender.SendAsync(new CreateUserCommand(email), cancellationToken);
```

## Notificaciones

`PublishAsync` ejecuta los manejadores uno detrás de otro, en orden de registro (`LoadMediator` los registra en orden alfabético de su nombre completo dentro de cada ensamblado), y **para en el primero que falla**, devolviendo ese error. Sólo se ejecutan los manejadores del tipo exacto: publicar un evento derivado no ejecuta los de su clase base.

## Comprobación en un test de arquitectura

```cs
var services = new ServiceCollection();
// ... el mismo registro que la aplicación ...
using var provider = services.BuildServiceProvider();

var result = await MediatorVerifier.VerifyAsync(provider);

Assert.IsTrue(result.IsSuccess, result.Error?.Message); // una línea por problema
```

## Lo que no hace (a propósito)

- **Trimming / NativeAOT:** no es compatible. Busca manejadores por reflexión y crea tipos genéricos en ejecución, y `LoadMediator` lo declara con `[RequiresUnreferencedCode]` y `[RequiresDynamicCode]`.
- **ASP.NET Core:** el mediador no lo usa, pero Kitpymes.Core.Results 1.1.1 lo trae (a través de Kitpymes.Core.Shared), así que quien referencie el mediador lo recibe.
- **Datos en el log:** el mediador no registra la petición, pero el mensaje de algunas excepciones puede llevar valores. Por ejemplo, EF Core con `EnableSensitiveDataLogging`, que no se activa fuera de desarrollo.

## 📄 Licencia

MIT. Ver [LICENSE.txt](LICENSE.txt).
