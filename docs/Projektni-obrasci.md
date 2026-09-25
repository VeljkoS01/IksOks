# Projektni obrasci u IksOks aplikaciji

U finalnoj implementaciji tri glavna projektna obrasca koja se eksplicitno koriste u sopstvenoj aplikacionoj/domenskog logici su:

1. Strategy
2. State
3. Command

MVC, SignalR-ov publish/subscribe mehanizam, Entity Framework i drugi obrasci koje obezbeđuju biblioteke nisu korišćeni kao tri obrasca potrebna za zahtev predmeta.

UML dijagrami se nalaze u `docs/uml/`.

---

## 1. Strategy obrazac

### Problem

Aplikacija podržava više režima igre. Classic i Connect-K nemaju ista pravila validacije konfiguracije, a detekcija pobede mora da radi sa različitim potrebnim brojem povezanih simbola.

Bez Strategy obrasca endpoint ili handler bi imao grananje tipa `if mode == Classic ... else if mode == ConnectK ...` na više mesta. Dodavanje novog režima bi širilo takve uslove kroz kod.

### Učesnici obrasca

Interfejs:

`src/IksOks.Web/Domain/Strategies/IGameRulesStrategy.cs`

Implementacije:

- `ClassicGameRulesStrategy`
- `ConnectKGameRulesStrategy`

Izbor konkretne strategije:

- `GameRulesStrategyFactory`

Zajednička pomoćna logika za detekciju niza:

- `ConsecutiveSymbolsDetector`

### Važne metode

`IGameRulesStrategy` izlaže:

- `IsValidConfiguration(int boardSize, int winLength)`
- `IsWinningMove(IEnumerable<MatchMove> moves, MatchMove lastMove, int winLength)`

`GameRulesStrategyFactory.GetStrategy(MatchMode mode)` vraća odgovarajuću implementaciju.

### Gde se koristi

Pri kreiranju meča `MatchEndpoints.CreateMatchAsync` bira strategiju i proverava da li je konfiguracija dozvoljena.

Pri potezu `MakeMoveCommandHandler` bira strategiju na osnovu `match.Mode` i poziva `IsWinningMove`.

### Zašto je obrazac koristan

Algoritam koji zavisi od režima je izdvojen iza zajedničkog interfejsa. Kod koji orkestrira meč ne mora da zna detalje konkretnog režima.

Dodavanje novog režima zahteva novu implementaciju `IGameRulesStrategy` i proširenje izbora u `GameRulesStrategyFactory`, umesto izmene celog toka poteza.

### Primer toka

1. meč ima `Mode = ConnectK`;
2. handler poziva `GetStrategy(MatchMode.ConnectK)`;
3. factory vraća `ConnectKGameRulesStrategy`;
4. handler poziva `IsWinningMove`;
5. strategija koristi `ConsecutiveSymbolsDetector` sa `winLength` vrednošću meča.

UML: `docs/uml/strategy.puml`.

---

## 2. State obrazac

### Problem

Dozvoljene operacije zavise od trenutnog stanja meča. Meč koji čeka protivnika ne sme da prima poteze. Pauziran meč ne sme da se igra, ali sme da se nastavi. Završen meč ne sme da se menja.

Ako bi sva pravila bila u endpointima, vrlo brzo bi nastao veliki skup `if`/`switch` provera koji se ponavlja u više operacija.

### Učesnici obrasca

Interfejs:

`src/IksOks.Web/Domain/States/IMatchState.cs`

Konkretna stanja:

- `WaitingForOpponentMatchState`
- `InProgressMatchState`
- `PausedMatchState`
- `FinishedMatchState`

Izbor stanja:

- `MatchStateFactory`

### Važne operacije

`IMatchState` definiše pravila kao što su:

- `CanJoin(GameMatch match)`
- `CanMakeMove(GameMatch match)`
- `CanPause(GameMatch match)`
- `CanResume(GameMatch match)`
- `OnOpponentJoined()`
- `OnGameFinished()`
- `OnPaused()`
- `OnResumed()`

`MatchStateFactory.GetState(MatchStatus status)` vraća objekat koji odgovara trenutnom `MatchStatus` stanju.

### Primeri ponašanja

`WaitingForOpponentMatchState`:

- dozvoljava join ako nema protivnika;
- ne dozvoljava potez;
- `OnOpponentJoined()` vraća `InProgress`.

`InProgressMatchState`:

- dozvoljava potez ako postoji protivnik;
- dozvoljava pauzu;
- `OnPaused()` vraća `Paused`;
- `OnGameFinished()` vraća `Finished`.

`PausedMatchState`:

- ne dozvoljava potez;
- dozvoljava nastavak;
- `OnResumed()` vraća `InProgress`.

`FinishedMatchState`:

- ne dozvoljava join, potez, pauzu ili nastavak.

### Gde se koristi

- `MatchEndpoints` pri pridruživanju meču;
- `MakeMoveCommandHandler` pri potezu i završetku;
- `MatchControlCommandHandler` pri pauzi/nastavku i zahtevima.

### Zašto je obrazac koristan

Dozvole i tranzicije su grupisane po stanju. Time je lakše dokazati koje su operacije moguće u kom stanju i smanjuje se verovatnoća da se u jednom endpointu zaboravi neka provera.

UML: `docs/uml/state.puml`.

---

## 3. Command obrazac

### Problem

Operacije kao što su potez, pauza i nastavak imaju mnogo koraka: autorizacija učesnika, zaključavanje meča, proveru stanja, promenu domena, čuvanje u bazi i vraćanje kontrolisanog rezultata.

Ako bi endpoint direktno implementirao celu operaciju, HTTP sloj bi bio previše vezan za poslovnu logiku i teško bi se testirao ili ponovo koristio.

### Osnovne apstrakcije

`src/IksOks.Web/Application/Commands/ICommand.cs`

`src/IksOks.Web/Application/Commands/ICommandHandler.cs`

Komanda predstavlja zahtev da se izvrši operacija, a handler zna kako se ta operacija izvršava.

### Komanda za potez

- `MakeMoveCommand`
- `MakeMoveCommandHandler`
- `MakeMoveCommandResult`

`MakeMoveCommand` sadrži podatke potrebne za operaciju:

- `MatchId`
- `UserId`
- `Row`
- `Column`

`MakeMoveCommandHandler.HandleAsync`:

1. zaključava konkretni meč preko `MatchOperationLock`;
2. otvara DB transakciju;
3. učitava meč i poteze;
4. koristi State da proveri da li je potez dozvoljen;
5. proverava timeout, učesnika, granice table, zauzeto polje i red igrača;
6. kreira `MatchMove`;
7. koristi Strategy za proveru pobede;
8. menja stanje meča ako je pobeda ili remi;
9. pri završetku kreira `OutboxMessage`;
10. čuva promene i commit-uje transakciju;
11. vraća `MakeMoveCommandResult`.

### Komande za kontrolu meča

U `MatchControlCommands.cs` postoje:

- `RequestPauseCommand`
- `PauseMatchCommand`
- `RejectPauseRequestCommand`
- `ResumeMatchCommand`
- `RequestResumeCommand`
- `RejectResumeRequestCommand`

Njih obrađuje `MatchControlCommandHandler`.

Isti handler implementira više `ICommandHandler<TCommand, MatchControlCommandResult>` interfejsa zato što sve te komande rade nad istim skupom pravila i zavisnosti.

### Gde se koristi

`MatchEndpoints` formira odgovarajuću komandu iz HTTP zahteva i prosleđuje je handleru. Endpoint zatim prevodi rezultat komande u odgovarajući HTTP odgovor i šalje real-time obaveštenje kada je potrebno.

### Zašto je obrazac koristan

- odvaja HTTP transport od poslovne operacije;
- komande eksplicitno opisuju nameru korisnika;
- handler centralizuje validaciju i izmenu stanja;
- operacije mogu lakše da se testiraju;
- isti infrastrukturni mehanizmi, kao što je `MatchOperationLock`, mogu da se koriste na jednom mestu.

UML: `docs/uml/command.puml`.

---

## 4. Kako se obrasci kombinuju

Najbolji primer za odbranu je jedan potez:

1. HTTP endpoint primi zahtev za potez.
2. Endpoint kreira `MakeMoveCommand`. **Command** opisuje šta korisnik želi da uradi.
3. `MakeMoveCommandHandler` učita `GameMatch`.
4. `MatchStateFactory` vrati trenutno stanje. **State** odlučuje da li je potez u tom stanju dozvoljen.
5. Handler utvrdi da je igrač na potezu i kreira `MatchMove`.
6. `GameRulesStrategyFactory` izabere pravila za Classic ili Connect-K. **Strategy** odlučuje da li je poslednji potez pobednički.
7. Promene se snime u bazu.
8. Endpoint po uspehu pošalje SignalR obaveštenje klijentima.

Ovaj tok pokazuje da obrasci nisu samo nacrtani na UML-u, već učestvuju u stvarnoj poslovnoj operaciji.

---

## 5. Dodatni obrasci i tehnike koje ne računamo kao glavna tri

### Factory

`MatchStateFactory` i `GameRulesStrategyFactory` centralizuju izbor konkretne implementacije. Factory je prisutan, ali ga nije potrebno računati kao jedan od tri glavna obrasca.

### Transactional Outbox

Pri završetku meča događaj se prvo čuva u `OutboxMessages`, a zatim ga `OutboxPublisher` šalje RabbitMQ-u. To smanjuje rizik da se stanje meča sačuva, a događaj izgubi zbog privremenog pada brokera.

### Idempotent Consumer

`MatchFinishedConsumer` koristi jedinstveni `EventId` i tabelu `MatchFinishedEvents` da isti događaj ne dodeli nagrade dva puta.

### Per-match locking

`MatchOperationLock` obezbeđuje serijalizaciju operacija nad istim mečem, dok različiti mečevi mogu da se menjaju paralelno.

### Real-time publish/subscribe

SignalR grupe omogućavaju obaveštavanje svih klijenata koji prate isti meč. To je važan deo arhitekture, ali se ne predstavlja kao jedan od tri sopstvena projektna obrasca jer SignalR bibliotekа obezbeđuje veliki deo mehanizma.
