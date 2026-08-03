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
        modelBuilder.Entity("PaperlessScanBridge.Infrastructure.Persistence.ProfileDefaultsEntity", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<int>("ColorMode").HasColumnType("INTEGER");
            b.Property<int?>("CorrespondentId").HasColumnType("INTEGER");
            b.Property<int?>("DocumentTypeId").HasColumnType("INTEGER");
            b.Property<int>("ResolutionDpi").HasColumnType("INTEGER");
            b.Property<long?>("ScannerId").HasColumnType("INTEGER");
            b.Property<string>("Source").HasColumnType("TEXT");
            b.Property<string>("TagIdsJson").IsRequired().HasColumnType("TEXT");
            b.Property<string>("Title").HasColumnType("TEXT");
            b.Property<DateTimeOffset>("UpdatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.ToTable("ProfileDefaults");
        });
        modelBuilder.Entity("PaperlessScanBridge.Infrastructure.Persistence.SchemaMarker", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.ToTable("SchemaMarkers");
        });
        modelBuilder.Entity("PaperlessScanBridge.Infrastructure.Persistence.SelectedScannerEntity", b =>
        {
            b.Property<long>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<string>("DisplayName").IsRequired().HasColumnType("TEXT");
            b.Property<string>("EsclUrl").IsRequired().HasColumnType("TEXT");
            b.Property<string>("IpAddress").IsRequired().HasColumnType("TEXT");
            b.Property<int>("Port").HasColumnType("INTEGER");
            b.Property<string>("Protocol").IsRequired().HasColumnType("TEXT");
            b.Property<string>("SaneDeviceId").HasColumnType("TEXT");
            b.Property<string>("SourcesJson").HasColumnType("TEXT");
            b.Property<string>("ResolutionsJson").HasColumnType("TEXT");
            b.Property<DateTimeOffset>("ValidatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.ToTable("SelectedScanners");
        });
    }
}
