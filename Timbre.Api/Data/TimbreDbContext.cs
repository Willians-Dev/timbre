using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Timbre.Api.Models;

namespace Timbre.Api.Data;

public partial class TimbreDbContext : DbContext
{
    public TimbreDbContext(
        DbContextOptions<TimbreDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Auditoria> Auditoria { get; set; }

    public virtual DbSet<Empleado> Empleado { get; set; }

    public virtual DbSet<JornadaLaboral> JornadaLaboral { get; set; }

    public virtual DbSet<MarcacionAsistencia> MarcacionAsistencia { get; set; }

    public virtual DbSet<Rol> Rol { get; set; }

    public virtual DbSet<RostroEmpleado> RostroEmpleado { get; set; }

    public virtual DbSet<Usuario> Usuario { get; set; }

    public virtual DbSet<ValidacionFacial> ValidacionFacial { get; set; }


    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        // =====================================================
        // AUDITORIA
        // =====================================================
        modelBuilder.Entity<Auditoria>(entity =>
        {
            entity.HasKey(e => e.IdAuditoria)
                .HasName("auditoria_pkey");

            entity.ToTable("auditoria");

            entity.HasIndex(
                e => e.FechaAccion,
                "ix_auditoria_fecha"
            );

            entity.Property(e => e.IdAuditoria)
                .HasColumnName("id_auditoria");

            entity.Property(e => e.Accion)
                .HasMaxLength(100)
                .HasColumnName("accion");

            entity.Property(e => e.Entidad)
                .HasMaxLength(100)
                .HasColumnName("entidad");

            entity.Property(e => e.FechaAccion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_accion");

            entity.Property(e => e.IdEntidad)
                .HasColumnName("id_entidad");

            entity.Property(e => e.IdUsuario)
                .HasColumnName("id_usuario");

            entity.Property(e => e.IpOrigen)
                .HasMaxLength(50)
                .HasColumnName("ip_origen");

            entity.Property(e => e.ValorAnterior)
                .HasColumnType("jsonb")
                .HasColumnName("valor_anterior");

            entity.Property(e => e.ValorNuevo)
                .HasColumnType("jsonb")
                .HasColumnName("valor_nuevo");

            entity.HasOne(
                    d => d.IdUsuarioNavigation
                )
                .WithMany(
                    p => p.Auditoria
                )
                .HasForeignKey(
                    d => d.IdUsuario
                )
                .HasConstraintName(
                    "fk_auditoria_usuario"
                );
        });


        // =====================================================
        // EMPLEADO
        // =====================================================
        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.IdEmpleado)
                .HasName("empleado_pkey");

            entity.ToTable("empleado");

            entity.HasIndex(
                e => e.IdJornada,
                "ix_empleado_jornada"
            );

            entity.HasIndex(
                    e => e.Identificacion,
                    "uq_empleado_identificacion"
                )
                .IsUnique();

            entity.Property(e => e.IdEmpleado)
                .HasColumnName("id_empleado");

            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");

            entity.Property(e => e.Apellidos)
                .HasMaxLength(100)
                .HasColumnName("apellidos");

            entity.Property(e => e.Area)
                .HasMaxLength(150)
                .HasColumnName("area");

            entity.Property(e => e.Cargo)
                .HasMaxLength(150)
                .HasColumnName("cargo");

            entity.Property(e => e.Correo)
                .HasMaxLength(150)
                .HasColumnName("correo");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_creacion");

            entity.Property(e => e.FechaIngreso)
                .HasColumnName("fecha_ingreso");

            entity.Property(e => e.FechaModificacion)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_modificacion");

            entity.Property(e => e.FechaSalida)
                .HasColumnName("fecha_salida");

            entity.Property(e => e.IdJornada)
                .HasColumnName("id_jornada");

            entity.Property(e => e.Identificacion)
                .HasMaxLength(20)
                .HasColumnName("identificacion");

            entity.Property(e => e.Nombres)
                .HasMaxLength(100)
                .HasColumnName("nombres");

            entity.Property(e => e.Telefono)
                .HasMaxLength(30)
                .HasColumnName("telefono");

            entity.HasOne(
                    d => d.IdJornadaNavigation
                )
                .WithMany(
                    p => p.Empleado
                )
                .HasForeignKey(
                    d => d.IdJornada
                )
                .OnDelete(
                    DeleteBehavior.ClientSetNull
                )
                .HasConstraintName(
                    "fk_empleado_jornada"
                );
        });


        // =====================================================
        // JORNADA LABORAL
        // =====================================================
        modelBuilder.Entity<JornadaLaboral>(entity =>
        {
            entity.HasKey(e => e.IdJornada)
                .HasName("jornada_laboral_pkey");

            entity.ToTable("jornada_laboral");

            entity.Property(e => e.IdJornada)
                .HasColumnName("id_jornada");

            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");

            entity.Property(e => e.Domingo)
                .HasColumnName("domingo");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_creacion");

            entity.Property(e => e.FechaModificacion)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_modificacion");

            entity.Property(e => e.HoraEntrada)
                .HasColumnName("hora_entrada");

            entity.Property(e => e.HoraFinAlmuerzo)
                .HasColumnName("hora_fin_almuerzo");

            entity.Property(e => e.HoraInicioAlmuerzo)
                .HasColumnName("hora_inicio_almuerzo");

            entity.Property(e => e.HoraSalida)
                .HasColumnName("hora_salida");

            entity.Property(e => e.Jueves)
                .HasDefaultValue(true)
                .HasColumnName("jueves");

            entity.Property(e => e.Lunes)
                .HasDefaultValue(true)
                .HasColumnName("lunes");

            entity.Property(e => e.Martes)
                .HasDefaultValue(true)
                .HasColumnName("martes");

            entity.Property(e => e.Miercoles)
                .HasDefaultValue(true)
                .HasColumnName("miercoles");

            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");

            entity.Property(e => e.Sabado)
                .HasColumnName("sabado");

            entity.Property(e => e.ToleranciaEntradaMinutos)
                .HasColumnName("tolerancia_entrada_minutos");

            entity.Property(e => e.Viernes)
                .HasDefaultValue(true)
                .HasColumnName("viernes");
        });


        // =====================================================
        // MARCACION ASISTENCIA
        // =====================================================
        modelBuilder.Entity<MarcacionAsistencia>(entity =>
        {
            entity.HasKey(e => e.IdMarcacion)
                .HasName("marcacion_asistencia_pkey");

            entity.ToTable("marcacion_asistencia");

            entity.HasIndex(
                e => e.IdEmpleado,
                "ix_marcacion_empleado"
            );

            entity.HasIndex(
                e => new
                {
                    e.IdEmpleado,
                    e.FechaMarcacion
                },
                "ix_marcacion_empleado_fecha"
            );

            entity.HasIndex(
                e => e.FechaMarcacion,
                "ix_marcacion_fecha"
            );

            entity.HasIndex(
                    e => new
                    {
                        e.IdEmpleado,
                        e.FechaMarcacion,
                        e.TipoMarcacion
                    },
                    "uq_marcacion_empleado_fecha_tipo_activa"
                )
                .IsUnique()
                .HasFilter(
                    "((estado_marcacion)::text = 'Activa'::text)"
                );

            entity.Property(e => e.IdMarcacion)
                .HasColumnName("id_marcacion");

            entity.Property(e => e.EstadoMarcacion)
                .HasMaxLength(20)
                .HasDefaultValueSql(
                    "'Activa'::character varying"
                )
                .HasColumnName("estado_marcacion");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_creacion");

            entity.Property(e => e.FechaHora)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_hora");

            entity.Property(e => e.FechaMarcacion)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnName("fecha_marcacion");

            entity.Property(e => e.FechaModificacion)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_modificacion");

            entity.Property(e => e.IdEmpleado)
                .HasColumnName("id_empleado");

            entity.Property(e => e.IdUsuarioCreacion)
                .HasColumnName("id_usuario_creacion");

            entity.Property(e => e.IdUsuarioModificacion)
                .HasColumnName("id_usuario_modificacion");

            entity.Property(e => e.Observacion)
                .HasMaxLength(500)
                .HasColumnName("observacion");

            entity.Property(e => e.TipoMarcacion)
                .HasMaxLength(30)
                .HasColumnName("tipo_marcacion");

            entity.HasOne(
                    d => d.IdEmpleadoNavigation
                )
                .WithMany(
                    p => p.MarcacionAsistencia
                )
                .HasForeignKey(
                    d => d.IdEmpleado
                )
                .OnDelete(
                    DeleteBehavior.ClientSetNull
                )
                .HasConstraintName(
                    "fk_marcacion_empleado"
                );

            entity.HasOne(
                    d => d.IdUsuarioCreacionNavigation
                )
                .WithMany(
                    p => p.MarcacionAsistenciaIdUsuarioCreacionNavigation
                )
                .HasForeignKey(
                    d => d.IdUsuarioCreacion
                )
                .HasConstraintName(
                    "fk_marcacion_usuario_creacion"
                );

            entity.HasOne(
                    d => d.IdUsuarioModificacionNavigation
                )
                .WithMany(
                    p => p.MarcacionAsistenciaIdUsuarioModificacionNavigation
                )
                .HasForeignKey(
                    d => d.IdUsuarioModificacion
                )
                .HasConstraintName(
                    "fk_marcacion_usuario_modificacion"
                );
        });


        // =====================================================
        // ROL
        // =====================================================
        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.IdRol)
                .HasName("rol_pkey");

            entity.ToTable("rol");

            entity.HasIndex(
                    e => e.Nombre,
                    "uq_rol_nombre"
                )
                .IsUnique();

            entity.Property(e => e.IdRol)
                .HasColumnName("id_rol");

            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(250)
                .HasColumnName("descripcion");

            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
        });


        // =====================================================
        // ROSTRO EMPLEADO
        //
        // RELACION:
        //
        // EMPLEADO 1 ---- N ROSTRO_EMPLEADO
        //
        // Un empleado puede conservar varios enrolamientos
        // históricos, pero solamente uno puede estar activo.
        // =====================================================
        modelBuilder.Entity<RostroEmpleado>(entity =>
        {
            entity.HasKey(e => e.IdRostro)
                .HasName("rostro_empleado_pkey");

            entity.ToTable("rostro_empleado");


            // =================================================
            // IMPORTANTE:
            //
            // YA NO EXISTE:
            //
            // uq_rostro_empleado UNIQUE(id_empleado)
            //
            // porque impediría almacenar el histórico.
            // =================================================


            // =================================================
            // SOLO UN ROSTRO ACTIVO POR EMPLEADO
            // =================================================
            entity.HasIndex(
                    e => e.IdEmpleado,
                    "ux_rostro_empleado_activo"
                )
                .IsUnique()
                .HasFilter(
                    "(activo = true)"
                );


            entity.Property(e => e.IdRostro)
                .HasColumnName("id_rostro");

            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");

            entity.Property(e => e.DimensionEmbedding)
                .HasColumnName("dimension_embedding");

            entity.Property(e => e.Embedding)
                .HasColumnName("embedding");

            entity.Property(e => e.FechaModificacion)
                .HasColumnType(
                    "timestamp without time zone"
                )
                .HasColumnName(
                    "fecha_modificacion"
                );

            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql(
                    "CURRENT_TIMESTAMP"
                )
                .HasColumnType(
                    "timestamp without time zone"
                )
                .HasColumnName(
                    "fecha_registro"
                );

            entity.Property(e => e.IdEmpleado)
                .HasColumnName(
                    "id_empleado"
                );

            entity.Property(e => e.Modelo)
                .HasMaxLength(100)
                .HasColumnName(
                    "modelo"
                );

            entity.Property(e => e.RutaImagen)
                .HasMaxLength(500)
                .HasColumnName(
                    "ruta_imagen"
                );


            // =================================================
            // CORRECCION PRINCIPAL
            //
            // ANTES:
            //
            // .WithOne(p => p.RostroEmpleado)
            //
            // AHORA:
            //
            // .WithMany()
            //
            // No usamos todavía una navegación ICollection
            // en Empleado para evitar depender de que
            // Empleado.cs haya sido regenerado.
            // =================================================
            entity.HasOne(
                    d => d.IdEmpleadoNavigation
                )
                .WithMany(
                    p => p.RostroEmpleado
                )
                .HasForeignKey(
                    d => d.IdEmpleado
                )
                .OnDelete(
                    DeleteBehavior.ClientSetNull
                )
                .HasConstraintName(
                    "fk_rostro_empleado"
                );
        });


        // =====================================================
        // USUARIO
        // =====================================================
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario)
                .HasName("usuario_pkey");

            entity.ToTable("usuario");

            entity.HasIndex(
                e => e.IdRol,
                "ix_usuario_rol"
            );

            entity.HasIndex(
                    e => e.IdEmpleado,
                    "uq_usuario_empleado"
                )
                .IsUnique();

            entity.HasIndex(
                    e => e.NombreUsuario,
                    "uq_usuario_nombre"
                )
                .IsUnique();

            entity.Property(e => e.IdUsuario)
                .HasColumnName("id_usuario");

            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");

            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_creacion");

            entity.Property(e => e.FechaModificacion)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_modificacion");

            entity.Property(e => e.IdEmpleado)
                .HasColumnName("id_empleado");

            entity.Property(e => e.IdRol)
                .HasColumnName("id_rol");

            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(100)
                .HasColumnName("nombre_usuario");

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500)
                .HasColumnName("password_hash");

            entity.Property(e => e.UltimoAcceso)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ultimo_acceso");

            entity.HasOne(
                    d => d.IdEmpleadoNavigation
                )
                .WithOne(
                    p => p.Usuario
                )
                .HasForeignKey<Usuario>(
                    d => d.IdEmpleado
                )
                .OnDelete(
                    DeleteBehavior.ClientSetNull
                )
                .HasConstraintName(
                    "fk_usuario_empleado"
                );

            entity.HasOne(
                    d => d.IdRolNavigation
                )
                .WithMany(
                    p => p.Usuario
                )
                .HasForeignKey(
                    d => d.IdRol
                )
                .OnDelete(
                    DeleteBehavior.ClientSetNull
                )
                .HasConstraintName(
                    "fk_usuario_rol"
                );
        });


        // =====================================================
        // VALIDACION FACIAL
        // =====================================================
        modelBuilder.Entity<ValidacionFacial>(entity =>
        {
            entity.HasKey(e => e.IdValidacion)
                .HasName("validacion_facial_pkey");

            entity.ToTable("validacion_facial");

            entity.HasIndex(
                    e => e.IdMarcacion,
                    "uq_validacion_marcacion"
                )
                .IsUnique();

            entity.Property(e => e.IdValidacion)
                .HasColumnName("id_validacion");

            entity.Property(e => e.Aprobado)
                .HasColumnName("aprobado");

            entity.Property(e => e.FechaValidacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha_validacion");

            entity.Property(e => e.IdEmpleado)
                .HasColumnName("id_empleado");

            entity.Property(e => e.IdMarcacion)
                .HasColumnName("id_marcacion");

            entity.Property(e => e.ResultadoTecnico)
                .HasColumnType("jsonb")
                .HasColumnName("resultado_tecnico");

            entity.Property(e => e.Similitud)
                .HasPrecision(5, 2)
                .HasColumnName("similitud");

            entity.Property(e => e.Umbral)
                .HasPrecision(5, 2)
                .HasColumnName("umbral");

            entity.HasOne(
                    d => d.IdEmpleadoNavigation
                )
                .WithMany(
                    p => p.ValidacionFacial
                )
                .HasForeignKey(
                    d => d.IdEmpleado
                )
                .OnDelete(
                    DeleteBehavior.ClientSetNull
                )
                .HasConstraintName(
                    "fk_validacion_empleado"
                );

            entity.HasOne(
                    d => d.IdMarcacionNavigation
                )
                .WithOne(
                    p => p.ValidacionFacial
                )
                .HasForeignKey<ValidacionFacial>(
                    d => d.IdMarcacion
                )
                .OnDelete(
                    DeleteBehavior.ClientSetNull
                )
                .HasConstraintName(
                    "fk_validacion_marcacion"
                );
        });


        OnModelCreatingPartial(
            modelBuilder
        );
    }


    partial void OnModelCreatingPartial(
        ModelBuilder modelBuilder
    );
}