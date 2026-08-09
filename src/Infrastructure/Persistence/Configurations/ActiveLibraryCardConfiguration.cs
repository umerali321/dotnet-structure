using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkillsetsBackend.Domain.Skillsoft;

namespace SkillsetsBackend.Infrastructure.Persistence.Configurations;

public class ActiveLibraryCardConfiguration : IEntityTypeConfiguration<ActiveLibraryCard>
{
    public void Configure(EntityTypeBuilder<ActiveLibraryCard> builder)
    {
        builder.ToTable("ActiveLibraryCards");
        builder.HasNoKey();

        builder.Property(x => x.CompanyCode).HasColumnName("Company_Code").HasMaxLength(50);
        builder.Property(x => x.CompanyName).HasColumnName("Company_Name").HasMaxLength(100);
        builder.Property(x => x.ManagerId).HasColumnName("Manager_ID").HasMaxLength(50);
        builder.Property(x => x.FirstName).HasColumnName("First_Name").HasMaxLength(50);
        builder.Property(x => x.LastName).HasColumnName("Last_Name").HasMaxLength(50);
        builder.Property(x => x.Email).HasColumnName("Email").HasMaxLength(50);
        builder.Property(x => x.UserId).HasColumnName("User_ID").HasMaxLength(50);
        builder.Property(x => x.StartDate).HasColumnName("Start_Date");
        builder.Property(x => x.EndDate).HasColumnName("End_Date");
        builder.Property(x => x.FdmName).HasColumnName("FDM_Name").HasMaxLength(50);
    }
}
