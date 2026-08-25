# Módulo TurnMatch — turnos con idempotencia

Prueba de concepto de una partida asíncrona por turnos entre dos jugadores, con el
servidor como única autoridad.

## El problema que resuelve

**La red pierde respuestas, no peticiones.** Un cliente que envía "paso turno" y no
recibe nada no sabe si el servidor lo aplicó o si la petición se perdió por el camino.
Su única salida sensata es reintentar. Si el servidor no está preparado, ese reintento
pasa dos turnos y el jugador pierde su jugada sin entender por qué.

Hay dos mecanismos que se confunden a menudo y que resuelven problemas distintos:

| | Protege contra | Qué pasa sin él |
|---|---|---|
| **Clave de idempotencia** (`requestId`) | El reintento de un mismo cliente | Un reintento cuenta dos turnos |
| **Write lock** de Cloud Save | Dos jugadores escribiendo a la vez | Una jugada pisa la otra y desaparece |

Hacen falta los dos. Este módulo usa los dos.

## El orden importa

En `SubmitTurn`, la comprobación de idempotencia va **antes** que cualquier validación:

```
1. ¿Ya procesamos este requestId?   -> replayed, no se toca el estado
2. ¿Participa este jugador?          -> si no, excepción
3. ¿La partida ha empezado?          -> not_started
4. ¿El cliente va con el estado viejo? -> stale
5. ¿Es su turno?                     -> not_your_turn
6. Aplicar y guardar con write lock  -> applied, o conflict si perdió la carrera
```

Si la validación fuera primero, un reintento legítimo se rechazaría con "no es tu
turno" — porque la primera petición sí llegó y el turno ya pasó al rival. El jugador
se quedaría sin saber si su jugada contó. Hay un test que documenta justo eso:
`UnReintentoLegitimoSeriaRechazadoSiSeValidaraAntesDeMirarLaIdempotencia`.

## Dónde vive el estado

Cloud Save, como **custom data privado**:

- *Custom data* porque el estado cuelga del código de la partida y no de un jugador,
  que es lo que permite que dos personas compartan el mismo dato.
- *Privado* porque así sólo Cloud Code puede leerlo y escribirlo. Ninguno de los dos
  jugadores puede reescribir la partida desde su cliente.

El `writeLock` que devuelve la lectura se reenvía al guardar. Si otra escritura entró
en medio, Cloud Save responde 409 y el módulo devuelve `conflict` en vez de pisar el
trabajo del otro.

## El código de partida

No se sortea al azar: se **deriva del `requestId`** con un hash. Si el código fuera
aleatorio, reintentar "crear partida" dejaría al jugador con dos partidas. Derivándolo,
el reintento cae sobre la misma y el módulo responde `replayed`.

El alfabeto excluye `I`, `O`, `0` y `1`, que son los que se confunden al dictar un
código en voz alta.

## Endpoints

| Función | Parámetros | Qué hace |
|---|---|---|
| `CreateMatch` | `requestId` | Crea la partida y devuelve su código |
| `JoinMatch` | `matchCode`, `requestId` | Entra el segundo jugador y arranca el turno 1 |
| `SubmitTurn` | `matchCode`, `requestId`, `expectedTurnNumber` | Pasa el turno |
| `GetMatch` | `matchCode` | Consulta el estado |

`Outcome` en la respuesta: `ok`, `applied`, `replayed`, `not_started`, `stale`,
`not_your_turn`, `conflict`.

Las jugadas rechazadas viajan en el `Outcome`, no como excepción: no son errores, son
parte normal del juego, y el cliente necesita distinguirlas para reaccionar bien. Las
excepciones se reservan para lo que sí es excepcional (código inexistente, no participas
en la partida).

## Probarlo

```bash
dotnet test CloudCode.Modules/TurnMatch/TurnMatch.sln
```

17 pruebas sobre `MatchRules` y `MatchCode`. Corren sin servidor porque la lógica está
separada de la entrada/salida: `MatchRules` no sabe que existe Cloud Save.

## Demostrarlo en clase

1. Despliega el módulo y abre `Assets/Scenes/TurnMatch.unity` en dos clientes
   (Editor y una build, o dos builds).
2. En el primero: **Entrar como invitado** → **Crear partida**. Apunta el código.
3. En el segundo: **Entrar como invitado** → escribe el código → **Unirse**.
4. Pasa turnos alternando. Cada cliente ve el turno avanzar por el sondeo.
5. Ahora lo interesante: pulsa **Reenviar la misma petición**. El servidor responde
   `replayed`, el turno **no** avanza y la pantalla lo dice.
6. Compara: pulsa **Pasar turno** dos veces seguidas cuando te toque. La primera se
   aplica; la segunda es una jugada nueva, con `requestId` nuevo, y el servidor la
   rechaza con `not_your_turn` porque el turno ya pasó al rival.

El paso 5 frente al 6 es el PoC entero: la misma llamada al mismo endpoint se comporta
distinto según si el identificador es el mismo o uno nuevo.

## Lo que este PoC no cubre

- **Creación simultánea del mismo código.** `CreateMatch` lee y luego escribe sin write
  lock, porque al crear no hay lock previo que enviar. Dos peticiones que derivaran el
  mismo código en el mismo instante podrían pisarse. Con 4 caracteres del alfabeto son
  ~1 millón de combinaciones, así que en clase no pasa; en producción se resolvería con
  una escritura condicional "sólo si no existe".
- **Limpieza de partidas.** Nada borra las partidas terminadas.
- **Tiempo real.** El rival aparece por sondeo cada 2 segundos, no por notificación.
  Cloud Code tiene mensajería en tiempo real (Wire) para eso; queda para la semana 9.
- **Abandono.** No hay rendición, expulsión por inactividad ni fin de partida.
