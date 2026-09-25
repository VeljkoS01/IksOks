# Arhitektura sistema IksOks

## 1. Pregled

IksOks je višekorisnička web aplikacija za igranje Iks-Oksa u realnom vremenu. Finalna implementacija je **modularni monolit u ASP.NET Core-u**, sa PostgreSQL bazom, SignalR komunikacijom u realnom vremenu i RabbitMQ brokerom za asinhronu obradu događaja.

Frontend je statički HTML/CSS/JavaScript klijent koji komunicira sa backendom na dva načina:

- HTTP pozivi prema Minimal API endpointima za komande i upite;
- SignalR veza za obaveštenja i sinhronizaciju u realnom vremenu.

Glavni arhitekturni dijagram nalazi se u `docs/uml/architecture.puml`.

## 2. Glavne komponente

### Browser klijent

Lokacije:

- `src/IksOks.Web/wwwroot/index.html`
- `src/IksOks.Web/wwwroot/css/site.css`
- `src/IksOks.Web/wwwroot/js/auth.js`

Klijent prikazuje autentikaciju, lobby, aktivne i završene mečeve, tablu, spectator prikaz, leaderboard, profil, prodavnicu, emoji chat i kontrole meča.

HTTP se koristi za operacije koje menjaju ili čitaju stanje sistema. SignalR se koristi da drugi klijenti odmah dobiju obaveštenje da se stanje promenilo.

### HTTP API sloj

Lokacije:

- `src/IksOks.Web/Endpoints/AuthEndpoints.cs`
- `src/IksOks.Web/Endpoints/MatchEndpoints.cs`
- `src/IksOks.Web/Endpoints/UserEndpoints.cs`
- `src/IksOks.Web/Endpoints/StoreEndpoints.cs`

Endpointi su ulazna tačka za registraciju i prijavu, kreiranje/pridruživanje meču, poteze, pauzu/nastavak, istoriju, live mečeve, profil, leaderboard i prodavnicu.

Endpoint sloj ne treba da sadrži celu domensku logiku. Za najvažnije operacije nad mečem koristi Command handlere, State i Strategy komponente.

### Application sloj

Lokacije:

- `src/IksOks.Web/Application/Commands/`
- `src/IksOks.Web/Application/Concurrency/MatchOperationLock.cs`
- `src/IksOks.Web/Application/Background/MatchTimeoutWorker.cs`

Ovaj sloj orkestrira operacije nad domenom. `MakeMoveCommandHandler` obrađuje potez, proverava stanje meča, red igrača, zauzetost polja, detekciju pobede, završetak meča i upis Outbox poruke.

`MatchControlCommandHandler` obrađuje zahteve i akcije za pauzu i nastavak.

`MatchOperationLock` serijalizuje operacije nad istim mečem pomoću `SemaphoreSlim` instance po `matchId`. Različiti mečevi koriste različite brave i mogu da se obrađuju paralelno.

### Domain sloj

Lokacije:

- `src/IksOks.Web/Domain/Entities/`
- `src/IksOks.Web/Domain/Enums/`
- `src/IksOks.Web/Domain/States/`
- `src/IksOks.Web/Domain/Strategies/`
- `src/IksOks.Web/Domain/Store/`

Glavni domen čine:

- `AppUser`
- `GameMatch`
- `MatchMove`
- `UserPurchase`

Stanje meča je predstavljeno enumeracijom `MatchStatus`:

- `WaitingForOpponent`
- `InProgress`
- `Paused`
- `Finished`

Pravila igre zavise od režima `MatchMode`:

- `Classic`
- `ConnectK`

Domenski sloj sadrži tri glavna projektna obrasca korišćena za zahtev predmeta: Strategy, State i Command (Command je tehnički smešten u Application sloj, ali predstavlja sopstvenu aplikacionu/domensku komandu, a ne obrazac koji dolazi iz frameworka).

## 3. Perzistencija i ORM

Lokacija:

- `src/IksOks.Web/Infrastructure/Persistence/IksOksDbContext.cs`

Aplikacija koristi Entity Framework Core sa Npgsql providerom za PostgreSQL.

`IksOksDbContext` mapira:

- `AppUser` -> `Users`
- `GameMatch` -> `Matches`
- `MatchMove` -> `MatchMoves`
- `UserPurchase` -> `UserPurchases`
- `MatchFinishedEventRecord` -> `MatchFinishedEvents`
- `OutboxMessage` -> `OutboxMessages`

Migracije se nalaze u `src/IksOks.Web/Migrations/`.

SQL dump namenjen predaji nalazi se u `database/iksoks.sql`.

Važna ograničenja postoje i na nivou baze, npr. jedinstveno korisničko ime, jedinstven par `(MatchId, Row, Column)` i jedinstven `(MatchId, MoveNumber)`.

## 4. Real-time komunikacija

Lokacija:

- `src/IksOks.Web/Realtime/MatchHub.cs`

SignalR Hub je mapiran na `/hubs/match` i zahteva autentikaciju.

Za svaki meč koristi se posebna grupa:

`match:{matchId}`

Na taj način više različitih grupa korisnika može istovremeno da radi na različitim mečevima bez mešanja poruka.

Primeri SignalR događaja koje klijent dobija:

- `MatchUpdated`
- `LobbyUpdated`
- `MatchControlChanged`
- `EmojiReceived`
- `BalanceUpdated`

Javni aktivni mečevi mogu da se posmatraju. Spectator ulazi u SignalR grupu, ali se ne registruje kao učesnik koji može da dobije kontrolu nad mečem.

## 5. Kontrola pristupa i konkurentnost

Lokacije:

- `src/IksOks.Web/Realtime/Collaboration/MatchControlRegistry.cs`
- `src/IksOks.Web/Application/Concurrency/MatchOperationLock.cs`

`MatchControlRegistry` vodi red aktivnih učesnika po meču. Prvi učesnik koji se priključi preko SignalR-a postaje kontroler administrativnih operacija meča. Sledeći učesnik nema direktnu kontrolu, ali može da pošalje zahtev za pauzu/nastavak.

Ako kontroler napusti meč ili mu pukne veza, sledeći aktivni učesnik postaje kontroler. `MatchHub` tada emituje `MatchControlChanged`.

Ovo je odvojeno od samog pravila poteza. Tokom igre oba igrača menjaju meč, ali samo kada je njihov red. To ograničenje proverava `MakeMoveCommandHandler`.

`MatchOperationLock` sprečava da dve serverske operacije istovremeno menjaju isti meč. Različiti mečevi se ne zaključavaju međusobno.

## 6. Message broker i Transactional Outbox

Lokacije:

- `src/IksOks.Web/Messaging/IEventPublisher.cs`
- `src/IksOks.Web/Messaging/RabbitMqEventPublisher.cs`
- `src/IksOks.Web/Messaging/OutboxPublisher.cs`
- `src/IksOks.Web/Messaging/MatchFinishedConsumer.cs`
- `src/IksOks.Web/Messaging/Contracts/MatchFinishedEvent.cs`

Kada se meč završi, aplikacija ne šalje RabbitMQ događaj direktno iz iste poslovne operacije. Umesto toga u istoj bazi upisuje `OutboxMessage` sa routing key-em `match.finished`.

Tok je:

1. završi se meč;
2. promena meča i Outbox poruka se snime u PostgreSQL;
3. `OutboxPublisher` periodično čita neobrađene poruke;
4. `RabbitMqEventPublisher` objavi događaj na RabbitMQ topic exchange;
5. `MatchFinishedConsumer` prima događaj;
6. consumer proverava da li je `EventId` već obrađen;
7. dodeljuje tokene igračima i upisuje `MatchFinishedEventRecord`;
8. korisnicima se preko SignalR-a šalje `BalanceUpdated`.

`EventId` ima jedinstven indeks u tabeli `MatchFinishedEvents`, čime se podržava idempotentna obrada događaja.

## 7. Timeout poteza

Lokacija:

- `src/IksOks.Web/Application/Background/MatchTimeoutWorker.cs`

Worker na približno jednu sekundu proverava aktivne mečeve kojima je istekao `TurnDeadlineAt`. Pre izmene uzima `MatchOperationLock` za konkretan meč i ponovo proverava stanje. Ako je vreme stvarno isteklo, igrač koji je bio na potezu gubi, meč se završava, kreira se Outbox događaj i klijenti se obaveštavaju preko SignalR-a.

## 8. Autentikacija i zaštita

Aplikacija koristi cookie autentikaciju.

Relevantno:

- cookie: `IksOks.Auth`
- `HttpOnly = true`
- autentikovani API endpointi koriste `RequireAuthorization()`
- SignalR Hub takođe koristi `RequireAuthorization()`
- lozinke se ne čuvaju kao čist tekst; koristi se ASP.NET Core `PasswordHasher<AppUser>`.

## 9. Infrastruktura

`docker-compose.yml` definiše tri servisa:

- `web`
- `postgres`
- `rabbitmq`

PostgreSQL i RabbitMQ imaju healthcheck. Web servis čeka da oba infrastrukturna servisa postanu zdrava.

Dockerfile koristi multi-stage build: .NET SDK sliku za publish i ASP.NET runtime sliku za izvršavanje.

## 10. Finalna arhitekturna odluka

Finalni sistem nije skup nezavisnih mikroservisa. Backend je jedna ASP.NET Core aplikacija sa jasno odvojenim folderima/slojevima i sa spoljnim PostgreSQL i RabbitMQ servisima. To je preciznije opisati kao modularni monolit sa asinhronom integracijom preko message brokera.

To je važno pri odbrani: treba objašnjavati ono što je stvarno implementirano u finalnom kodu, a ne raniji plan iz prve faze.
