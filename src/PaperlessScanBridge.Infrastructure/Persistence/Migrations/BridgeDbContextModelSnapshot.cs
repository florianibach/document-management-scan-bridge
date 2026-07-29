using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace PaperlessScanBridge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BridgeDbContext))]
partial class BridgeDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.2");
        modelBuilder.Entity("PaperlessScanBridge.Infrastructure.Persistence.SchemaMarker", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.ToTable("SchemaMarkers");
        });
    }
}
