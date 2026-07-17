using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Milu.Web.Infrastructure.Security;

public sealed class MiluIdentityDbContext(
    DbContextOptions<MiluIdentityDbContext> options)
    : IdentityDbContext<MiluUser, MiluRole, string>(options)
{
    public DbSet<MiluRolePermission> RolePermissions => Set<MiluRolePermission>();

    public DbSet<MiluUserPermission> UserPermissions => Set<MiluUserPermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<MiluUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(120).IsRequired();
            entity.HasIndex(user => user.IsActive);
        });

        builder.Entity<MiluRole>(entity =>
        {
            entity.Property(role => role.Description).HasMaxLength(500);
        });

        builder.Entity<MiluRolePermission>(entity =>
        {
            entity.HasKey(permission => new
            {
                permission.RoleId,
                permission.ModuleKey,
                permission.Operation
            });
            entity.Property(permission => permission.ModuleKey).HasMaxLength(64);
            entity.HasOne<MiluRole>()
                .WithMany()
                .HasForeignKey(permission => permission.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MiluUserPermission>(entity =>
        {
            entity.HasKey(permission => new
            {
                permission.UserId,
                permission.ModuleKey,
                permission.Operation
            });
            entity.Property(permission => permission.ModuleKey).HasMaxLength(64);
            entity.HasOne<MiluUser>()
                .WithMany()
                .HasForeignKey(permission => permission.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
