using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Infrastructure.Data;

public static class MiluMediaSchema
{
    public static void EnsureCreated(MiluDbContext database)
    {
        database.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "MediaAssets" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_MediaAssets" PRIMARY KEY AUTOINCREMENT,
                "FileName" TEXT NOT NULL, "StoredFileName" TEXT NOT NULL,
                "ContentType" TEXT NOT NULL, "Size" INTEGER NOT NULL,
                "Title" TEXT NOT NULL, "AltText" TEXT NOT NULL, "CreatedAt" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_MediaAssets_CreatedAt" ON "MediaAssets" ("CreatedAt");
            CREATE TABLE IF NOT EXISTS "MediaUsages" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_MediaUsages" PRIMARY KEY AUTOINCREMENT,
                "MediaAssetId" INTEGER NOT NULL, "ModuleKey" TEXT NOT NULL,
                "EntityType" TEXT NOT NULL, "EntityId" TEXT NOT NULL,
                "FieldName" TEXT NOT NULL, "DisplayName" TEXT NOT NULL, "EditUrl" TEXT NULL,
                CONSTRAINT "FK_MediaUsages_MediaAssets_MediaAssetId" FOREIGN KEY ("MediaAssetId")
                    REFERENCES "MediaAssets" ("Id") ON DELETE CASCADE
            );
            DROP INDEX IF EXISTS "IX_MediaUsages_Reference";
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_MediaUsages_Reference"
                ON "MediaUsages" ("MediaAssetId", "ModuleKey", "EntityType", "EntityId", "FieldName");
            CREATE TABLE IF NOT EXISTS "Settings" (
                "Key" TEXT NOT NULL CONSTRAINT "PK_Settings" PRIMARY KEY,
                "Value" TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "LayoutInstallations" (
                "Key" TEXT NOT NULL CONSTRAINT "PK_LayoutInstallations" PRIMARY KEY,
                "DisplayName" TEXT NOT NULL, "Description" TEXT NOT NULL,
                "InstalledVersion" TEXT NOT NULL, "AvailableVersion" TEXT NOT NULL,
                "ViewPath" TEXT NOT NULL, "IsEnabled" INTEGER NOT NULL,
                "IsInstalled" INTEGER NOT NULL, "IsProtected" INTEGER NOT NULL, "IsBuiltIn" INTEGER NOT NULL
            );
            INSERT OR IGNORE INTO "LayoutInstallations" VALUES
                ('classic','Classic','Klassische Navigation mit hellem Bootstrap-Aufbau.','1.0.0','1.0.0','/Views/Shared/Layouts/_Classic.cshtml',1,1,1,1),
                ('modern','Modern','Großzügiges, modernes Layout mit dunkler Kopfleiste.','1.0.0','1.1.0','/Views/Shared/Layouts/_Modern.cshtml',1,1,0,1),
                ('minimal','Minimal','Reduziertes Layout mit Fokus auf Typografie und Inhalt.','1.0.0','1.0.0','/Views/Shared/Layouts/_Minimal.cshtml',1,1,0,1);
            """);

        var newsColumns = database.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('NewsArticles')")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!newsColumns.Contains("FeaturedMediaId"))
        {
            database.Database.ExecuteSqlRaw("ALTER TABLE \"NewsArticles\" ADD COLUMN \"FeaturedMediaId\" INTEGER NULL");
        }
    }
}
