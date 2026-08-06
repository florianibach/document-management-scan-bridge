using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace PaperlessScanBridge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BridgeDbContext))]
[Migration("20260805000000_AddAuthenticatedProfiles")]
public sealed class AddAuthenticatedProfiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ProfileId", table: "ProfileDefaults", type: "TEXT", nullable: false, defaultValue: "anonymous");
        migrationBuilder.CreateTable(name: "UserProfiles", columns: table => new
        {
            Id = table.Column<string>(type: "TEXT", nullable: false),
            Issuer = table.Column<string>(type: "TEXT", nullable: false),
            Subject = table.Column<string>(type: "TEXT", nullable: false),
            DisplayName = table.Column<string>(type: "TEXT", nullable: false),
            CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
            LastSeenAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_UserProfiles", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_ProfileDefaults_ProfileId", table: "ProfileDefaults", column: "ProfileId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_UserProfiles_Issuer_Subject", table: "UserProfiles", columns: ["Issuer", "Subject"], unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UserProfiles");
        migrationBuilder.DropIndex(name: "IX_ProfileDefaults_ProfileId", table: "ProfileDefaults");
        migrationBuilder.DropColumn(name: "ProfileId", table: "ProfileDefaults");
    }
}
