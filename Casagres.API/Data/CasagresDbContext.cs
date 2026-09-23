using Casagres.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Casagres.API.Data;

public class CasagresDbContext : DbContext
{
    public CasagresDbContext(
        DbContextOptions<CasagresDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }

    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public DbSet<PowerBiTablero> PowerBiTableros { get; set; }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("casagres_login");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.UsuarioNombre)
                .HasColumnName("usuario")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(150);

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(150);

            entity.Property(e => e.Rol)
                .HasColumnName("rol")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Activo)
                .HasColumnName("activo")
                .IsRequired();

            entity.Property(e => e.FechaCreacion)
                .HasColumnName("fecha_creacion")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.EmailVerificado)
                .HasColumnName("email_verificado")
                .IsRequired();

            entity.Property(e => e.FotoUrl)
                .HasColumnName("foto_url")
                .HasMaxLength(500);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("casagres_password_reset");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id")
                .IsRequired();

            entity.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Token)
                .HasColumnName("token")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.FechaExpiracion)
                .HasColumnName("fecha_expiracion")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.Usado)
                .HasColumnName("usado")
                .IsRequired();

            entity.Property(e => e.FechaCreacion)
                .HasColumnName("fecha_creacion")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.ToTable("casagres_email_verificacion");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.UsuarioId)
                .HasColumnName("usuario_id")
                .IsRequired();

            entity.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Token)
                .HasColumnName("token")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.FechaExpiracion)
                .HasColumnName("fecha_expiracion")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(e => e.Usado)
                .HasColumnName("usado")
                .IsRequired();

            entity.Property(e => e.FechaCreacion)
                .HasColumnName("fecha_creacion")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        });

        modelBuilder.Entity<PowerBiTablero>(entity =>
        {
            entity.ToTable("casagres_powerbi_tableros");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(e => e.Url)
                .HasColumnName("url")
                .IsRequired();

            entity.Property(e => e.FechaCreacion)
                .HasColumnName("fecha_creacion")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        });
    }
}