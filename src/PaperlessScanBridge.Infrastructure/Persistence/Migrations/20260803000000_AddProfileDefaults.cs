using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable
namespace PaperlessScanBridge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BridgeDbContext))]
[Migration("20260803000000_AddProfileDefaults")]
public sealed class AddProfileDefaults : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: "ProfileDefaults",
        columns: table => new
        {
            Id = table.Column<int>(type: "INTEGER", nullable: false),
            ScannerId = table.Column<long>(type: "INTEGER", nullable: true),
            Source = table.Column<string>(type: "TEXT", nullable: true),
            ColorMode = table.Column<int>(type: "INTEGER", nullable: false),
            ResolutionDpi = table.Column<int>(type: "INTEGER", nullable: false),
            Title = table.Column<string>(type: "TEXT", nullable: true),
            CorrespondentId = table.Column<int>(type: "INTEGER", nullable: true),
            DocumentTypeId = table.Column<int>(type: "INTEGER", nullable: true),
            TagIdsJson = table.Column<string>(type: "TEXT", nullable: false),
            UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_ProfileDefaults", x => x.Id));

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "ProfileDefaults");
}
