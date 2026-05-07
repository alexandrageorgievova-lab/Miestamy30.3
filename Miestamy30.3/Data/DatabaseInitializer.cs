using Dapper;
using Miestamy30._3.Repositories.Interfaces;

namespace Miestamy30._3.Data;

public class DatabaseInitializer(DbConnectionFactory factory, IKategoriaRepository kategoriaRepo,
    IFilterRepository filterRepo, IMiestoRepository miestoRepo)
{
    public async Task InitializeAsync()
    {
        await CreateSchemaAsync();
        await SeedAsync();
        await SeedEventsAsync();
    }

    private async Task CreateSchemaAsync()
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync(@"
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Kategoria (
                Id    INTEGER PRIMARY KEY AUTOINCREMENT,
                Nazov TEXT    NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS Filter (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Nazov       TEXT    NOT NULL,
                KategoriaId INTEGER NOT NULL,
                FOREIGN KEY (KategoriaId) REFERENCES Kategoria(Id),
                UNIQUE (Nazov, KategoriaId)
            );

            CREATE TABLE IF NOT EXISTS Miesto (
                Id     INTEGER PRIMARY KEY AUTOINCREMENT,
                Nazov  TEXT    NOT NULL UNIQUE,
                Adresa TEXT,
                Lat    REAL,
                Lng    REAL,
                Popis  TEXT,
                WebUrl TEXT
            );

            CREATE TABLE IF NOT EXISTS MiestoKategoria (
                MiestoId    INTEGER NOT NULL,
                KategoriaId INTEGER NOT NULL,
                JeHlavna    INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (MiestoId, KategoriaId),
                FOREIGN KEY (MiestoId)    REFERENCES Miesto(Id)    ON DELETE CASCADE,
                FOREIGN KEY (KategoriaId) REFERENCES Kategoria(Id)
            );

            CREATE TABLE IF NOT EXISTS MiestoFilter (
                MiestoId INTEGER NOT NULL,
                FilterId INTEGER NOT NULL,
                PRIMARY KEY (MiestoId, FilterId),
                FOREIGN KEY (MiestoId) REFERENCES Miesto(Id)   ON DELETE CASCADE,
                FOREIGN KEY (FilterId) REFERENCES Filter(Id)
            );

            CREATE TABLE IF NOT EXISTS TypPodujatia (
                Id    INTEGER PRIMARY KEY AUTOINCREMENT,
                Nazov TEXT    NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS EventFilter (
                Id    INTEGER PRIMARY KEY AUTOINCREMENT,
                Nazov TEXT    NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS Podujatie (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                Nazov    TEXT NOT NULL,
                Popis    TEXT,
                DatumOd  TEXT NOT NULL,
                DatumDo  TEXT,
                Adresa   TEXT,
                Lat      REAL,
                Lng      REAL,
                MiestoId INTEGER,
                FOREIGN KEY (MiestoId) REFERENCES Miesto(Id)
            );

            CREATE TABLE IF NOT EXISTS PodujatieTyp (
                PodujatieId INTEGER NOT NULL,
                TypId       INTEGER NOT NULL,
                PRIMARY KEY (PodujatieId, TypId),
                FOREIGN KEY (PodujatieId) REFERENCES Podujatie(Id) ON DELETE CASCADE,
                FOREIGN KEY (TypId)       REFERENCES TypPodujatia(Id)
            );

            CREATE TABLE IF NOT EXISTS PodujatieFilter (
                PodujatieId INTEGER NOT NULL,
                FilterId    INTEGER NOT NULL,
                PRIMARY KEY (PodujatieId, FilterId),
                FOREIGN KEY (PodujatieId) REFERENCES Podujatie(Id) ON DELETE CASCADE,
                FOREIGN KEY (FilterId)    REFERENCES EventFilter(Id)
            );
        ");
    }

    private async Task SeedAsync()
    {
        var existingKategorie = await kategoriaRepo.GetAll();
        if (existingKategorie.Any()) return; // already seeded

        // ── 1. Kategorie ────────────────────────────────────────────────────────
        var kategorieNazvy = new[] { "Kaviarne", "Jedlo", "Hudba", "Kultúra", "Šport", "Drinks", "Outdoor", "Fashion", "Craft", "Knihy" };
        var kategorieIds = new Dictionary<string, int>();
        foreach (var n in kategorieNazvy)
            kategorieIds[n] = await kategoriaRepo.Create(new Models.Kategoria { Nazov = n });

        // ── 2. Filtre per kategória ──────────────────────────────────────────────
        var filtreDef = new Dictionary<string, string[]>
        {
            ["Kaviarne"] = ["Fajčiarske dnu", "Speciality coffee", "Vegan options", "Terasa", "Terasa nefajčiarska", "Bezbariérový prístup", "Platba kartou", "Pet friendly", "Koláče", "Raňajky", "Matcha"],
            ["Jedlo"]    = ["Speciality coffee", "Vegan options", "Terasa", "Bezbariérový prístup", "Platba kartou", "Pet friendly", "Pekáreň", "Food truck", "Reštaurácia/Bistro", "Fast food", "Denné menu", "A la carte", "Raňajky", "Zmrzlina"],
            ["Hudba"]    = ["Divadlo", "Koncertná hala", "Vstup kartou", "Elektronika", "DnB", "Acoustic", "Gitarová hudba", "Jazz", "Klasická hudba", "Pet friendly"],
            ["Kultúra"]  = ["Galéria", "Eventový space", "Kino", "Divadlo", "Koncertná hala", "Cash / card", "Diskusie", "Bezbariérové", "Pet friendly"],
            ["Šport"]    = ["Svetlá", "Futbal", "Basket", "Hokej / Korčuľovanie", "Ping pong", "Free", "Treba sa objednať", "Plaváreň", "Platba kartou", "Platba cash", "Hokejbal / Florbal", "Tenis", "Bedminton", "Volejbal", "Bežecká dráha", "Bowling", "Workout", "Baseball", "Štadión", "Skate / inline", "Hala / pod strechou"],
            ["Drinks"]   = ["Čapované pivo", "Víno", "Miešané drinky", "Fajčiarske dnu", "Terasa nefajčiarska", "Terasa", "Late night", "Platba kartou"],
            ["Outdoor"]  = ["Skatespot", "Outdoor fitko", "Oplotené psie výbehy", "Psie výbehy", "Romantické miesta", "Opekanie", "Vyhliadkové miesta", "Park", "Les / lesopark", "Kúpanie"],
            ["Fashion"]  = ["Second hand", "Upcycling", "Nové", "Tatérske štúdiá", "Piercingové štúdiá"],
            ["Craft"]    = ["Fotolab", "Zdieľaná dielňa", "Streetart shop", "Železiarstvo", "Domáce potreby"],
            ["Knihy"]    = ["Antikvariát", "Kníhkupectvo", "Knižnica", "Spoločenské hry", "Čitáreň"],
        };

        var filtreIds = new Dictionary<(string kat, string fil), int>();
        foreach (var (kat, filtre) in filtreDef)
        {
            foreach (var f in filtre)
            {
                var id = await filterRepo.Create(new Models.Filter { Nazov = f, KategoriaId = kategorieIds[kat] });
                filtreIds[(kat, f)] = id;
            }
        }

        // ── 3. Miesta + ich kategórie + filtre ─────────────────────────────────
        async Task<int> AddMiesto(string nazov, string? adresa = null, double? lat = null, double? lng = null, string? popis = null)
        {
            return await miestoRepo.Create(new Models.Miesto { Nazov = nazov, Adresa = adresa, Lat = lat, Lng = lng, Popis = popis });
        }

        async Task LinkKat(int miestoId, string katNazov, bool hlavna = true)
            => await miestoRepo.AddKategoria(miestoId, kategorieIds[katNazov], hlavna);

        async Task LinkFil(int miestoId, string katNazov, params string[] filtre)
        {
            foreach (var f in filtre)
                if (filtreIds.TryGetValue((katNazov, f), out var fid))
                    await miestoRepo.AddFilter(miestoId, fid);
        }

        // ── Kaviarne ────────────────────────────────────────────────────────────
        var ciary = await AddMiesto("Čiary", "Námestie SNP, Bratislava", 48.14389920, 17.11077902);
        await LinkKat(ciary, "Kaviarne");
        await LinkKat(ciary, "Drinks", false);
        await LinkFil(ciary, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa", "Terasa nefajčiarska", "Platba kartou", "Koláče", "Matcha");
        await LinkFil(ciary, "Drinks", "Víno", "Terasa", "Platba kartou");

        var blueMondays = await AddMiesto("Blue Mondays", "Obchodná, Bratislava", 48.14458733, 17.11557950);
        await LinkKat(blueMondays, "Kaviarne");
        await LinkFil(blueMondays, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa", "Terasa nefajčiarska", "Platba kartou", "Koláče", "Raňajky", "Matcha");

        var soren = await AddMiesto("Soren", "Laurinská, Bratislava", 48.1432, 17.1098);
        await LinkKat(soren, "Kaviarne");
        await LinkFil(soren, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa", "Terasa nefajčiarska", "Platba kartou", "Koláče", "Raňajky", "Matcha");

        var jungleRoastery = await AddMiesto("Jungle Roastery", "Dostojevského rad, Bratislava", 48.1452, 17.1086);
        await LinkKat(jungleRoastery, "Kaviarne");
        await LinkKat(jungleRoastery, "Hudba", false);
        await LinkFil(jungleRoastery, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa", "Terasa nefajčiarska", "Platba kartou", "Koláče", "Raňajky");
        await LinkFil(jungleRoastery, "Hudba", "Acoustic");

        var goriffee = await AddMiesto("Goriffee", "Sedlárska, Bratislava", 48.1441, 17.1114);
        await LinkKat(goriffee, "Kaviarne");
        await LinkFil(goriffee, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa", "Terasa nefajčiarska", "Platba kartou", "Koláče");

        var kauka = await AddMiesto("Kauka", "Panenská, Bratislava", 48.1468, 17.1045);
        await LinkKat(kauka, "Kaviarne");
        await LinkFil(kauka, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa nefajčiarska", "Platba kartou", "Koláče", "Raňajky");

        var lotRoastery = await AddMiesto("LOT Roastery", "Školská, Bratislava", 48.1459, 17.1059);
        await LinkKat(lotRoastery, "Kaviarne");
        await LinkFil(lotRoastery, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa", "Terasa nefajčiarska", "Platba kartou");

        var matsu = await AddMiesto("Matsu", "Župné námestie, Bratislava", 48.1425, 17.1119);
        await LinkKat(matsu, "Kaviarne");
        await LinkFil(matsu, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa nefajčiarska", "Platba kartou", "Koláče", "Matcha");

        var baryk = await AddMiesto("Baryk", "Grösslingová, Bratislava", 48.1418, 17.1090);
        await LinkKat(baryk, "Kaviarne");
        await LinkFil(baryk, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa", "Platba kartou", "Koláče", "Raňajky");

        var vtakKaviaren = await AddMiesto("Kaviareň Vták", "Račianska, Bratislava", 48.14903887, 17.11716915);
        await LinkKat(vtakKaviaren, "Kaviarne");
        await LinkFil(vtakKaviaren, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa", "Terasa nefajčiarska", "Platba kartou", "Pet friendly");

        var temnyOstBlock = await AddMiesto("Temný Ost Block", "Špitálska, Bratislava", 48.14538041, 17.11474368);
        await LinkKat(temnyOstBlock, "Kaviarne");
        await LinkFil(temnyOstBlock, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa", "Platba kartou");

        var oproti = await AddMiesto("Oproti", "Heydukova, Bratislava", 48.1449, 17.1060);
        await LinkKat(oproti, "Kaviarne");
        await LinkFil(oproti, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa", "Terasa nefajčiarska", "Platba kartou", "Raňajky");

        var mogg = await AddMiesto("Mogg", "Panenská, Bratislava", 48.1469, 17.1047);
        await LinkKat(mogg, "Kaviarne");
        await LinkFil(mogg, "Kaviarne", "Speciality coffee", "Terasa", "Terasa nefajčiarska", "Platba kartou", "Raňajky", "Matcha");

        var mandla = await AddMiesto("Mandľa", "Štúrova, Bratislava", 48.1430, 17.1108);
        await LinkKat(mandla, "Kaviarne");
        await LinkFil(mandla, "Kaviarne", "Speciality coffee", "Vegan options", "Platba kartou", "Koláče");

        var giraffeB = await AddMiesto("Giraffe Bakery", "Župné námestie, Bratislava", 48.18342437, 17.13213319);
        await LinkKat(giraffeB, "Kaviarne");
        await LinkKat(giraffeB, "Jedlo", false);
        await LinkFil(giraffeB, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa", "Terasa nefajčiarska", "Platba kartou");
        await LinkFil(giraffeB, "Jedlo", "Pekáreň", "Denné menu", "Raňajky", "Vegan options", "Platba kartou");

        // ── Jedlo ────────────────────────────────────────────────────────────────
        var otto = await AddMiesto("Otto!", "Obchodná, Bratislava", 48.1465, 17.1072);
        await LinkKat(otto, "Jedlo");
        await LinkFil(otto, "Jedlo", "Speciality coffee", "Vegan options", "Terasa", "Bezbariérový prístup", "Platba kartou", "Reštaurácia/Bistro", "Denné menu", "A la carte", "Raňajky");

        var ramenKazu = await AddMiesto("Ramen Kazu", "Panenská, Bratislava", 48.1469, 17.1046);
        await LinkKat(ramenKazu, "Jedlo");
        await LinkFil(ramenKazu, "Jedlo", "Vegan options", "Terasa", "Platba kartou", "Reštaurácia/Bistro", "A la carte");

        var studna = await AddMiesto("Studňa", "Šafárikovo nám., Bratislava", 48.14325919, 17.10605714);
        await LinkKat(studna, "Jedlo");
        await LinkKat(studna, "Drinks", false);
        await LinkFil(studna, "Jedlo", "Vegan options", "Terasa", "Bezbariérový prístup", "Platba kartou", "Reštaurácia/Bistro", "A la carte");
        await LinkFil(studna, "Drinks", "Čapované pivo", "Víno", "Miešané drinky", "Fajčiarske dnu", "Terasa", "Late night", "Platba kartou");

        var palacinka = await AddMiesto("Palacinka Lacinka", "Obchodná, Bratislava", 48.1467, 17.1078);
        await LinkKat(palacinka, "Jedlo");
        await LinkFil(palacinka, "Jedlo", "Speciality coffee", "Terasa", "Platba kartou", "Reštaurácia/Bistro", "Food truck", "Denné menu", "A la carte", "Raňajky");

        var bistroHaj = await AddMiesto("Bistro Háj", "Háj, Bratislava", 48.16241505, 17.12390625);
        await LinkKat(bistroHaj, "Jedlo");
        await LinkKat(bistroHaj, "Kaviarne", false);
        await LinkFil(bistroHaj, "Jedlo", "Speciality coffee", "Vegan options", "Terasa", "Bezbariérový prístup", "Platba kartou", "Pet friendly", "Pekáreň", "Reštaurácia/Bistro", "Denné menu", "Raňajky");
        await LinkFil(bistroHaj, "Kaviarne", "Speciality coffee", "Vegan options", "Terasa", "Platba kartou", "Raňajky");

        var hanoi = await AddMiesto("Hanoi Garden", "Šancová, Bratislava", 48.1512, 17.1178);
        await LinkKat(hanoi, "Jedlo");
        await LinkFil(hanoi, "Jedlo", "Vegan options", "Terasa", "Bezbariérový prístup", "Platba kartou", "Reštaurácia/Bistro");

        var ejtytu = await AddMiesto("EjTyTu Streetfood", "Miletičova, Bratislava", 48.14896689, 17.13155574);
        await LinkKat(ejtytu, "Jedlo");
        await LinkKat(ejtytu, "Fashion", false);
        await LinkFil(ejtytu, "Jedlo", "Vegan options", "Platba kartou", "Food truck");

        // ── Hudba ────────────────────────────────────────────────────────────────
        var zrcadlovyHaj = await AddMiesto("Zrkadlový Háj", "Panónska cesta, Petržalka", 48.1234, 17.1093);
        await LinkKat(zrcadlovyHaj, "Hudba");
        await LinkKat(zrcadlovyHaj, "Kultúra", false);
        await LinkFil(zrcadlovyHaj, "Hudba", "Divadlo", "Koncertná hala", "Vstup kartou", "Elektronika", "DnB", "Acoustic", "Gitarová hudba", "Jazz", "Klasická hudba");
        await LinkFil(zrcadlovyHaj, "Kultúra", "Eventový space", "Kino");

        var fuga = await AddMiesto("Fuga", "Námestie SNP, Bratislava", 48.1441, 17.1098);
        await LinkKat(fuga, "Hudba");
        await LinkFil(fuga, "Hudba", "Koncertná hala", "Vstup kartou", "Elektronika", "DnB", "Acoustic", "Gitarová hudba");

        var kcNovaCvernovka = await AddMiesto("KC Nová Cvernovka", "Račianska, Bratislava", 48.18305933, 17.13178615);
        await LinkKat(kcNovaCvernovka, "Hudba");
        await LinkKat(kcNovaCvernovka, "Kultúra", false);
        await LinkKat(kcNovaCvernovka, "Outdoor", false);
        await LinkFil(kcNovaCvernovka, "Hudba", "Divadlo", "Koncertná hala", "Vstup kartou", "Elektronika", "DnB", "Acoustic", "Gitarová hudba", "Jazz");
        await LinkFil(kcNovaCvernovka, "Kultúra", "Galéria", "Eventový space", "Kino", "Divadlo", "Koncertná hala", "Diskusie", "Bezbariérové", "Pet friendly");
        await LinkFil(kcNovaCvernovka, "Outdoor", "Skatespot", "Outdoor fitko");

        var t3 = await AddMiesto("T3 Kultúrny Prostriedok", "Čajkovského, Bratislava", 48.1504, 17.1346);
        await LinkKat(t3, "Hudba");
        await LinkFil(t3, "Hudba", "Koncertná hala", "Vstup kartou", "Elektronika", "DnB", "Acoustic", "Gitarová hudba", "Jazz", "Klasická hudba");

        var pinkWhale = await AddMiesto("PinkWhale", "Stará Vajnorská, Bratislava", 48.1681, 17.1415);
        await LinkKat(pinkWhale, "Hudba");
        await LinkFil(pinkWhale, "Hudba", "Koncertná hala", "Vstup kartou", "Elektronika", "DnB", "Gitarová hudba");

        var a4 = await AddMiesto("A4", "Karpatská, Bratislava", 48.1463, 17.1035);
        await LinkKat(a4, "Hudba");
        await LinkKat(a4, "Kultúra", false);
        await LinkKat(a4, "Drinks", false);
        await LinkFil(a4, "Hudba", "Divadlo", "Koncertná hala", "Vstup kartou", "Elektronika", "Acoustic", "Jazz");
        await LinkFil(a4, "Kultúra", "Eventový space", "Diskusie");
        await LinkFil(a4, "Drinks", "Čapované pivo", "Víno", "Miešané drinky", "Terasa", "Late night", "Platba kartou");

        var kacecko = await AddMiesto("Kácéčko", "Kollárovo nám., Bratislava", 48.14519666, 17.11498054);
        await LinkKat(kacecko, "Hudba");
        await LinkKat(kacecko, "Drinks", false);
        await LinkFil(kacecko, "Hudba", "Koncertná hala", "Vstup kartou", "Elektronika", "DnB", "Acoustic", "Gitarová hudba", "Jazz");
        await LinkFil(kacecko, "Drinks", "Čapované pivo", "Víno", "Miešané drinky", "Terasa", "Late night", "Platba kartou");

        var subdeck = await AddMiesto("Subdeck", "Rybné nám., Bratislava", 48.1432, 17.1072);
        await LinkKat(subdeck, "Hudba");
        await LinkFil(subdeck, "Hudba", "Koncertná hala", "Vstup kartou", "Elektronika", "DnB", "Gitarová hudba");

        // ── Kultúra ──────────────────────────────────────────────────────────────
        var sng = await AddMiesto("SNG", "Rázusovo nábrežie, Bratislava", 48.1408, 17.1075);
        await LinkKat(sng, "Kultúra");
        await LinkFil(sng, "Kultúra", "Galéria", "Bezbariérové");

        var snd = await AddMiesto("SND", "Pribinova, Bratislava", 48.1404, 17.1139);
        await LinkKat(snd, "Kultúra");
        await LinkFil(snd, "Kultúra", "Divadlo", "Koncertná hala", "Bezbariérové");

        var kinoLumiere = await AddMiesto("Kino Lumiére", "Špitálska, Bratislava", 48.1477, 17.1068);
        await LinkKat(kinoLumiere, "Kultúra");
        await LinkFil(kinoLumiere, "Kultúra", "Kino", "Eventový space", "Bezbariérové");

        var kinoMladost = await AddMiesto("Kino Mladosť", "Hviezdoslavovo nám., Bratislava", 48.1414, 17.1084);
        await LinkKat(kinoMladost, "Kultúra");
        await LinkFil(kinoMladost, "Kultúra", "Kino");

        var staraTrznica = await AddMiesto("Stará Tržnica", "Námestie SNP, Bratislava", 48.1442, 17.1107);
        await LinkKat(staraTrznica, "Kultúra");
        await LinkKat(staraTrznica, "Hudba", false);
        await LinkFil(staraTrznica, "Kultúra", "Eventový space", "Diskusie", "Bezbariérové");
        await LinkFil(staraTrznica, "Hudba", "Koncertná hala", "Vstup kartou", "Jazz");

        var gmb = await AddMiesto("GMB", "Mirbachov palác, Bratislava", 48.1447, 17.1094);
        await LinkKat(gmb, "Kultúra");
        await LinkFil(gmb, "Kultúra", "Galéria", "Eventový space");

        // ── Šport ────────────────────────────────────────────────────────────────
        var skateparkSNP = await AddMiesto("Skatepark pod Mostom SNP", "Most SNP, Bratislava", 48.1395, 17.1057);
        await LinkKat(skateparkSNP, "Šport");
        await LinkKat(skateparkSNP, "Outdoor", false);
        await LinkFil(skateparkSNP, "Šport", "Skate / inline", "Free");
        await LinkFil(skateparkSNP, "Outdoor", "Skatespot");

        var skateparkGercenova = await AddMiesto("Skatepark Gercenova", "Gercenova, Petržalka", 48.1218, 17.1213);
        await LinkKat(skateparkGercenova, "Šport");
        await LinkKat(skateparkGercenova, "Outdoor", false);
        await LinkFil(skateparkGercenova, "Šport", "Skate / inline", "Free");
        await LinkFil(skateparkGercenova, "Outdoor", "Skatespot");

        var mudronka = await AddMiesto("Mudronka", "Horský park, Bratislava", 48.1581, 17.0892);
        await LinkKat(mudronka, "Šport");
        await LinkFil(mudronka, "Šport", "Svetlá", "Futbal", "Tenis", "Bežecká dráha", "Free");

        var strkovec = await AddMiesto("Štrkovecké jazero", "Štrkovecké jazero, Ružinov", 48.1502, 17.1352);
        await LinkKat(strkovec, "Šport");
        await LinkKat(strkovec, "Outdoor", false);
        await LinkFil(strkovec, "Šport", "Ping pong", "Free");
        await LinkFil(strkovec, "Outdoor", "Romantické miesta", "Park", "Kúpanie");

        var bowlingHviezda = await AddMiesto("Bowling Hviezda", "Bajkalská, Ružinov", 48.1502, 17.1469);
        await LinkKat(bowlingHviezda, "Šport");
        await LinkFil(bowlingHviezda, "Šport", "Bowling", "Platba kartou", "Treba sa objednať", "Hala / pod strechou");

        var mladaGarda = await AddMiesto("Športový areál Mladá Garda", "Račianska, Bratislava", 48.1635, 17.1189);
        await LinkKat(mladaGarda, "Šport");
        await LinkFil(mladaGarda, "Šport", "Svetlá", "Futbal", "Tenis", "Plaváreň", "Bežecká dráha", "Treba sa objednať");

        // ── Drinks ───────────────────────────────────────────────────────────────
        var thePeach = await AddMiesto("The Peach", "Obchodná, Bratislava", 48.1467, 17.1076);
        await LinkKat(thePeach, "Drinks");
        await LinkFil(thePeach, "Drinks", "Čapované pivo", "Víno", "Miešané drinky", "Fajčiarske dnu", "Terasa", "Late night");

        var cafeAxioma = await AddMiesto("Café Axioma", "Partizánska, Bratislava", 48.14746034, 17.11753096);
        await LinkKat(cafeAxioma, "Drinks");
        await LinkKat(cafeAxioma, "Kaviarne", false);
        await LinkFil(cafeAxioma, "Drinks", "Čapované pivo", "Víno", "Miešané drinky", "Fajčiarske dnu", "Terasa", "Late night");

        var nubaBar = await AddMiesto("Nuda Bar", "Kolárska, Bratislava", 48.1448, 17.1086);
        await LinkKat(nubaBar, "Drinks");
        await LinkFil(nubaBar, "Drinks", "Čapované pivo", "Víno", "Miešané drinky", "Terasa", "Platba kartou");

        var romerouvDom = await AddMiesto("Rómerov Dom", "Čulenova, Bratislava", 48.1475, 17.1122);
        await LinkKat(romerouvDom, "Drinks");
        await LinkKat(romerouvDom, "Hudba", false);
        await LinkFil(romerouvDom, "Drinks", "Čapované pivo", "Víno", "Miešané drinky", "Terasa");
        await LinkFil(romerouvDom, "Hudba", "Elektronika", "DnB", "Gitarová hudba");

        var viechaVinarov = await AddMiesto("Viecha malých vinárov", "Beblavého, Bratislava", 48.15083350, 17.13548322);
        await LinkKat(viechaVinarov, "Drinks");
        await LinkFil(viechaVinarov, "Drinks", "Čapované pivo", "Víno", "Terasa", "Platba kartou");

        var viechaHradom = await AddMiesto("Viecha pod hradom", "Beblavého, Bratislava", 48.1452, 17.1029);
        await LinkKat(viechaHradom, "Drinks");
        await LinkFil(viechaHradom, "Drinks", "Víno", "Terasa");

        var zalár = await AddMiesto("Žalár", "Štefanovičova, Bratislava", 48.1448, 17.1047);
        await LinkKat(zalár, "Drinks");
        await LinkFil(zalár, "Drinks", "Čapované pivo", "Víno", "Miešané drinky", "Fajčiarske dnu", "Late night");

        // ── Outdoor ──────────────────────────────────────────────────────────────
        var slavin = await AddMiesto("Slavín", "Slavín, Bratislava", 48.1549, 17.0978);
        await LinkKat(slavin, "Outdoor");
        await LinkFil(slavin, "Outdoor", "Skatespot", "Romantické miesta", "Vyhliadkové miesta");

        var kalvaria = await AddMiesto("Kalvária", "Kalvária, Bratislava", 48.1524, 17.0975);
        await LinkKat(kalvaria, "Outdoor");
        await LinkFil(kalvaria, "Outdoor", "Romantické miesta", "Vyhliadkové miesta", "Park", "Les / lesopark");

        var partizanskaLuka = await AddMiesto("Partizánska lúka", "Partizánska lúka, Bratislava", 48.1552, 17.0996);
        await LinkKat(partizanskaLuka, "Outdoor");
        await LinkFil(partizanskaLuka, "Outdoor", "Romantické miesta", "Opekanie", "Vyhliadkové miesta", "Park", "Les / lesopark");
        await LinkFil(partizanskaLuka, "Šport", "Ping pong", "Free");

        var lido = await AddMiesto("Lido / Elýzium", "Tyršovo nábrežie, Bratislava", 48.1304, 17.0888);
        await LinkKat(lido, "Outdoor");
        await LinkFil(lido, "Outdoor", "Romantické miesta", "Opekanie", "Park", "Les / lesopark", "Kúpanie");

        var botanickaZahrada = await AddMiesto("Botanická záhrada UK", "Dúbravská cesta, Bratislava", 48.1638, 17.0756);
        await LinkKat(botanickaZahrada, "Outdoor");
        await LinkFil(botanickaZahrada, "Outdoor", "Romantické miesta", "Park");

        var prezidentska = await AddMiesto("Prezidentská záhrada", "Hodžovo nám., Bratislava", 48.1460, 17.1011);
        await LinkKat(prezidentska, "Outdoor");
        await LinkFil(prezidentska, "Outdoor", "Romantické miesta", "Park");

        var devinskaKobyla = await AddMiesto("Devínska Kobyla", "Devín, Bratislava", 48.1761, 16.9940);
        await LinkKat(devinskaKobyla, "Outdoor");
        await LinkFil(devinskaKobyla, "Outdoor", "Romantické miesta", "Opekanie", "Les / lesopark");

        // ── Fashion ──────────────────────────────────────────────────────────────
        var textileParickova = await AddMiesto("Textile House Páričkova", "Páričkova, Bratislava", 48.14800158, 17.12735804);
        await LinkKat(textileParickova, "Fashion");
        await LinkFil(textileParickova, "Fashion", "Second hand");

        var vintageFrantiskanske = await AddMiesto("Vintage shop Františkánske", "Františkánske nám., Bratislava", 48.14410007, 17.10794299);
        await LinkKat(vintageFrantiskanske, "Fashion");
        await LinkFil(vintageFrantiskanske, "Fashion", "Second hand");

        var buffetClothing = await AddMiesto("Buffet Clothing", "Obchodná, Bratislava", 48.14421481, 17.11198868);
        await LinkKat(buffetClothing, "Fashion");
        await LinkFil(buffetClothing, "Fashion", "Nové");

        var slavicaStore = await AddMiesto("Slávica local design", "Obchodná, Bratislava", 48.14423296, 17.11204012);
        await LinkKat(slavicaStore, "Fashion");
        await LinkFil(slavicaStore, "Fashion", "Nové");

        var genesis = await AddMiesto("Genesis Nedbalova", "Nedbalova, Bratislava", 48.14479134, 17.11069578);
        await LinkKat(genesis, "Fashion");
        await LinkFil(genesis, "Fashion", "Second hand");

        // ── Craft ─────────────────────────────────────────────────────────────────
        var labster = await AddMiesto("Labster", "Tomášikova, Bratislava", 48.1565, 17.1428);
        await LinkKat(labster, "Craft");
        await LinkFil(labster, "Craft", "Fotolab");

        var fotosk = await AddMiesto("Foto.sk", "Obchodná, Bratislava", 48.1462, 17.1071);
        await LinkKat(fotosk, "Craft");
        await LinkFil(fotosk, "Craft", "Fotolab");

        var skycolorfoto = await AddMiesto("Skycolorfoto", "Vajnorská, Bratislava", 48.1651, 17.1425);
        await LinkKat(skycolorfoto, "Craft");
        await LinkFil(skycolorfoto, "Craft", "Fotolab");

        var nemeck0 = await AddMiesto("Nemeck0", "Námestie SNP, Bratislava", 48.1443, 17.1095);
        await LinkKat(nemeck0, "Craft");
        await LinkFil(nemeck0, "Craft", "Streetart shop");

        var makerSpace = await AddMiesto("Maker Space", "Botanická, Bratislava", 48.1558, 17.0673);
        await LinkKat(makerSpace, "Craft");
        await LinkFil(makerSpace, "Craft", "Zdieľaná dielňa");

        // ── Knihy ─────────────────────────────────────────────────────────────────
        var artforum = await AddMiesto("Artforum", "Kozia, Bratislava", 48.1453, 17.1118);
        await LinkKat(artforum, "Knihy");
        await LinkFil(artforum, "Knihy", "Kníhkupectvo");

        var brot = await AddMiesto("Brot", "Sedlárska, Bratislava", 48.1438, 17.1115);
        await LinkKat(brot, "Knihy");
        await LinkFil(brot, "Knihy", "Kníhkupectvo", "Antikvariát");

        var kniznicaKapucinska = await AddMiesto("Staromestská knižnica Kapucínska", "Kapucínska, Bratislava", 48.1458, 17.1074);
        await LinkKat(kniznicaKapucinska, "Knihy");
        await LinkFil(kniznicaKapucinska, "Knihy", "Knižnica", "Čitáreň", "Spoločenské hry");

        var univKniznica = await AddMiesto("Univerzitná knižnica", "Klariská, Bratislava", 48.1444, 17.1060);
        await LinkKat(univKniznica, "Knihy");
        await LinkFil(univKniznica, "Knihy", "Knižnica", "Čitáreň");

        var podZampou = await AddMiesto("Klub pod Lampou", "Štefánikova, Bratislava", 48.1463, 17.1006);
        await LinkKat(podZampou, "Knihy");
        await LinkKat(podZampou, "Kultúra", false);
        await LinkFil(podZampou, "Knihy", "Kníhkupectvo");
        await LinkFil(podZampou, "Kultúra", "Eventový space", "Diskusie");
    }

    private async Task SeedEventsAsync()
    {
        using var conn = factory.Create();

        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TypPodujatia");
        if (count > 0) return;

        // ── Typy podujatí ────────────────────────────────────────────────────────
        var typy = new[] {
            "Koncert", "Festival", "Výstava", "Workshop", "Divadelné predstavenie",
            "Filmové premietanie", "Diskusia / Panel", "Vernisáž", "Trh / Market",
            "Šport. podujatie", "Párty / Rave", "Stand-up", "Otvorené dvere", "Plein air"
        };
        var typIds = new Dictionary<string, int>();
        foreach (var t in typy)
        {
            var id = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO TypPodujatia (Nazov) VALUES (@Nazov);
                SELECT last_insert_rowid();", new { Nazov = t });
            typIds[t] = id;
        }

        // ── Event filtre ─────────────────────────────────────────────────────────
        var filtreNazvy = new[] {
            "Zadarmo", "Platené", "Vonku", "V interiéri", "S lístkom", "Bez lístka",
            "Pre deti", "18+", "Pet friendly", "Vegan-friendly", "Bezbariérový prístup",
            "Registrácia nutná", "Online / Hybrid"
        };
        var filtreIds = new Dictionary<string, int>();
        foreach (var f in filtreNazvy)
        {
            var id = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO EventFilter (Nazov) VALUES (@Nazov);
                SELECT last_insert_rowid();", new { Nazov = f });
            filtreIds[f] = id;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        async Task<int?> MId(string nazov) =>
            await conn.QuerySingleOrDefaultAsync<int?>(
                "SELECT Id FROM Miesto WHERE Nazov = @Nazov", new { Nazov = nazov });

        async Task<int> AddEvent(string nazov, string popis, string datumOd, string? datumDo,
            string? adresa, double? lat, double? lng, int? miestoId) =>
            await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO Podujatie (Nazov, Popis, DatumOd, DatumDo, Adresa, Lat, Lng, MiestoId)
                VALUES (@Nazov, @Popis, @DatumOd, @DatumDo, @Adresa, @Lat, @Lng, @MiestoId);
                SELECT last_insert_rowid();",
                new { Nazov = nazov, Popis = popis, DatumOd = datumOd, DatumDo = datumDo,
                      Adresa = adresa, Lat = lat, Lng = lng, MiestoId = miestoId });

        async Task LinkTyp(int podId, string typNazov)
        {
            if (typIds.TryGetValue(typNazov, out var tid))
                await conn.ExecuteAsync(
                    "INSERT OR IGNORE INTO PodujatieTyp (PodujatieId, TypId) VALUES (@PodujatieId, @TypId)",
                    new { PodujatieId = podId, TypId = tid });
        }

        async Task LinkFiltreE(int podId, params string[] nazvy)
        {
            foreach (var f in nazvy)
                if (filtreIds.TryGetValue(f, out var fid))
                    await conn.ExecuteAsync(
                        "INSERT OR IGNORE INTO PodujatieFilter (PodujatieId, FilterId) VALUES (@PodujatieId, @FilterId)",
                        new { PodujatieId = podId, FilterId = fid });
        }

        // ── Načítaj ID miest ─────────────────────────────────────────────────────
        var fugaId        = await MId("Fuga");
        var subdeckId     = await MId("Subdeck");
        var sngId         = await MId("SNG");
        var sndId         = await MId("SND");
        var lumiereId     = await MId("Kino Lumiére");
        var gmbId         = await MId("GMB");
        var kcId          = await MId("KC Nová Cvernovka");
        var pinkWhaleId   = await MId("PinkWhale");
        var a4Id          = await MId("A4");
        var kaceckoId     = await MId("Kácéčko");
        var labsterId     = await MId("Labster");
        var fotoskId      = await MId("Foto.sk");
        var mudronkaId    = await MId("Mudronka");
        var slavinId      = await MId("Slavín");
        var lidoId        = await MId("Lido / Elýzium");
        var jungleId      = await MId("Jungle Roastery");
        var trznicaId     = await MId("Stará Tržnica");
        var ejtytuId      = await MId("EjTyTu Streetfood");
        var artforumId    = await MId("Artforum");
        var buffetId      = await MId("Buffet Clothing");

        // ── 25 podujatí ──────────────────────────────────────────────────────────
        var e1 = await AddEvent("Jazz večer vo Fuge",
            "Večer plný jazzovej improvizácie s hosťami z celého Slovenska.",
            "2026-05-03", null, "Námestie SNP, Bratislava", 48.1441, 17.1098, fugaId);
        await LinkTyp(e1, "Koncert");
        await LinkFiltreE(e1, "Platené", "S lístkom", "V interiéri");

        var e2 = await AddEvent("Subdeck: Drum & Bass Night",
            "Nočná DnB session s local DJs a hosťujúcim umelcom z Prahy.",
            "2026-05-10", null, "Rybné nám., Bratislava", 48.1432, 17.1072, subdeckId);
        await LinkTyp(e2, "Párty / Rave");
        await LinkFiltreE(e2, "18+", "S lístkom", "Platené", "V interiéri");

        var e3 = await AddEvent("Slovenský dizajn 2026",
            "Výstava najlepšieho slovenského produktového a grafického dizajnu za posledné desaťročie.",
            "2026-05-15", "2026-07-15", "Rázusovo nábrežie, Bratislava", 48.1408, 17.1075, sngId);
        await LinkTyp(e3, "Výstava");
        await LinkFiltreE(e3, "Platené", "V interiéri", "Bezbariérový prístup");

        var e4 = await AddEvent("Workshop: Street Photography",
            "Celodenný workshop pouličnej fotografie so skúseným fotografom. Vlastný fotoaparát nutný.",
            "2026-05-16", null, "Tomášikova, Bratislava", 48.1565, 17.1428, labsterId);
        await LinkTyp(e4, "Workshop");
        await LinkFiltreE(e4, "Platené", "V interiéri", "Registrácia nutná");

        var e5 = await AddEvent("Bratislava Design Week",
            "Päťdňový festival dizajnu, architektúry a vizuálnej kultúry v srdci mesta.",
            "2026-05-18", "2026-05-24", "Námestie SNP, Bratislava", 48.1442, 17.1107, trznicaId);
        await LinkTyp(e5, "Festival");
        await LinkFiltreE(e5, "Zadarmo", "V interiéri", "Pre deti", "Bezbariérový prístup");

        var e6 = await AddEvent("Hamlet – SND",
            "Shakespearova tragédia v réžii renomovaného slovenského režiséra.",
            "2026-05-20", null, "Pribinova, Bratislava", 48.1404, 17.1139, sndId);
        await LinkTyp(e6, "Divadelné predstavenie");
        await LinkFiltreE(e6, "Platené", "S lístkom", "V interiéri", "Bezbariérový prístup");

        var e7 = await AddEvent("Cannes 2026: Výber filmov",
            "Špeciálna projekcia výberu z programu Filmového festivalu v Cannes.",
            "2026-05-22", null, "Špitálska, Bratislava", 48.1477, 17.1068, lumiereId);
        await LinkTyp(e7, "Filmové premietanie");
        await LinkFiltreE(e7, "Platené", "S lístkom", "V interiéri");

        var e8 = await AddEvent("Vernisáž: Urban Fragments",
            "Otvorenie výstavy fotografií dokumentujúcich premenu bratislavských štvrtí.",
            "2026-05-28", null, "Mirbachov palác, Bratislava", 48.1447, 17.1094, gmbId);
        await LinkTyp(e8, "Vernisáž");
        await LinkFiltreE(e8, "Zadarmo", "V interiéri", "Bezbariérový prístup");

        var e9 = await AddEvent("Trh mladých dizajnérov",
            "Mesačný trh s tvorbou lokálnych dizajnérov, ilustrátorov a remeselníkov.",
            "2026-06-01", null, "Račianska, Bratislava", 48.1579, 17.1361, kcId);
        await LinkTyp(e9, "Trh / Market");
        await LinkFiltreE(e9, "Zadarmo", "Vonku", "Pre deti", "Pet friendly");

        var e10 = await AddEvent("PinkWhale: Indie Noc",
            "Intenzívny koncertný večer so štyrmi indie kapelami v unikátnom priestore.",
            "2026-06-05", null, "Stará Vajnorská, Bratislava", 48.1681, 17.1415, pinkWhaleId);
        await LinkTyp(e10, "Koncert");
        await LinkFiltreE(e10, "Platené", "S lístkom", "V interiéri", "18+");

        var e11 = await AddEvent("Stand-up Comedy Night",
            "Večer so slovenskými stand-up komikmi a prekvapivými hosťami.",
            "2026-06-07", null, "Kollárovo nám., Bratislava", 48.1498, 17.1063, kaceckoId);
        await LinkTyp(e11, "Stand-up");
        await LinkFiltreE(e11, "Platené", "S lístkom", "18+", "V interiéri");

        var e12 = await AddEvent("Workshop: Filmový fotobooth",
            "Naučíš sa obsluhovať analógový fotoaparát a vyvoláš si vlastné filmy v labre.",
            "2026-06-10", null, "Obchodná, Bratislava", 48.1462, 17.1071, fotoskId);
        await LinkTyp(e12, "Workshop");
        await LinkFiltreE(e12, "Platené", "Registrácia nutná", "V interiéri");

        var e13 = await AddEvent("Otvorené dvere: KC Nová Cvernovka",
            "Deň otvorených dverí: prehliadka priestoru, workshopy zadarmo a živá hudba na dvore.",
            "2026-06-14", null, "Račianska, Bratislava", 48.1579, 17.1361, kcId);
        await LinkTyp(e13, "Otvorené dvere");
        await LinkFiltreE(e13, "Zadarmo", "Vonku", "Pet friendly", "Pre deti");

        var e14 = await AddEvent("Plein Air: Slavín",
            "Skupinové maľovanie en plein air s výhľadom na celú Bratislavu. Farby a plátno k dispozícii.",
            "2026-06-20", null, "Slavín, Bratislava", 48.1549, 17.0978, slavinId);
        await LinkTyp(e14, "Plein air");
        await LinkFiltreE(e14, "Zadarmo", "Vonku", "Registrácia nutná");

        var e15 = await AddEvent("Streetball Turnaj",
            "Trojčlenné tímy, dve ihriská, jeden víťaz. Registruj sa a bojuj o pohár z Mudronky.",
            "2026-06-21", null, "Horský park, Bratislava", 48.1581, 17.0892, mudronkaId);
        await LinkTyp(e15, "Šport. podujatie");
        await LinkFiltreE(e15, "Zadarmo", "Vonku", "Registrácia nutná");

        var e16 = await AddEvent("Diskusia: Budúcnosť Bratislavy",
            "Panelová diskusia s architektmi, urbanistami a zástupcami mesta o tom, aké mesto chceme.",
            "2026-06-25", null, "Karpatská, Bratislava", 48.1463, 17.1035, a4Id);
        await LinkTyp(e16, "Diskusia / Panel");
        await LinkFiltreE(e16, "Zadarmo", "V interiéri", "Bezbariérový prístup");

        var e17 = await AddEvent("Pohoda Warm-Up Party",
            "Predpohôdový večer s najlepším music programom a food corner priamo v Starej Tržnici.",
            "2026-07-04", null, "Námestie SNP, Bratislava", 48.1442, 17.1107, trznicaId);
        await LinkTyp(e17, "Festival");
        await LinkFiltreE(e17, "Platené", "S lístkom", "V interiéri", "18+");

        var e18 = await AddEvent("Acoustic Session: Jungle Roastery",
            "Neformálny akustický koncert v útulnej kaviarni. Vstup voľný, odporúča sa rezervácia miesta.",
            "2026-07-06", null, "Dostojevského rad, Bratislava", 48.1452, 17.1086, jungleId);
        await LinkTyp(e18, "Koncert");
        await LinkFiltreE(e18, "Zadarmo", "Bez lístka", "V interiéri");

        var e19 = await AddEvent("Mladí umelci SK 2026",
            "Letná skupinová výstava absolventov VŠVU a AFAD – maľba, socha, digitálne médiá.",
            "2026-07-10", "2026-08-30", "Mirbachov palác, Bratislava", 48.1447, 17.1094, gmbId);
        await LinkTyp(e19, "Výstava");
        await LinkFiltreE(e19, "Platené", "V interiéri");

        var e20 = await AddEvent("Letné kino: Lido",
            "Vonkajšie premietanie filmov každý utorok a piatok pri Dunaji. Bring your blanket!",
            "2026-07-15", "2026-08-29", "Tyršovo nábrežie, Bratislava", 48.1304, 17.0888, lidoId);
        await LinkTyp(e20, "Filmové premietanie");
        await LinkFiltreE(e20, "Zadarmo", "Vonku", "Pet friendly", "Pre deti");

        var e21 = await AddEvent("Letný Rave: Subdeck",
            "Dlhá noc elektronickej hudby so zahraničnými headlinermi.",
            "2026-07-18", null, "Rybné nám., Bratislava", 48.1432, 17.1072, subdeckId);
        await LinkTyp(e21, "Párty / Rave");
        await LinkFiltreE(e21, "18+", "Platené", "S lístkom", "V interiéri");

        var e22 = await AddEvent("Vintage & Craft Trh",
            "Mesačný víkendový trh s vintage oblečením, handmade šperkami a upcyklovanými predmetmi.",
            "2026-07-25", null, "Miletičova, Bratislava", 48.1378, 17.1289, ejtytuId);
        await LinkTyp(e22, "Trh / Market");
        await LinkFiltreE(e22, "Zadarmo", "Vonku", "Vegan-friendly");

        var e23 = await AddEvent("Vernisáž: Štrkovecké zrkadlá",
            "Fotografická výstava zachytávajúca jazero v rôznych ročných obdobiach.",
            "2026-08-05", null, "Kozia, Bratislava", 48.1453, 17.1118, artforumId);
        await LinkTyp(e23, "Vernisáž");
        await LinkFiltreE(e23, "Zadarmo", "V interiéri");

        var e24 = await AddEvent("Workshop: Upcycling módy",
            "Nauč sa premeniť staré oblečenie na nové kúsky. Materiál zabezpečený.",
            "2026-08-12", null, "Obchodná, Bratislava", 48.1462, 17.1074, buffetId);
        await LinkTyp(e24, "Workshop");
        await LinkFiltreE(e24, "Platené", "Registrácia nutná", "V interiéri");

        var e25 = await AddEvent("Street Food Weekend",
            "Dvojdňový festival street food z celého sveta s živou hudbou a detským kútom.",
            "2026-08-22", "2026-08-23", "Račianska, Bratislava", 48.1579, 17.1361, kcId);
        await LinkTyp(e25, "Festival");
        await LinkFiltreE(e25, "Zadarmo", "Vonku", "Pre deti", "Pet friendly", "Vegan-friendly");
    }
}
