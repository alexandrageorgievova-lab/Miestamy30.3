using Dapper;
using Miestamy30._3.Repositories.Interfaces;

namespace Miestamy30._3.Data;

public class DatabaseInitializer(DbConnectionFactory factory, IKategoriaRepository kategoriaRepo,
    IFilterRepository filterRepo, IMiestoRepository miestoRepo)
{
    public async Task InitializeAsync()
    {
        await CreateSchemaAsync();
        await MigrateSchemaAsync();
        await SeedAsync();
        await SeedEventsAsync();
    }

    private async Task MigrateSchemaAsync()
    {
        using var conn = factory.Create();
        if (IsPostgres())
        {
            await conn.ExecuteAsync("ALTER TABLE Podujatie ADD COLUMN IF NOT EXISTS ImageUrl TEXT");
            await conn.ExecuteAsync("ALTER TABLE Podujatie ADD COLUMN IF NOT EXISTS SourceUrl TEXT");
            await conn.ExecuteAsync("ALTER TABLE Miesto ADD COLUMN IF NOT EXISTS ImageUrl TEXT");
        }
        else
        {
            try { await conn.ExecuteAsync("ALTER TABLE Podujatie ADD COLUMN ImageUrl TEXT"); } catch { }
            try { await conn.ExecuteAsync("ALTER TABLE Podujatie ADD COLUMN SourceUrl TEXT"); } catch { }
            try { await conn.ExecuteAsync("ALTER TABLE Miesto ADD COLUMN ImageUrl TEXT"); } catch { }
        }

        // Seed image URLs for known venues (idempotent — only sets where still NULL)
        var imageSeeds = new[]
        {
            // ── Previously seeded ─────────────────────────────────────────────────
            ("KC Nová Cvernovka",  "https://novacvernovka.eu/wp-content/plugins/hk-cvernovka/images/fb-meta-univerzal.jpg"),
            ("A4",                 "https://a4.sk/wp-content/uploads/2021/04/IMG_5558.jpg"),
            ("Stará Tržnica",      "https://staratrznica.sk/assets/img/og.jpg"),
            ("T3 Kultúrny Prostriedok", "https://t3.sk/og-image.png"),
            ("Subdeck",            "https://subdeck.sk/ogimage.png"),
            ("Mogg",               "https://mogg-bratislava.vercel.app/og-image.jpg"),
            // ── Kaviarne ──────────────────────────────────────────────────────────
            ("Blue Mondays",       "https://bluemondays.sk/cdn/shop/collections/olivier-collet-1bRqiHGtPK0-unsplash.jpg?v=1762096009&width=3840"),
            ("LOT Roastery",       "https://www.lotroastery.com/wp-content/uploads/2025/06/IMG_4192-Edit.webp"),
            ("Matsu",              "https://www.matsu-matcha.com/wp-content/uploads/2025/08/Rectangle-2527-780x1024.png"),
            ("Baryk",              "https://img02.restaurantguru.com/cc58-Restaurant-Baryk-food.jpg"),
            ("Kaviareň Vták",      "https://www.visitbratislava.com/wp-content/uploads/2023/04/Vtak-interier-fb-800x800.jpg"),
            ("Temný Ost Block",    "https://www.visitbratislava.com/wp-content/uploads/2023/04/Ost-fb-740x800.jpg"),
            ("Oproti",             "https://www.oproti.sk/wp-content/uploads/2025/11/48f5e61b-99ff-4b8b-8520-b47895f0db54.jpg"),
            ("Mandľa",             "https://mandlove.com/wp-content/uploads/2021/11/IMG-7076-1200x901.jpg"),
            // ── Jedlo ─────────────────────────────────────────────────────────────
            ("Otto!",              "https://images.squarespace-cdn.com/content/v1/5ed792068735f36f2afd13de/83727a70-3f25-4b95-b3b6-5505e85389ab/Otto.7-22_byLousy-1.jpg"),
            ("Ramen Kazu",         "https://ramenkazu.sk/wp-content/uploads/2026/01/ramen-kazu.webp"),
            ("Studňa",             "https://img02.restaurantguru.com/c4f3-Studna-umelecky-klub-street-food-craft-beer-a-borovicka-bar-dishes.jpg"),
            ("Hanoi Garden",       "https://lh3.googleusercontent.com/sitesv/AA5AbUAk9T-3nRFzKF-uawlFsuELGr4JODcRUEOCSp7uJ2UMGiX0deIzxCLlpou3xZsgOUxuw3r9XBlR-Ck9TXGHGqrrr-KC2WbZ2QROMJzJ-UYQrnT844aVrTlBz6xS9WlY42p46uavQ9INRRiyWv8_V2MMMWQF4ynxVFL0vFbMTifs9SCtli_fVLQZkVxadB9pLZQqmLv06upBcdaLiixJOLeVwa-PwVqDrtca=w1280"),
            ("EjTyTu Streetfood",  "https://www.visitbratislava.com/wp-content/uploads/2024/08/20240828_121430-800x370.jpg"),
            // ── Hudba ─────────────────────────────────────────────────────────────
            ("Zrkadlový Háj",      "https://www.petrzalka.sk/uploads/prenajmy/hladisko-velka-sala-dk-zrkadlovy-haj.jpg"),
            ("Fuga",               "https://fuga.forumabsurdum.sk/wp-content/uploads/624590299_1335960868571477_6799567575291985200_n-360x202.jpg"),
            ("PinkWhale",          "https://www.visitbratislava.com/wp-content/uploads/2023/06/Pink-whale-z-ich-webu.jpg"),
            ("Kácéčko",            "https://www.visitbratislava.com/wp-content/uploads/2023/11/fokty_kc_dunaj-21.jpg"),
            // ── Kultúra ───────────────────────────────────────────────────────────
            ("SNG",                "https://upload.wikimedia.org/wikipedia/commons/9/98/Slovak_National_Gallery.JPG"),
            ("SND",                "https://upload.wikimedia.org/wikipedia/commons/thumb/1/10/Bratyslawa_Teatr_Narodowy.jpg/250px-Bratyslawa_Teatr_Narodowy.jpg"),
            ("Kino Mladosť",       "https://www.visitbratislava.com/wp-content/uploads/2014/12/ilustracne_foto_cinemax_kinosala-800x533.jpg"),
            ("GMB",                "https://cdn-api.bratislava.sk/strapi-city-gallery/upload/FSVD_1714_50aeda679b.jpg"),
            // ── Šport ─────────────────────────────────────────────────────────────
            ("Skatepark pod Mostom SNP", "https://mib.sk/wp-content/uploads/2024/05/20240525_091151000_iOS-1024x683.jpg"),
            ("Skatepark Gercenova", "https://cdn.sita.sk/sites/3/2017/11/miniskatepark-1-640x361.jpg"),
            ("Mudronka",           "https://www.naspark.sk/en/photoloader/638/2.jpg/0x0/_1920x650"),
            ("Štrkovecké jazero",  "https://upload.wikimedia.org/wikipedia/commons/7/77/Slovakia_Bratislava_Strkovec1.JPG"),
            ("Bowling Hviezda",    "https://bnc-sk.sk/media/preview/blocks/4c9a8d2eeac88fb625a71204c6bc28a5/image/52bc0eaad79e7824126437933db787a5/huQBoxsLZ1mUwHk6OYk6rRdpq0NvaPmVTndeY9IW.webp?w=2878&h=1924"),
            ("Športový areál Mladá Garda", "https://garda.sk/wp-content/uploads/2022/03/SportoviskoMG.jpg"),
            // ── Drinks ────────────────────────────────────────────────────────────
            ("Café Axioma",        "https://www.visitbratislava.com/wp-content/uploads/2015/05/axioma1-800x533.jpg"),
            ("Nuda Bar",           "https://images.squarespace-cdn.com/content/v1/64cbbee0f51cff10af4e81a4/e01d2ca1-3cb0-4cfc-a4a7-402ad1d4b52a/DSCF1068.jpg"),
            ("Viecha malých vinárov", "https://www.visitbratislava.com/wp-content/uploads/2016/05/viecha2-800x407.jpg"),
            // ── Outdoor ───────────────────────────────────────────────────────────
            ("Slavín",             "https://upload.wikimedia.org/wikipedia/commons/thumb/6/60/Monumento_a_Slav%C3%ADn%2C_Bratislava%2C_Eslovaquia%2C_2020-02-01%2C_DD_13.jpg/330px-Monumento_a_Slav%C3%ADn%2C_Bratislava%2C_Eslovaquia%2C_2020-02-01%2C_DD_13.jpg"),
            ("Partizánska lúka",   "https://www.visitbratislava.com/wp-content/uploads/2015/04/Kovac_BA_Nove-Mesto_Lesopark_Les_Bus_Doprava__nevyhradna-licencia_1920x1280.jpg"),
            ("Lido / Elýzium",     "https://www.visitbratislava.com/wp-content/uploads/2020/06/Molnar_Dunaj_Magio-pl%C3%A1%C5%BE_BA-hrad_deti_family_vyhradna-licencia-800x584.jpg"),
            ("Botanická záhrada UK", "https://uniba.sk/typo3temp/pics/jar_BZUK_927697052a.jpg"),
            ("Prezidentská záhrada", "https://www.prezident.sk/upload-files/pages/xniwjhp3rajxtkprxwybxnckzazqj07lgtctoyph.jpeg"),
            ("Devínska Kobyla",    "https://upload.wikimedia.org/wikipedia/commons/thumb/d/d2/Devinska_Kobyla_02.jpg/330px-Devinska_Kobyla_02.jpg"),
            // ── Fashion ───────────────────────────────────────────────────────────
            ("Textile House Páričkova", "https://textilehouse.sk/wp-content/uploads/2026/04/SK-768x1088.jpg"),
            ("Vintage shop Františkánske", "https://textilehouse.sk/wp-content/uploads/2018/08/vint.jpg"),
            ("Buffet Clothing",    "https://www.buffetclothing.com/cdn/shop/files/IMG_8574.jpg?v=1770648376"),
            ("Slávica local design", "https://www.visitbratislava.com/wp-content/uploads/2015/06/sl%C3%A1vica-2-533x800.jpg"),
            // ── Craft ─────────────────────────────────────────────────────────────
            ("Labster",            "https://www.labster.sk/wp-content/uploads/2025/09/webskuska3-768x169.jpg"),
            ("Foto.sk",            "https://foto.sk/wp-content/uploads/2023/09/4196a-1657x2048-1-829x1024.jpg"),
            ("Nemeck0",            "https://www.nemeck0.sk/wp-content/uploads/2023/02/DSC00536-750x1000.jpg"),
            ("Maker Space",        "https://static.wixstatic.com/media/11062b_ec32664ac38e4bbabd76f46838541c64~mv2.jpg/v1/fill/w_288,h_192,al_c,q_80,usm_0.66_1.00_0.01,blur_2,enc_avif,quality_auto/11062b_ec32664ac38e4bbabd76f46838541c64~mv2.jpg"),
            // ── Knihy ─────────────────────────────────────────────────────────────
            ("Artforum",           "https://static.artforum.sk/media/homepage/1035_600.jpg"),
            ("Brot",               "https://brot.sk/cdn/shop/files/Brot_Books_Deli_logo_horizontal_c2e94fc5-34c2-49a4-bf25-47592dde9f58.png?v=1638786944&width=1000"),
        };
        foreach (var (nazov, url) in imageSeeds)
            await conn.ExecuteAsync(
                "UPDATE Miesto SET ImageUrl = @Url WHERE Nazov = @Nazov AND ImageUrl IS NULL",
                new { Url = url, Nazov = nazov });
    }

    private bool IsPostgres() => factory.IsPostgres;

    private async Task CreateSchemaAsync()
    {
        using var conn = factory.Create();

        if (IsPostgres())
        {
            // PostgreSQL — execute each statement separately (no PRAGMA, use SERIAL)
            var statements = new[]
            {
                @"CREATE TABLE IF NOT EXISTS Kategoria (
                    Id    SERIAL PRIMARY KEY,
                    Nazov TEXT   NOT NULL UNIQUE
                )",
                @"CREATE TABLE IF NOT EXISTS Filter (
                    Id          SERIAL PRIMARY KEY,
                    Nazov       TEXT    NOT NULL,
                    KategoriaId INTEGER NOT NULL REFERENCES Kategoria(Id),
                    UNIQUE (Nazov, KategoriaId)
                )",
                @"CREATE TABLE IF NOT EXISTS Miesto (
                    Id     SERIAL PRIMARY KEY,
                    Nazov  TEXT   NOT NULL UNIQUE,
                    Adresa TEXT,
                    Lat    DOUBLE PRECISION,
                    Lng    DOUBLE PRECISION,
                    Popis  TEXT,
                    WebUrl TEXT
                )",
                @"CREATE TABLE IF NOT EXISTS MiestoKategoria (
                    MiestoId    INTEGER NOT NULL REFERENCES Miesto(Id)    ON DELETE CASCADE,
                    KategoriaId INTEGER NOT NULL REFERENCES Kategoria(Id),
                    JeHlavna    BOOLEAN NOT NULL DEFAULT FALSE,
                    PRIMARY KEY (MiestoId, KategoriaId)
                )",
                @"CREATE TABLE IF NOT EXISTS MiestoFilter (
                    MiestoId INTEGER NOT NULL REFERENCES Miesto(Id)  ON DELETE CASCADE,
                    FilterId INTEGER NOT NULL REFERENCES Filter(Id),
                    PRIMARY KEY (MiestoId, FilterId)
                )",
                @"CREATE TABLE IF NOT EXISTS TypPodujatia (
                    Id    SERIAL PRIMARY KEY,
                    Nazov TEXT   NOT NULL UNIQUE
                )",
                @"CREATE TABLE IF NOT EXISTS EventFilter (
                    Id    SERIAL PRIMARY KEY,
                    Nazov TEXT   NOT NULL UNIQUE
                )",
                @"CREATE TABLE IF NOT EXISTS Podujatie (
                    Id       SERIAL PRIMARY KEY,
                    Nazov    TEXT NOT NULL,
                    Popis    TEXT,
                    DatumOd  TEXT NOT NULL,
                    DatumDo  TEXT,
                    Adresa   TEXT,
                    Lat      DOUBLE PRECISION,
                    Lng      DOUBLE PRECISION,
                    MiestoId INTEGER REFERENCES Miesto(Id)
                )",
                @"CREATE TABLE IF NOT EXISTS PodujatieTyp (
                    PodujatieId INTEGER NOT NULL REFERENCES Podujatie(Id)   ON DELETE CASCADE,
                    TypId       INTEGER NOT NULL REFERENCES TypPodujatia(Id),
                    PRIMARY KEY (PodujatieId, TypId)
                )",
                @"CREATE TABLE IF NOT EXISTS PodujatieFilter (
                    PodujatieId INTEGER NOT NULL REFERENCES Podujatie(Id) ON DELETE CASCADE,
                    FilterId    INTEGER NOT NULL REFERENCES EventFilter(Id),
                    PRIMARY KEY (PodujatieId, FilterId)
                )"
            };
            foreach (var sql in statements)
                await conn.ExecuteAsync(sql);
        }
        else
        {
            // SQLite — original schema
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
    }

    private async Task SeedAsync()
    {
        using var checkConn = factory.Create();
        var kategoriaCount = await checkConn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Kategoria");
        if (kategoriaCount > 0)
        {
            var miestoKategoriaCount = await checkConn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM MiestoKategoria");
            if (miestoKategoriaCount > 0) return; // fully seeded

            // Partial seed: categories/places exist but links are missing — wipe and re-seed
            // Must null out MiestoId on Podujatie first to avoid FK violation when deleting Miesto
            await checkConn.ExecuteAsync("UPDATE Podujatie SET MiestoId = NULL");
            await checkConn.ExecuteAsync("DELETE FROM MiestoFilter");
            await checkConn.ExecuteAsync("DELETE FROM MiestoKategoria");
            await checkConn.ExecuteAsync("DELETE FROM Miesto");
            await checkConn.ExecuteAsync("DELETE FROM Filter");
            await checkConn.ExecuteAsync("DELETE FROM Kategoria");
        }

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
        var insertTypSql = factory.IsPostgres
            ? "INSERT INTO TypPodujatia (Nazov) VALUES (@Nazov) ON CONFLICT DO NOTHING"
            : "INSERT OR IGNORE INTO TypPodujatia (Nazov) VALUES (@Nazov)";
        foreach (var t in typy)
            await conn.ExecuteAsync(insertTypSql, new { Nazov = t });

        // ── Event filtre ─────────────────────────────────────────────────────────
        var filtreNazvy = new[] {
            "Zadarmo", "Platené", "Vonku", "V interiéri", "S lístkom", "Bez lístka",
            "Pre deti", "18+", "Pet friendly", "Vegan-friendly", "Bezbariérový prístup",
            "Registrácia nutná", "Online / Hybrid"
        };
        var insertFilterSql = factory.IsPostgres
            ? "INSERT INTO EventFilter (Nazov) VALUES (@Nazov) ON CONFLICT DO NOTHING"
            : "INSERT OR IGNORE INTO EventFilter (Nazov) VALUES (@Nazov)";
        foreach (var f in filtreNazvy)
            await conn.ExecuteAsync(insertFilterSql, new { Nazov = f });

    }




}
