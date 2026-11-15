using Microsoft.EntityFrameworkCore;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;

namespace RetoAbierto.Infrastructure.Data;

public class RetoAbiertoDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantService _tenantService;

    public RetoAbiertoDbContext(
        DbContextOptions<RetoAbiertoDbContext> options,
        ITenantService tenantService) : base(options)
    {
        _tenantService = tenantService;
    }

    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Universidad> Universidades { get; set; } = null!;
    public DbSet<UniversidadUsuario> UniversidadUsuarios { get; set; } = null!;
    public DbSet<Estudiante> Estudiantes { get; set; } = null!;
    public DbSet<Beca> Becas { get; set; } = null!;

    public DbSet<Empresa> Empresas { get; set; } = null!;
    public DbSet<Rol> Roles { get; set; } = null!;
    public DbSet<EmpresaUsuario> EmpresaUsuarios { get; set; } = null!;

    public DbSet<Reto> Retos { get; set; } = null!;
    public DbSet<Pago> Pagos { get; set; } = null!;
    public DbSet<Recompensa> Recompensas { get; set; } = null!;
    public DbSet<RecompensaEntrega> RecompensaEntregas { get; set; } = null!;

    public DbSet<Equipo> Equipos { get; set; } = null!;
    public DbSet<EquipoIntegrante> EquipoIntegrantes { get; set; } = null!;
    public DbSet<Postulacion> Postulaciones { get; set; } = null!;
    public DbSet<Asignacion> Asignaciones { get; set; } = null!;

    public DbSet<Meta> Metas { get; set; } = null!;
    public DbSet<Avance> Avances { get; set; } = null!;
    public DbSet<RegistroHoras> RegistroHoras { get; set; } = null!;
    public DbSet<Evaluacion> Evaluaciones { get; set; } = null!;

    public DbSet<Habilidad> Habilidades { get; set; } = null!;
    public DbSet<EstudianteHabilidad> EstudianteHabilidades { get; set; } = null!;
    public DbSet<RetoHabilidad> RetoHabilidades { get; set; } = null!;

    IQueryable<Usuario> IApplicationDbContext.Usuarios => Usuarios;
    IQueryable<Universidad> IApplicationDbContext.Universidades => Universidades;
    IQueryable<UniversidadUsuario> IApplicationDbContext.UniversidadUsuarios => UniversidadUsuarios;
    IQueryable<Estudiante> IApplicationDbContext.Estudiantes => Estudiantes;
    IQueryable<Beca> IApplicationDbContext.Becas => Becas;
    IQueryable<Empresa> IApplicationDbContext.Empresas => Empresas;
    IQueryable<Rol> IApplicationDbContext.Roles => Roles;
    IQueryable<EmpresaUsuario> IApplicationDbContext.EmpresaUsuarios => EmpresaUsuarios;
    IQueryable<Reto> IApplicationDbContext.Retos => Retos;
    IQueryable<Pago> IApplicationDbContext.Pagos => Pagos;
    IQueryable<Recompensa> IApplicationDbContext.Recompensas => Recompensas;
    IQueryable<RecompensaEntrega> IApplicationDbContext.RecompensaEntregas => RecompensaEntregas;
    IQueryable<Equipo> IApplicationDbContext.Equipos => Equipos;
    IQueryable<EquipoIntegrante> IApplicationDbContext.EquipoIntegrantes => EquipoIntegrantes;
    IQueryable<Postulacion> IApplicationDbContext.Postulaciones => Postulaciones;
    IQueryable<Asignacion> IApplicationDbContext.Asignaciones => Asignaciones;
    IQueryable<Meta> IApplicationDbContext.Metas => Metas;
    IQueryable<Avance> IApplicationDbContext.Avances => Avances;
    IQueryable<RegistroHoras> IApplicationDbContext.RegistroHoras => RegistroHoras;
    IQueryable<Evaluacion> IApplicationDbContext.Evaluaciones => Evaluaciones;
    IQueryable<Habilidad> IApplicationDbContext.Habilidades => Habilidades;
    IQueryable<EstudianteHabilidad> IApplicationDbContext.EstudianteHabilidades => EstudianteHabilidades;
    IQueryable<RetoHabilidad> IApplicationDbContext.RetoHabilidades => RetoHabilidades;

    public async Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
    {
        await Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public void Remove<TEntity>(TEntity entity) where TEntity : class
    {
        Set<TEntity>().Remove(entity);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(b =>
        {
            b.ToTable("Usuarios");
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.Correo).IsUnique();
            b.Property(u => u.Nombre).HasMaxLength(100).IsRequired();
            b.Property(u => u.Correo).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<Universidad>(b =>
        {
            b.ToTable("Universidades");
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.Correo).IsUnique();
            b.Property(u => u.Nombre).HasMaxLength(150).IsRequired();
            b.Property(u => u.Correo).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<UniversidadUsuario>(b =>
        {
            b.ToTable("UniversidadUsuarios");
            b.HasKey(uu => new { uu.UniversidadId, uu.UsuarioId });

            b.HasOne(uu => uu.Universidad)
                .WithMany(u => u.UniversidadUsuarios)
                .HasForeignKey(uu => uu.UniversidadId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(uu => uu.Usuario)
                .WithMany(u => u.UniversidadUsuarios)
                .HasForeignKey(uu => uu.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Estudiante>(b =>
        {
            b.ToTable("Estudiantes");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.UsuarioId).IsUnique();
            b.HasIndex(e => e.UniversidadId);

            b.HasOne(e => e.Usuario)
                .WithOne(u => u.Estudiante)
                .HasForeignKey<Estudiante>(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Universidad)
                .WithMany(u => u.Estudiantes)
                .HasForeignKey(e => e.UniversidadId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.RegistradoPorUsuario)
                .WithMany()
                .HasForeignKey(e => e.RegistradoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Beca>(b =>
        {
            b.ToTable("Becas");
            b.HasKey(b => b.Id);
            b.HasIndex(b => b.EstudianteId);

            b.HasOne(b => b.Estudiante)
                .WithMany(e => e.Becas)
                .HasForeignKey(b => b.EstudianteId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(b => b.ValidadaPorUsuario)
                .WithMany()
                .HasForeignKey(b => b.ValidadaPorUsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Empresa>(b =>
        {
            b.ToTable("Empresas");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.Correo).IsUnique();
            b.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            b.Property(e => e.Correo).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<Rol>(b =>
        {
            b.ToTable("Roles");
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.Nombre).IsUnique();
            b.Property(r => r.Nombre).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<EmpresaUsuario>(b =>
        {
            b.ToTable("EmpresaUsuarios");
            b.HasKey(eu => new { eu.EmpresaId, eu.UsuarioId });
            b.HasIndex(eu => eu.RolId);

            b.HasOne(eu => eu.Empresa)
                .WithMany(e => e.EmpresaUsuarios)
                .HasForeignKey(eu => eu.EmpresaId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(eu => eu.Usuario)
                .WithMany(u => u.EmpresaUsuarios)
                .HasForeignKey(eu => eu.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(eu => eu.Rol)
                .WithMany(r => r.EmpresaUsuarios)
                .HasForeignKey(eu => eu.RolId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reto>(b =>
        {
            b.ToTable("Retos");
            b.HasKey(r => r.Id);
            b.HasIndex(r => new { r.EmpresaId, r.Estado });
            b.Property(r => r.Titulo).HasMaxLength(150).IsRequired();

            b.HasOne(r => r.Empresa)
                .WithMany(e => e.Retos)
                .HasForeignKey(r => r.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pago>(b =>
        {
            b.ToTable("Pagos");
            b.HasKey(p => p.Id);
            b.Property(p => p.Monto).HasPrecision(18, 2);

            b.HasOne(p => p.Empresa)
                .WithMany()
                .HasForeignKey(p => p.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.Reto)
                .WithMany(r => r.Pagos)
                .HasForeignKey(p => p.RetoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Recompensa>(b =>
        {
            b.ToTable("Recompensas");
            b.HasKey(r => r.Id);
            b.Property(r => r.Valor).HasPrecision(18, 2);

            b.HasOne(r => r.Empresa)
                .WithMany()
                .HasForeignKey(r => r.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(r => r.Reto)
                .WithMany(r => r.Recompensas)
                .HasForeignKey(r => r.RetoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecompensaEntrega>(b =>
        {
            b.ToTable("RecompensaEntregas");
            b.HasKey(re => new { re.RecompensaId, re.EstudianteId });

            b.HasOne(re => re.Empresa)
                .WithMany()
                .HasForeignKey(re => re.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(re => re.Recompensa)
                .WithMany(r => r.Entregas)
                .HasForeignKey(re => re.RecompensaId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(re => re.Estudiante)
                .WithMany(e => e.RecompensasEntregadas)
                .HasForeignKey(re => re.EstudianteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Equipo>(b =>
        {
            b.ToTable("Equipos");
            b.HasKey(e => e.Id);
            b.Property(e => e.Nombre).HasMaxLength(100).IsRequired();

            b.HasOne(e => e.Empresa)
                .WithMany()
                .HasForeignKey(e => e.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Reto)
                .WithMany(r => r.Equipos)
                .HasForeignKey(e => e.RetoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EquipoIntegrante>(b =>
        {
            b.ToTable("EquipoIntegrantes");
            b.HasKey(ei => new { ei.EquipoId, ei.EstudianteId });

            b.HasOne(ei => ei.Empresa)
                .WithMany()
                .HasForeignKey(ei => ei.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(ei => ei.Equipo)
                .WithMany(e => e.Integrantes)
                .HasForeignKey(ei => ei.EquipoId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(ei => ei.Estudiante)
                .WithMany(e => e.Equipos)
                .HasForeignKey(ei => ei.EstudianteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Postulacion>(b =>
        {
            b.ToTable("Postulaciones");
            b.HasKey(p => p.Id);
            b.HasIndex(p => new { p.EquipoId, p.RetoId }).IsUnique();

            b.HasOne(p => p.Empresa)
                .WithMany()
                .HasForeignKey(p => p.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.Reto)
                .WithMany(r => r.Postulaciones)
                .HasForeignKey(p => p.RetoId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.Equipo)
                .WithMany(e => e.Postulaciones)
                .HasForeignKey(p => p.EquipoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Asignacion>(b =>
        {
            b.ToTable("Asignaciones");
            b.HasKey(a => a.Id);
            b.HasIndex(a => a.PostulacionId).IsUnique();

            b.HasOne(a => a.Empresa)
                .WithMany()
                .HasForeignKey(a => a.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(a => a.Postulacion)
                .WithOne(p => p.Asignacion)
                .HasForeignKey<Asignacion>(a => a.PostulacionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Meta>(b =>
        {
            b.ToTable("Metas");
            b.HasKey(m => m.Id);
            b.Property(m => m.Titulo).HasMaxLength(150).IsRequired();

            b.HasOne(m => m.Empresa)
                .WithMany()
                .HasForeignKey(m => m.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(m => m.Asignacion)
                .WithMany(a => a.Metas)
                .HasForeignKey(m => m.AsignacionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Avance>(b =>
        {
            b.ToTable("Avances");
            b.HasKey(a => a.Id);
            b.HasIndex(a => a.MetaId);

            b.HasOne(a => a.Empresa)
                .WithMany()
                .HasForeignKey(a => a.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(a => a.Meta)
                .WithMany(m => m.Avances)
                .HasForeignKey(a => a.MetaId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.Estudiante)
                .WithMany(e => e.Avances)
                .HasForeignKey(a => a.EstudianteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RegistroHoras>(b =>
        {
            b.ToTable("RegistroHoras");
            b.HasKey(r => r.Id);

            b.HasOne(r => r.Empresa)
                .WithMany()
                .HasForeignKey(r => r.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(r => r.Asignacion)
                .WithMany(a => a.RegistrosHoras)
                .HasForeignKey(r => r.AsignacionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(r => r.Estudiante)
                .WithMany(e => e.RegistrosHoras)
                .HasForeignKey(r => r.EstudianteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Evaluacion>(b =>
        {
            b.ToTable("Evaluaciones");
            b.HasKey(e => e.Id);

            b.HasOne(e => e.Empresa)
                .WithMany()
                .HasForeignKey(e => e.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Asignacion)
                .WithMany(a => a.Evaluaciones)
                .HasForeignKey(e => e.AsignacionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(e => e.EvaluadoPorUsuario)
                .WithMany()
                .HasForeignKey(e => e.EvaluadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Habilidad>(b =>
        {
            b.ToTable("Habilidades");
            b.HasKey(h => h.Id);
            b.HasIndex(h => h.Nombre).IsUnique();
            b.Property(h => h.Nombre).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<EstudianteHabilidad>(b =>
        {
            b.ToTable("EstudianteHabilidades");
            b.HasKey(eh => new { eh.EstudianteId, eh.HabilidadId });

            b.HasOne(eh => eh.Estudiante)
                .WithMany(e => e.Habilidades)
                .HasForeignKey(eh => eh.EstudianteId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(eh => eh.Habilidad)
                .WithMany(h => h.Estudiantes)
                .HasForeignKey(eh => eh.HabilidadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RetoHabilidad>(b =>
        {
            b.ToTable("RetoHabilidades");
            b.HasKey(rh => new { rh.RetoId, rh.HabilidadId });

            b.HasOne(rh => rh.Empresa)
                .WithMany()
                .HasForeignKey(rh => rh.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(rh => rh.Reto)
                .WithMany(r => r.Habilidades)
                .HasForeignKey(rh => rh.RetoId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(rh => rh.Habilidad)
                .WithMany(h => h.Retos)
                .HasForeignKey(rh => rh.HabilidadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmpresaUsuario>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);

        modelBuilder.Entity<Reto>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);

        modelBuilder.Entity<Pago>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);

        modelBuilder.Entity<Recompensa>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);

        modelBuilder.Entity<RecompensaEntrega>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);

        modelBuilder.Entity<Equipo>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);

        modelBuilder.Entity<EquipoIntegrante>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);

        modelBuilder.Entity<Postulacion>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);

        modelBuilder.Entity<Asignacion>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);

        modelBuilder.Entity<Meta>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);

        modelBuilder.Entity<Avance>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);

        modelBuilder.Entity<RegistroHoras>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);

        modelBuilder.Entity<Evaluacion>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);

        modelBuilder.Entity<RetoHabilidad>()
            .HasQueryFilter(e => _tenantService.EmpresaId == null || e.EmpresaId == _tenantService.EmpresaId);
    }
}

