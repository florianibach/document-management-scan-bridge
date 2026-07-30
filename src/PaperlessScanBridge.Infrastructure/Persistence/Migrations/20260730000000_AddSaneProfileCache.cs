using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace PaperlessScanBridge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BridgeDbContext))]
[Migration("20260730000000_AddSaneProfileCache")]
public sealed class AddSaneProfileCache : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("SaneDeviceId", "SelectedScanners", "TEXT", nullable: true);
        migrationBuilder.AddColumn<string>("SourcesJson", "SelectedScanners", "TEXT", nullable: true);
        migrationBuilder.AddColumn<string>("ResolutionsJson", "SelectedScanners", "TEXT", nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("SaneDeviceId", "SelectedScanners");
        migrationBuilder.DropColumn("SourcesJson", "SelectedScanners");
        migrationBuilder.DropColumn("ResolutionsJson", "SelectedScanners");
    }
}
