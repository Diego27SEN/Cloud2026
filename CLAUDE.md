# Cloud2026 — Desarrollo de Videojuegos en Soluciones Cloud

Proyecto del curso (16 semanas): implementar Unity Gaming Services sobre un juego en Unity 6.

## Stack

Unity 6 · URP 2D · Input System · Test Framework 1.6.0

UGS instalado: Authentication 3.7.4 · Cloud Save 3.4.1 · Economy 3.5.4 · Cloud Code 2.10.4 ·
Leaderboards 2.3.4 · Remote Config 4.2.5 · Deployment 1.7.2

Falta para la semana 13: `com.unity.services.analytics`.

## Regla de arquitectura (no negociable)

**El cliente no es autoridad.** Ninguna decisión que afecte al valor del juego —saldo, recompensa,
precio, progresión, puntuación de leaderboard, desbloqueo— se calcula en C#. El cliente pide,
Cloud Code decide, el cliente muestra el resultado.

Cloud Code no confía en `params`: valida contra el estado real del servidor y contra la hora del
servidor, nunca contra lo que diga el cliente.

## Estructura

```
Assets/Scripts/Core/       Bootstrap: InitializeAsync, entorno, sesión
Assets/Scripts/Services/   Un wrapper por servicio de UGS
Assets/Scripts/Gameplay/
Assets/Scripts/UI/
Assets/Tests/              EditMode / PlayMode
Assets/Editor/             Herramientas del Editor (generadores de escena, menús)
Assets/CloudCode/          Módulos .js y referencias .ccmr (los descubre la ventana de Deployment)
Assets/UGS/Economy/        .ecs
Assets/UGS/RemoteConfig/   .rc
Assets/UGS/Leaderboards/   .lb
CloudCode.Modules/         Módulos C# (fuera de Assets: Unity no debe compilarlos)
```

La UI y el gameplay no llaman al SDK de UGS directamente: pasan por `Services/`.

## Convenciones

- IDs de UGS (currencies, claves de Remote Config, leaderboards) en `snake_case`. Un ID es un
  contrato entre config, C# y Cloud Code: al renombrar, se actualizan los tres en el mismo cambio.
- Los IDs de módulo de Cloud Code son la excepción: UGS los deriva del nombre del `.csproj`,
  así que van en PascalCase (`HelloWorld`).
- Todo valor ajustable por diseño vive en Remote Config, no incrustado en código.
- Todo `await` de UGS va en try/catch tipado. Nada de `catch` vacíos.
- Secretos y service account keys nunca se versionan.
- Los identificadores de UGS (project ID, organization ID, environment ID) los vacía un filtro
  `clean` de git antes de que entren. Sin configurar, el filtro no falla: deja pasar el contenido
  tal cual. Cada clon lo activa una vez con
  `git config filter.ugs-ids.clean "sh Tools/scrub-ugs-ids.sh"`.
- El código lo leen estudiantes: claridad por encima de astucia.

## Verificación

Con el MCP de Unity conectado, lee la consola del Editor después de editar C# en lugar de asumir
que compiló. Requiere que el Editor esté abierto.

## Agentes

`unity-client` · `cloudcode-backend` · `ugs-config` · `auditor-autoritativo` · `qa-tester` · `github`
