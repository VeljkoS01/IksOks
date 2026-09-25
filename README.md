# IksOks

IksOks je višekorisnička web aplikacija za igranje Iks-Oksa u realnom vremenu.

Aplikacija podržava klasični 3x3 režim i Connect-K režim sa prilagodljivom veličinom table i brojem polja potrebnih za pobedu.

Pored samog igranja, sistem podržava javne i privatne mečeve, posmatranje mečeva uživo, istoriju mečeva, rang listu, personalizaciju profila, emoji chat, prodavnicu i kontrolu toka meča.

---

## Tehnologije

### Backend

- ASP.NET Core
- .NET 8
- Entity Framework Core
- PostgreSQL
- SignalR
- RabbitMQ

### Frontend

- HTML
- CSS
- JavaScript

### Testiranje

- xUnit

### Infrastruktura

- Docker
- Docker Compose

---

## Glavne funkcionalnosti

Aplikacija podržava:

- Registraciju korisnika
- Prijavu i odjavu korisnika
- Classic 3x3 režim
- Connect-K režim
- Izbor veličine table
- Izbor broja povezanih polja potrebnih za pobedu
- Javne mečeve
- Privatne mečeve
- Pridruživanje privatnom meču pomoću koda
- Više aktivnih mečeva
- Real-time sinhronizaciju pomoću SignalR-a
- Posmatranje javnih mečeva uživo
- Istoriju završenih mečeva
- Rang listu korisnika
- Profil korisnika
- Pauziranje i nastavak meča
- Zahteve za pauzu i nastavak
- Dinamičko vlasništvo nad kontrolom meča
- Prenos kontrole meča kada trenutni kontroler napusti meč
- Automatski timeout poteza
- Predaju aktivnog meča
- Odustajanje od meča koji čeka protivnika
- Prikaz završetka meča u realnom vremenu
- Emoji chat tokom meča
- Premium emoji
- Prodavnicu
- Sistem tokena
- Nagrade nakon završenog meča
- Personalizovane okvire profila i igrača
- Prikaz okvira oba igrača tokom meča
- Spectator režim za javne mečeve

---

## Projektni obrasci

U domenskoj i aplikacionoj logici korišćena su tri projektna obrasca:

1. Strategy
2. State
3. Command

Detaljno objašnjenje njihove primene nalazi se u:

docs/Projektni-obrasci.md

UML dijagrami nalaze se u:

docs/uml/

---

## Struktura projekta

Najvažniji delovi repozitorijuma su:

IksOks/
├── database/
│   └── iksoks.sql
│
├── docs/
│   ├── Arhitektura.md
│   ├── Projektni-obrasci.md
│   └── uml/
│       ├── architecture.puml
│       ├── command.puml
│       ├── state.puml
│       └── strategy.puml
│
├── src/
│   └── IksOks.Web/
│
├── tests/
│   └── IksOks.Web.Tests/
│
├── docker-compose.yml
├── IksOks.slnx
└── README.md

---

## Preduslovi

Za pokretanje projekta potrebno je instalirati:

- .NET 8 SDK
- Docker Desktop
- Git

---

## Pokretanje infrastrukture

Iz root foldera projekta pokrenuti:

docker compose up -d postgres rabbitmq

Ova komanda pokreće:

- PostgreSQL bazu podataka
- RabbitMQ message broker

Provera pokrenutih kontejnera:

docker compose ps

---

## Entity Framework alati

Projekat koristi lokalni dotnet-ef alat.

Nakon kloniranja repozitorijuma pokrenuti:

dotnet tool restore

---

## Kreiranje i ažuriranje baze

Nakon pokretanja PostgreSQL kontejnera potrebno je primeniti migracije:

dotnet tool run dotnet-ef database update --project src/IksOks.Web

Time će se kreirati potrebne tabele i primeniti sve postojeće migracije.

---

## Pokretanje aplikacije

Iz root foldera projekta pokrenuti:

dotnet run --project src/IksOks.Web --urls http://localhost:5000

Aplikacija će biti dostupna na adresi:

http://localhost:5000

Za testiranje rada sa više korisnika moguće je otvoriti aplikaciju u više browser prozora ili koristiti različite browser profile.

---

## Pokretanje testova

Svi automatizovani testovi pokreću se komandom:

dotnet test

Testovi proveravaju između ostalog:

- Strategy logiku
- State logiku
- Konkurentni pristup operacijama nad mečem
- Registry za dinamičko vlasništvo nad kontrolom meča

---

## Build projekta

Za proveru da li se ceo projekat uspešno kompajlira koristiti:

dotnet build

---

## Solution fajl

Solution fajl projekta je:

IksOks.slnx

Projekat koristi .slnx format solution fajla.

---

## Baza podataka

Aplikacija koristi PostgreSQL.

Tokom razvoja baza se pokreće preko Docker Compose konfiguracije.

SQL dump baze namenjen za predaju projekta nalazi se u:

database/iksoks.sql

Dump može da se napravi komandom:

docker compose exec -T postgres pg_dump -U iksoks -d iksoks --clean --if-exists --no-owner --no-privileges > database/iksoks.sql

---

## Real-time komunikacija

Za komunikaciju između klijenata koristi se SignalR.

SignalR se koristi za:

- Osvežavanje stanja meča
- Osvežavanje lobby prikaza
- Promenu kontrolera meča
- Emoji chat
- Obaveštavanje o završetku meča
- Real-time spectator prikaz

Svaki meč koristi posebnu SignalR grupu.

---

## Dinamička kontrola meča

Aplikacija implementira sistem kontrole meča između aktivnih učesnika.

Prvi aktivni učesnik koji otvori meč dobija pravo direktne kontrole administrativnih operacija kao što su pauziranje i nastavak.

Drugi učesnik može da šalje zahtev za pauzu ili nastavak.

Ako trenutni kontroler napusti meč ili izgubi konekciju, kontrola se automatski prenosi sledećem aktivnom učesniku.

Spectator korisnici ne učestvuju u redu za kontrolu.

---

## Message broker i Outbox

Za asinhronu komunikaciju koristi se RabbitMQ.

Događaji vezani za završetak meča prvo se čuvaju u bazi kao Outbox poruke.

Poseban publisher šalje događaje ka RabbitMQ brokeru.

Consumer obrađuje završetak meča i dodeljuje nagrade korisnicima.

Ovaj pristup omogućava pouzdanije slanje događaja i odvaja završetak meča od dodatne obrade.

---

## Timeout poteza

Svaki potez ima vremensko ograničenje.

Server čuva rok za završetak poteza i pozadinski worker proverava mečeve kojima je vreme isteklo.

Ako igrač ne odigra potez na vreme, meč se završava odgovarajućim rezultatom.

Operacije koje menjaju stanje meča koriste zaključavanje po ID-u meča kako bi se smanjila mogućnost konkurentnih izmena.

---

## Prodavnica i tokeni

Korisnici dobijaju tokene kroz završene mečeve.

Tokeni mogu da se koriste za kupovinu:

- Premium emoji-ja
- Personalizovanih okvira

Kupljeni emoji mogu da se koriste tokom meča.

Kupljeni okvir može da se aktivira i prikazuje se na profilu i tokom meča.

---

## Javni i privatni mečevi

Javni mečevi mogu biti prikazani u lobby-ju i mogu ih posmatrati drugi korisnici.

Privatni mečevi koriste kod za pridruživanje.

Korisnik koji nema odgovarajući kod ne može da pristupi privatnom meču kao spectator.

---

## Dokumentacija

Dokumentacija projekta nalazi se u:

docs/

Opis projektnih obrazaca:

docs/Projektni-obrasci.md

Opis arhitekture:

docs/Arhitektura.md

UML dijagrami:

docs/uml/strategy.puml
docs/uml/state.puml
docs/uml/command.puml
docs/uml/architecture.puml

---

## Preporučeni redosled pokretanja nakon kloniranja

git clone <repository-url>

cd IksOks

docker compose up -d postgres rabbitmq

dotnet tool restore

dotnet tool run dotnet-ef database update --project src/IksOks.Web

dotnet build

dotnet test

dotnet run --project src/IksOks.Web --urls http://localhost:5000

Nakon toga otvoriti:

http://localhost:5000

---

## Autori

- Veljko Simonović
- Miloš Pavlović