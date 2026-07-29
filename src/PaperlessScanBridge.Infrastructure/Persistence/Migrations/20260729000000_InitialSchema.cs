using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace PaperlessScanBridge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BridgeDbContext))]
[Migration("20260729000000_InitialSchema")]
public partial class InitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: "SchemaMarkers",
        columns: table => new
        {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                .Annotation("Sqlite:Autoincrement", true),
            CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
        },
        constraints: table => table.PrimaryKey("PK_SchemaMarkers", x => x.Id));

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("SchemaMarkers");
}
