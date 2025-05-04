using MetalFlowScheduler.Api.Domain.Entities;
using MetalFlowScheduler.Api.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace MetalFlowScheduler.Api.Infrastructure.Data
{
    /// <summary>
    /// Contexto do banco de dados para a aplicação, usando Entity Framework Core.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Line> Lines { get; set; }
        public DbSet<WorkCenter> WorkCenters { get; set; }
        public DbSet<Operation> Operations { get; set; }
        public DbSet<OperationType> OperationTypes { get; set; }
        public DbSet<ProductionOrder> ProductionOrders { get; set; }
        public DbSet<ProductionOrderItem> ProductionOrderItems { get; set; }
        public DbSet<LineWorkCenterRoute> LineWorkCenterRoutes { get; set; }
        public DbSet<ProductAvailablePerLine> ProductAvailablePerLines { get; set; }
        public DbSet<ProductOperationRoute> ProductOperationRoutes { get; set; }
        public DbSet<WorkCenterOperationRoute> WorkCenterOperationRoutes { get; set; }
        public DbSet<SurplusPerProductAndWorkCenter> SurplusPerProductAndWorkCenters { get; set; }

        /// <summary>
        /// Construtor utilizado pela injeção de dependência.
        /// </summary>
        /// <param name="options">Opções de configuração do DbContext.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        /// <summary>
        /// Configura o modelo de dados usando Fluent API.
        /// </summary>
        /// <param name="modelBuilder">O construtor usado para construir o modelo para este contexto.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Configurações Específicas ---

            // Restrição de Nome Único (R01)
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name)
                .IsUnique();

            modelBuilder.Entity<Line>()
                .HasIndex(l => l.Name)
                .IsUnique();

            modelBuilder.Entity<OperationType>()
                .HasIndex(ot => ot.Name)
                .IsUnique();

            // Configuração Relacionamento Muitos-para-Muitos: Product <-> Line (via ProductAvailablePerLine)
            // Garante unicidade da combinação ProductID e LineID na tabela de ligação.
            modelBuilder.Entity<ProductAvailablePerLine>()
               .HasIndex(pal => new { pal.ProductID, pal.LineID })
               .IsUnique();

            modelBuilder.Entity<ProductAvailablePerLine>()
                .HasOne(pal => pal.Product)
                .WithMany(p => p.AvailableOnLines)
                .HasForeignKey(pal => pal.ProductID);

            modelBuilder.Entity<ProductAvailablePerLine>()
                .HasOne(pal => pal.Line)
                .WithMany(l => l.AvailableProducts)
                .HasForeignKey(pal => pal.LineID);

            // Configuração Relacionamento Um-para-Muitos: Line -> WorkCenter
            modelBuilder.Entity<WorkCenter>()
                .HasOne(wc => wc.Line)
                .WithMany(l => l.WorkCenters)
                .HasForeignKey(wc => wc.LineID);

            // Configuração Relacionamento Um-para-Muitos: WorkCenter -> Operation
            modelBuilder.Entity<Operation>()
                .HasOne(op => op.WorkCenter)
                .WithMany(wc => wc.Operations)
                .HasForeignKey(op => op.WorkCenterID);

            // Configuração Relacionamento Um-para-Muitos: OperationType -> Operation
            modelBuilder.Entity<Operation>()
               .HasOne(op => op.OperationType)
               .WithMany(ot => ot.Operations)
               .HasForeignKey(op => op.OperationTypeID);

            // Configuração de Tipos Decimais e Float (reforçando DataAnnotations)
            modelBuilder.Entity<Product>()
                .Property(p => p.UnitPricePerTon)
                .HasColumnType("decimal(18, 2)");
            modelBuilder.Entity<Product>()
                .Property(p => p.ProfitMargin)
                .HasColumnType("decimal(18, 4)");
            modelBuilder.Entity<Product>()
               .Property(p => p.PenaltyCost)
               .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<ProductionOrderItem>()
               .Property(poi => poi.Quantity)
               .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<WorkCenter>()
              .Property(wc => wc.OptimalBatch)
              .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<SurplusPerProductAndWorkCenter>()
               .Property(s => s.Surplus)
               .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<Operation>()
               .Property(o => o.Capacity)
               .HasColumnType("float"); // double precision no PostgreSQL

            // Configuração Relacionamento: LineWorkCenterRoute
            modelBuilder.Entity<LineWorkCenterRoute>()
               .HasOne(lwcr => lwcr.Line)
               .WithMany(l => l.WorkCenterRoutes)
               .HasForeignKey(lwcr => lwcr.LineID);
            modelBuilder.Entity<LineWorkCenterRoute>()
                .HasOne(lwcr => lwcr.WorkCenter)
                .WithMany(wc => wc.LineRoutes)
                .HasForeignKey(lwcr => lwcr.WorkCenterID);

            // Configuração Relacionamento: ProductOperationRoute
            modelBuilder.Entity<ProductOperationRoute>()
                .HasOne(por => por.Product)
                .WithMany(p => p.OperationRoutes)
                .HasForeignKey(por => por.ProductID);
            modelBuilder.Entity<ProductOperationRoute>()
                .HasOne(por => por.OperationType)
                .WithMany(ot => ot.ProductRoutes)
                .HasForeignKey(por => por.OperationTypeID);

            // Configuração Relacionamento: WorkCenterOperationRoute
            modelBuilder.Entity<WorkCenterOperationRoute>()
                .HasOne(wcor => wcor.WorkCenter)
                .WithMany(wc => wc.OperationRoutes)
                .HasForeignKey(wcor => wcor.WorkCenterID);
            modelBuilder.Entity<WorkCenterOperationRoute>()
                .HasOne(wcor => wcor.OperationType)
                .WithMany(ot => ot.WorkCenterRoutes)
                .HasForeignKey(wcor => wcor.OperationTypeID);

            // Configuração Relacionamento: ProductionOrder -> ProductionOrderItem
            modelBuilder.Entity<ProductionOrderItem>()
                .HasOne(poi => poi.ProductionOrder)
                .WithMany(po => po.Items)
                .HasForeignKey(poi => poi.ProductionOrderID);
            modelBuilder.Entity<ProductionOrderItem>()
                .HasOne(poi => poi.Product)
                .WithMany(p => p.ProductionOrderItems)
                .HasForeignKey(poi => poi.ProductID);

            // Configuração Relacionamento: SurplusPerProductAndWorkCenter
            modelBuilder.Entity<SurplusPerProductAndWorkCenter>()
                .HasOne(s => s.Product)
                .WithMany(p => p.SurplusStocks)
                .HasForeignKey(s => s.ProductID);
            modelBuilder.Entity<SurplusPerProductAndWorkCenter>()
                .HasOne(s => s.WorkCenter)
                .WithMany(wc => wc.SurplusStocks)
                .HasForeignKey(s => s.WorkCenterID);

            // Query Filters Globais podem ser adicionados aqui se necessário
            // Ex: modelBuilder.Entity<Product>().HasQueryFilter(p => p.Enabled);
        }
    }
}
