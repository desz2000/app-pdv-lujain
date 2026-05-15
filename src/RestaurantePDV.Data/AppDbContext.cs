using Microsoft.EntityFrameworkCore;
using RestaurantePDV.Core;

namespace RestaurantePDV.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Comanda> Comandas => Set<Comanda>();
    public DbSet<ItemComanda> ItensComanda => Set<ItemComanda>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Produto>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Nome).IsRequired().HasMaxLength(120);
            e.Property(p => p.Preco).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Comanda>(e =>
        {
            e.HasKey(c => c.Id);
            // Numero so e unico entre comandas abertas (status=0). Comandas fechadas/canceladas
            // mantem o numero pra historico, e o mesmo cartao fisico pode ser reusado em novas visitas.
            e.HasIndex(c => c.Numero)
                .IsUnique()
                .HasFilter("\"Status\" = 0")
                .HasDatabaseName("IX_Comandas_Numero_Aberta");
            e.Property(c => c.ValorTotal).HasColumnType("decimal(10,2)");
            e.HasMany(c => c.Itens)
                .WithOne(i => i.Comanda)
                .HasForeignKey(i => i.ComandaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemComanda>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Descricao).IsRequired().HasMaxLength(200);
            e.Property(i => i.Valor).HasColumnType("decimal(10,2)");
            e.HasOne(i => i.Produto)
                .WithMany()
                .HasForeignKey(i => i.ProdutoId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
