using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace PaperlessScanBridge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BridgeDbContext))]
[Migration("20260729010000_AddSelectedScanner")]
public partial class AddSelectedScanner : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: "SelectedScanners",
        columns: table => new
        {
            Id = table.Column<long>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
            DisplayName = table.Column<string>(type: "TEXT", nullable: false),
            IpAddress = table.Column<string>(type: "TEXT", nullable: false),
            Port = table.Column<int>(type: "INTEGER", nullable: false),
            Protocol = table.Column<string>(type: "TEXT", nullable: false),
            EsclUrl = table.Column<string>(type: "TEXT", nullable: false),
            ValidatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_SelectedScanners", x => x.Id));
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("SelectedScanners");
}
