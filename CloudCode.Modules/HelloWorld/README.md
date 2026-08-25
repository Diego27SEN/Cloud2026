# Módulo HelloWorld — Cloud Code (C#)

Primer módulo del curso. Sirve para comprobar de punta a punta que el cliente habla con el
servidor, y para fijar desde el minuto uno quién decide qué.

## Qué hace

Un único endpoint, `SayHello`, que devuelve tres datos:

| Campo           | Lo pone         | Por qué                                                      |
|-----------------|-----------------|--------------------------------------------------------------|
| `Message`       | El servidor     | El saludo se compone en la nube, no en el cliente.            |
| `PlayerId`      | El servidor     | Sale del token de sesión que UGS ya validó.                   |
| `ServerTimeUtc` | El servidor     | La hora del cliente se puede cambiar en los ajustes del SO.   |

El cliente sólo envía `displayName`, que es cosmético. El módulo lo recorta y lo limita antes
de devolverlo: nunca se reenvía tal cual una cadena que venga de fuera.

## Estructura

```
HelloWorld.sln
├── Project/                  El módulo que se despliega
│   ├── HelloWorld.csproj     Su nombre determina el nombre del módulo en UGS
│   ├── HelloWorldModule.cs   El endpoint SayHello
│   ├── GreetingResponse.cs   Lo que devuelve el endpoint
│   ├── ModuleSetup.cs        Registro de dependencias (Economy, Cloud Save, ...)
│   └── Properties/PublishProfiles/FolderProfile.pubxml
└── TestProject/              Pruebas unitarias (no entra en el .ccm)
```

La ventana de Deployment localiza el proyecto de entrada buscando el **único** `.pubxml` de la
solución y subiendo por el árbol hasta encontrar un `.csproj`. Si añades más publish profiles o
más de un `.csproj` en esa carpeta, el despliegue falla.

## Compilar y probar en local

```bash
dotnet test CloudCode.Modules/HelloWorld/HelloWorld.sln
```

## Desplegar

1. Abre el proyecto en Unity con la cuenta de UGS vinculada.
2. **Edit > Preferences > Cloud Code**: comprueba que la ruta de .NET apunta a tu instalación.
3. **Services > Deployment** (o **Window > Deployment**).
4. Selecciona el entorno de destino.
5. Marca `HelloWorld.ccmr` y pulsa **Deploy**.

Unity publica la solución en `Release` para `linux-x64`, la empaqueta como `HelloWorld.ccm` y la
sube. El proyecto de pruebas queda fuera porque lleva `IsPublishable=false`.

## Llamarlo desde el cliente

`Assets/Scripts/Services/UGSCloudCodeService.cs`:

```csharp
CloudCodeService.Instance.CallModuleEndpointAsync<GreetingResult>(
    "HelloWorld", "SayHello", args);
```

El nombre del módulo (`"HelloWorld"`) es el del `.csproj`. Si renombras el proyecto, hay que
cambiarlo también en la constante `ModuleName` del servicio.

## Si algo falla

| Síntoma                                   | Causa habitual                                             |
|-------------------------------------------|------------------------------------------------------------|
| `NotFound` al llamar                      | El módulo no está desplegado en ese entorno.               |
| `Unauthorized`                            | No hay sesión iniciada, o el token caducó.                 |
| `Failed to compile` en Deployment         | Falta .NET, o la ruta en Preferences apunta a otro sitio.  |
| `Could not find a Publish Profile`        | Se borró el `.pubxml`, o hay más de uno.                   |
