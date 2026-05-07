-- MiestaMy – databázová schéma
-- SQLite (kompatibilné aj s SQL Server pri drobných úpravách)
-- =====================================================================

PRAGMA foreign_keys = ON;

-- 1. Kategoria
--    Hlavná kategória miesta (Kaviarne, Jedlo, Hudba, Kultúra, ...)
CREATE TABLE IF NOT EXISTS Kategoria (
    Id    INTEGER PRIMARY KEY AUTOINCREMENT,
    Nazov TEXT    NOT NULL UNIQUE  -- napr. 'Kaviarne', 'Drinks'
);

-- 2. Filter
--    Konkrétny filter patriaci do jednej kategórie
--    (1:N – jedna Kategória má veľa Filtrov)
CREATE TABLE IF NOT EXISTS Filter (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Nazov       TEXT    NOT NULL,
    KategoriaId INTEGER NOT NULL,
    FOREIGN KEY (KategoriaId) REFERENCES Kategoria(Id),
    UNIQUE (Nazov, KategoriaId)   -- ten istý filter nemôže byť 2x v tej istej kategórii
);

-- 3. Miesto
--    Konkrétne miesto v Bratislave
CREATE TABLE IF NOT EXISTS Miesto (
    Id     INTEGER PRIMARY KEY AUTOINCREMENT,
    Nazov  TEXT NOT NULL UNIQUE,  -- názov musí byť unikátny a neprázdny
    Adresa TEXT,
    Lat    REAL,  -- zemepisná šírka
    Lng    REAL,  -- zemepisná dĺžka
    Popis  TEXT,
    WebUrl TEXT
);

-- 4. MiestoKategoria  (M:N – Miesto ↔ Kategória)
--    Jedno miesto môže mať viac kategórií (napr. Studňa = Jedlo + Drinks)
CREATE TABLE IF NOT EXISTS MiestoKategoria (
    MiestoId    INTEGER NOT NULL,
    KategoriaId INTEGER NOT NULL,
    JeHlavna    INTEGER NOT NULL DEFAULT 0,  -- 1 = hlavná kategória
    PRIMARY KEY (MiestoId, KategoriaId),
    FOREIGN KEY (MiestoId)    REFERENCES Miesto(Id)    ON DELETE CASCADE,
    FOREIGN KEY (KategoriaId) REFERENCES Kategoria(Id)
);

-- 5. MiestoFilter  (M:N – Miesto ↔ Filter)
--    Ukladáme len hodnoty ÁNO; ak filter nie je v tabuľke = NIE/N/A
CREATE TABLE IF NOT EXISTS MiestoFilter (
    MiestoId INTEGER NOT NULL,
    FilterId INTEGER NOT NULL,
    PRIMARY KEY (MiestoId, FilterId),
    FOREIGN KEY (MiestoId) REFERENCES Miesto(Id)   ON DELETE CASCADE,
    FOREIGN KEY (FilterId) REFERENCES Filter(Id)
);

-- =====================================================================
-- JOIN príklady (minimum 3 požadované zadaním)
-- =====================================================================

-- JOIN 1: Všetky miesta v kategórii 'Kaviarne'
--         Tabuľky: Miesto ⟵ MiestoKategoria ⟶ Kategoria
SELECT m.Id, m.Nazov, k.Nazov AS Kategoria
FROM Miesto m
INNER JOIN MiestoKategoria mk ON m.Id       = mk.MiestoId
INNER JOIN Kategoria        k ON mk.KategoriaId = k.Id
WHERE k.Nazov = 'Kaviarne'
ORDER BY m.Nazov;

-- JOIN 2: Všetky miesta s filtrom 'Speciality coffee'
--         Tabuľky: Miesto ⟵ MiestoFilter ⟶ Filter ⟶ Kategoria
SELECT DISTINCT m.Id, m.Nazov, k.Nazov AS HlavnaKategoria
FROM Miesto m
INNER JOIN MiestoFilter    mf ON m.Id       = mf.MiestoId
INNER JOIN Filter           f ON mf.FilterId = f.Id
INNER JOIN Kategoria        k ON f.KategoriaId = k.Id
WHERE f.Nazov = 'Speciality coffee'
ORDER BY m.Nazov;

-- JOIN 3: Kompletný detail miesta – všetky kategórie a filtre (grouped)
--         Tabuľky: Miesto ⟵ MiestoFilter ⟶ Filter ⟶ Kategoria
SELECT m.Nazov AS Miesto, k.Nazov AS Kategoria, f.Nazov AS Filter
FROM Miesto m
INNER JOIN MiestoFilter mf ON m.Id       = mf.MiestoId
INNER JOIN Filter        f ON mf.FilterId = f.Id
INNER JOIN Kategoria     k ON f.KategoriaId = k.Id
WHERE m.Id = 1
ORDER BY k.Nazov, f.Nazov;
