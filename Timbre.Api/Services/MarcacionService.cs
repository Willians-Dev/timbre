using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.DTOs.Marcaciones;
using Timbre.Api.Models;

namespace Timbre.Api.Services;


public class MarcacionService
{
    private readonly TimbreDbContext
        _context;


    private readonly FechaHoraService
        _fechaHoraService;


    public MarcacionService(
        TimbreDbContext context,
        FechaHoraService fechaHoraService)
    {
        _context =
            context;


        _fechaHoraService =
            fechaHoraService;
    }


    // =====================================================
    // REGISTRAR MARCACIÓN
    // =====================================================

    public async Task<RegistroMarcacionResultadoDto>
        RegistrarAsync(
            long idEmpleado,
            CancellationToken cancellationToken =
                default)
    {
        // =================================================
        // VALIDAR EMPLEADO
        // =================================================

        if (
            idEmpleado <=
            0
        )
        {
            throw new ArgumentException(
                "Debe indicar un empleado válido."
            );
        }


        // =================================================
        // EMPLEADO
        // =================================================

        var empleado =
            await _context
                .Empleado
                .Include(e =>
                    e.IdJornadaNavigation)
                .FirstOrDefaultAsync(
                    e =>
                        e.IdEmpleado ==
                            idEmpleado &&
                        e.Activo,

                    cancellationToken
                );


        if (
            empleado is null
        )
        {
            throw new InvalidOperationException(
                "El empleado no existe o se encuentra inactivo."
            );
        }


        // =================================================
        // JORNADA
        // =================================================

        var jornada =
            empleado
                .IdJornadaNavigation;


        if (
            jornada is null
        )
        {
            throw new InvalidOperationException(
                "El empleado no tiene una jornada laboral asignada."
            );
        }


        if (
            !jornada.Activo
        )
        {
            throw new InvalidOperationException(
                "La jornada del empleado se encuentra inactiva."
            );
        }


        // =================================================
        // FECHA / HORA ECUADOR
        // =================================================

        var ahora =
            _fechaHoraService
                .AhoraEcuador();


        var hoy =
            DateOnly
                .FromDateTime(
                    ahora
                );


        // =================================================
        // DÍA LABORABLE
        // =================================================

        if (
            !TrabajaHoy(
                jornada,
                ahora.DayOfWeek
            )
        )
        {
            throw new InvalidOperationException(
                "El empleado no tiene jornada laboral configurada para hoy."
            );
        }


        // =================================================
        // MARCACIONES ACTIVAS DEL DÍA
        // =================================================

        var marcacionesHoy =
            await _context
                .MarcacionAsistencia
                .Where(m =>
                    m.IdEmpleado ==
                        empleado.IdEmpleado &&

                    m.FechaMarcacion ==
                        hoy &&

                    m.EstadoMarcacion ==
                        "Activa"
                )
                .OrderBy(m =>
                    m.FechaHora)
                .ToListAsync(
                    cancellationToken
                );


        // =================================================
        // VALIDAR ESTADO PREVIO
        //
        // Aquí impedimos que el kiosko continúe una
        // jornada que nunca tuvo Entrada.
        //
        // No se inventan marcaciones faltantes.
        // =================================================

        ValidarSecuenciaCritica(
            ahora,
            jornada,
            marcacionesHoy
        );


        // =================================================
        // DETERMINAR MARCACIÓN
        // =================================================

        var tipoMarcacion =
            DeterminarTipoMarcacion(
                ahora,
                jornada,
                marcacionesHoy
            );


        if (
            tipoMarcacion is null
        )
        {
            throw new InvalidOperationException(
                ObtenerMensajeSinMarcacionDisponible(
                    ahora,
                    jornada,
                    marcacionesHoy
                )
            );
        }


        // =================================================
        // EVITAR DUPLICADO ACTIVO
        // =================================================

        var yaExiste =
            marcacionesHoy
                .Any(m =>
                    m.TipoMarcacion ==
                    tipoMarcacion
                );


        if (
            yaExiste
        )
        {
            throw new InvalidOperationException(
                $"La marcación {ObtenerNombreTipo(tipoMarcacion)} ya fue registrada hoy."
            );
        }


        // =================================================
        // GUARDAR
        // =================================================

        var marcacion =
            new MarcacionAsistencia
            {
                IdEmpleado =
                    empleado.IdEmpleado,

                FechaMarcacion =
                    hoy,

                FechaHora =
                    ahora,

                TipoMarcacion =
                    tipoMarcacion,

                EstadoMarcacion =
                    "Activa"
            };


        _context
            .MarcacionAsistencia
            .Add(
                marcacion
            );


        await _context
            .SaveChangesAsync(
                cancellationToken
            );


        // =================================================
        // ESTADO ACTUALIZADO
        // =================================================

        var marcacionesActualizadas =
            marcacionesHoy
                .Append(
                    marcacion
                )
                .ToList();


        var estadoDia =
            ObtenerEstadoDia(
                empleado.IdEmpleado,

                $"{empleado.Nombres} " +
                $"{empleado.Apellidos}",

                hoy,

                jornada,

                marcacionesActualizadas
            );


        // =================================================
        // PUNTUALIDAD
        // =================================================

        EstadoEntradaDto?
            estadoEntrada =
                null;


        if (
            marcacion.TipoMarcacion ==
            "Entrada"
        )
        {
            estadoEntrada =
                CalcularEstadoEntrada(
                    jornada,
                    marcacion.FechaHora
                );
        }


        // =================================================
        // MENSAJE
        // =================================================

        var mensaje =
            ObtenerMensajeRegistro(
                marcacion.TipoMarcacion,
                estadoDia
            );


        // =================================================
        // RESULTADO
        // =================================================

        return new RegistroMarcacionResultadoDto
        {
            IdMarcacion =
                marcacion.IdMarcacion,

            IdEmpleado =
                empleado.IdEmpleado,

            Empleado =
                $"{empleado.Nombres} " +
                $"{empleado.Apellidos}",

            Fecha =
                marcacion.FechaMarcacion,

            FechaHora =
                marcacion.FechaHora,

            TipoMarcacion =
                marcacion.TipoMarcacion,

            EstadoEntrada =
                estadoEntrada,

            Completa =
                estadoDia.Completa,

            MarcacionesFaltantes =
                estadoDia
                    .MarcacionesFaltantes,

            Mensaje =
                mensaje
        };
    }


    // =====================================================
    // VALIDACIÓN DE SECUENCIA CRÍTICA
    //
    // Regla:
    //
    // Si no existe Entrada, el sistema NO debe permitir:
    //
    // - Inicio de almuerzo
    // - Fin de almuerzo
    // - Salida
    //
    // Una marcación posterior no puede existir sin una
    // jornada previamente iniciada.
    // =====================================================

    private static void ValidarSecuenciaCritica(
        DateTime ahora,
        JornadaLaboral jornada,
        List<MarcacionAsistencia> marcaciones)
    {
        var horaActual =
            TimeOnly
                .FromDateTime(
                    ahora
                );


        var tieneEntrada =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                "Entrada"
            );


        // =================================================
        // TODAVÍA ESTAMOS EN EL PERÍODO DE ENTRADA
        // =================================================

        if (
            horaActual <
            jornada.HoraInicioAlmuerzo
        )
        {
            return;
        }


        // =================================================
        // SI EXISTE ENTRADA, PODEMOS CONTINUAR
        // =================================================

        if (
            tieneEntrada
        )
        {
            return;
        }


        // =================================================
        // SALIDA SIN ENTRADA
        // =================================================

        if (
            horaActual >=
            jornada.HoraSalida
        )
        {
            throw new InvalidOperationException(
                "No es posible registrar la salida porque no existe una entrada registrada para hoy. " +
                "Solicite a RRHH la regularización de la marcación faltante."
            );
        }


        // =================================================
        // ALMUERZO SIN ENTRADA
        // =================================================

        throw new InvalidOperationException(
            "No es posible registrar una marcación de almuerzo porque no existe una entrada registrada para hoy. " +
            "Solicite a RRHH la regularización de la marcación faltante."
        );
    }


    // =====================================================
    // DETERMINAR TIPO DE MARCACIÓN
    // =====================================================

    private static string? DeterminarTipoMarcacion(
        DateTime ahora,
        JornadaLaboral jornada,
        List<MarcacionAsistencia> marcaciones)
    {
        var horaActual =
            TimeOnly
                .FromDateTime(
                    ahora
                );


        var tieneEntrada =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                "Entrada"
            );


        var tieneInicioAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                "InicioAlmuerzo"
            );


        var tieneFinAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                "FinAlmuerzo"
            );


        var tieneSalida =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                "Salida"
            );


        // =================================================
        // 1. SALIDA
        //
        // La salida tiene prioridad desde HoraSalida.
        //
        // REGLA:
        // - requiere Entrada
        // - NO requiere obligatoriamente almuerzos
        //
        // Esto permite:
        //
        // Entrada          08:02
        // Inicio almuerzo  -
        // Fin almuerzo     -
        // Salida           17:04
        //
        // La salida es real y debe conservarse.
        // La jornada quedará Incompleta.
        // =================================================

        if (
            horaActual >=
            jornada.HoraSalida
        )
        {
            if (
                !tieneEntrada
            )
            {
                return null;
            }


            if (
                !tieneSalida
            )
            {
                return "Salida";
            }


            return null;
        }


        // =================================================
        // 2. FIN DE ALMUERZO
        //
        // Requiere:
        // - Entrada
        // - InicioAlmuerzo
        // =================================================

        if (
            horaActual >=
            jornada.HoraFinAlmuerzo
        )
        {
            if (
                tieneEntrada &&
                tieneInicioAlmuerzo &&
                !tieneFinAlmuerzo
            )
            {
                return "FinAlmuerzo";
            }


            return null;
        }


        // =================================================
        // 3. INICIO DE ALMUERZO
        //
        // Requiere Entrada.
        // =================================================

        if (
            horaActual >=
            jornada.HoraInicioAlmuerzo
        )
        {
            if (
                tieneEntrada &&
                !tieneInicioAlmuerzo
            )
            {
                return "InicioAlmuerzo";
            }


            return null;
        }


        // =================================================
        // 4. ENTRADA
        // =================================================

        if (
            !tieneEntrada
        )
        {
            return "Entrada";
        }


        return null;
    }


    // =====================================================
    // MENSAJE CUANDO NO HAY MARCACIÓN DISPONIBLE
    // =====================================================

    private static string
        ObtenerMensajeSinMarcacionDisponible(
            DateTime ahora,
            JornadaLaboral jornada,
            List<MarcacionAsistencia> marcaciones)
    {
        var horaActual =
            TimeOnly
                .FromDateTime(
                    ahora
                );


        var tieneEntrada =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                "Entrada"
            );


        var tieneInicioAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                "InicioAlmuerzo"
            );


        var tieneFinAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                "FinAlmuerzo"
            );


        var tieneSalida =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                "Salida"
            );


        // =================================================
        // JORNADA YA FINALIZADA
        // =================================================

        if (
            tieneSalida
        )
        {
            return
                "La jornada de hoy ya registra una salida.";
        }


        // =================================================
        // SALIDA SIN ENTRADA
        // Protección adicional.
        // =================================================

        if (
            horaActual >=
                jornada.HoraSalida &&
            !tieneEntrada
        )
        {
            return
                "No es posible registrar la salida porque no existe una entrada registrada para hoy. " +
                "Solicite a RRHH la regularización correspondiente.";
        }


        // =================================================
        // OLVIDÓ INICIO DE ALMUERZO
        // =================================================

        if (
            horaActual >=
                jornada.HoraFinAlmuerzo &&
            tieneEntrada &&
            !tieneInicioAlmuerzo
        )
        {
            return
                "No existe una marcación de inicio de almuerzo registrada. " +
                "La marcación faltante deberá ser regularizada por RRHH.";
        }


        // =================================================
        // FIN DE ALMUERZO YA REGISTRADO
        // =================================================

        if (
            horaActual >=
                jornada.HoraFinAlmuerzo &&
            tieneFinAlmuerzo
        )
        {
            return
                "La marcación de fin de almuerzo ya fue registrada.";
        }


        // =================================================
        // INICIO DE ALMUERZO YA REGISTRADO
        // =================================================

        if (
            horaActual >=
                jornada.HoraInicioAlmuerzo &&
            tieneInicioAlmuerzo
        )
        {
            return
                "La marcación de inicio de almuerzo ya fue registrada.";
        }


        // =================================================
        // ENTRADA YA REGISTRADA
        // =================================================

        if (
            tieneEntrada
        )
        {
            return
                "La entrada ya fue registrada. No existe otra marcación disponible en este momento.";
        }


        return
            "No existe una marcación disponible para registrar en este momento.";
    }


    // =====================================================
    // MENSAJE DE REGISTRO
    // =====================================================

    private static string ObtenerMensajeRegistro(
        string tipoMarcacion,
        EstadoMarcacionDiaDto estadoDia)
    {
        var nombre =
            ObtenerNombreTipo(
                tipoMarcacion
            );


        // =================================================
        // SALIDA CON JORNADA INCOMPLETA
        // =================================================

        if (
            tipoMarcacion ==
                "Salida" &&
            !estadoDia.Completa
        )
        {
            var faltantes =
                estadoDia
                    .MarcacionesFaltantes
                    .Where(m =>
                        m !=
                        "Salida"
                    )
                    .Select(
                        ObtenerNombreTipo
                    )
                    .ToList();


            if (
                faltantes.Count >
                0
            )
            {
                return
                    $"{nombre} registrada correctamente. " +
                    $"La jornada quedó incompleta por marcaciones faltantes: " +
                    $"{string.Join(", ", faltantes)}. " +
                    "Solicite la regularización a RRHH.";
            }
        }


        return
            $"{nombre} registrada correctamente.";
    }


    // =====================================================
    // NOMBRE AMIGABLE
    // =====================================================

    private static string ObtenerNombreTipo(
        string tipo)
    {
        return tipo switch
        {
            "Entrada" =>
                "Entrada",

            "InicioAlmuerzo" =>
                "Inicio de almuerzo",

            "FinAlmuerzo" =>
                "Fin de almuerzo",

            "Salida" =>
                "Salida",

            _ =>
                tipo
        };
    }


    // =====================================================
    // ESTADO DEL DÍA
    // =====================================================

    private static EstadoMarcacionDiaDto
        ObtenerEstadoDia(
            long idEmpleado,
            string nombreEmpleado,
            DateOnly fecha,
            JornadaLaboral jornada,
            List<MarcacionAsistencia> marcaciones)
    {
        var entrada =
            marcaciones
                .FirstOrDefault(m =>
                    m.TipoMarcacion ==
                        "Entrada" &&

                    m.EstadoMarcacion ==
                        "Activa"
                );


        var tieneEntrada =
            entrada is not null;


        var tieneInicioAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                    "InicioAlmuerzo" &&

                m.EstadoMarcacion ==
                    "Activa"
            );


        var tieneFinAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                    "FinAlmuerzo" &&

                m.EstadoMarcacion ==
                    "Activa"
            );


        var tieneSalida =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                    "Salida" &&

                m.EstadoMarcacion ==
                    "Activa"
            );


        // =================================================
        // PUNTUALIDAD
        // =================================================

        EstadoEntradaDto?
            estadoEntrada =
                null;


        if (
            entrada is not null
        )
        {
            estadoEntrada =
                CalcularEstadoEntrada(
                    jornada,
                    entrada.FechaHora
                );
        }


        // =================================================
        // FALTANTES
        // =================================================

        var faltantes =
            new List<string>();


        if (
            !tieneEntrada
        )
        {
            faltantes.Add(
                "Entrada"
            );
        }


        if (
            !tieneInicioAlmuerzo
        )
        {
            faltantes.Add(
                "InicioAlmuerzo"
            );
        }


        if (
            !tieneFinAlmuerzo
        )
        {
            faltantes.Add(
                "FinAlmuerzo"
            );
        }


        if (
            !tieneSalida
        )
        {
            faltantes.Add(
                "Salida"
            );
        }


        return new EstadoMarcacionDiaDto
        {
            IdEmpleado =
                idEmpleado,

            Empleado =
                nombreEmpleado,

            Fecha =
                fecha,

            TieneEntrada =
                tieneEntrada,

            TieneInicioAlmuerzo =
                tieneInicioAlmuerzo,

            TieneFinAlmuerzo =
                tieneFinAlmuerzo,

            TieneSalida =
                tieneSalida,

            EstadoEntrada =
                estadoEntrada?
                    .Estado,

            MinutosAtraso =
                estadoEntrada?
                    .MinutosAtraso ??
                0,

            Completa =
                faltantes.Count ==
                0,

            MarcacionesFaltantes =
                faltantes
        };
    }


    // =====================================================
    // PUNTUALIDAD
    // =====================================================

    private static EstadoEntradaDto
        CalcularEstadoEntrada(
            JornadaLaboral jornada,
            DateTime fechaHoraMarcacion)
    {
        var horaMarcacion =
            TimeOnly
                .FromDateTime(
                    fechaHoraMarcacion
                );


        var horaLimite =
            jornada
                .HoraEntrada
                .AddMinutes(
                    jornada
                        .ToleranciaEntradaMinutos
                );


        if (
            horaMarcacion <=
            horaLimite
        )
        {
            return new EstadoEntradaDto
            {
                Estado =
                    "Puntual",

                MinutosAtraso =
                    0,

                HoraEntradaProgramada =
                    jornada.HoraEntrada,

                ToleranciaMinutos =
                    jornada
                        .ToleranciaEntradaMinutos,

                HoraLimitePuntual =
                    horaLimite,

                HoraMarcacion =
                    horaMarcacion
            };
        }


        var diferencia =
            horaMarcacion
                .ToTimeSpan() -
            horaLimite
                .ToTimeSpan();


        return new EstadoEntradaDto
        {
            Estado =
                "Atraso",

            MinutosAtraso =
                (int)Math.Ceiling(
                    diferencia
                        .TotalMinutes
                ),

            HoraEntradaProgramada =
                jornada.HoraEntrada,

            ToleranciaMinutos =
                jornada
                    .ToleranciaEntradaMinutos,

            HoraLimitePuntual =
                horaLimite,

            HoraMarcacion =
                horaMarcacion
        };
    }


    // =====================================================
    // DÍA LABORABLE
    // =====================================================

    private static bool TrabajaHoy(
        JornadaLaboral jornada,
        DayOfWeek dia)
    {
        return dia switch
        {
            DayOfWeek.Monday =>
                jornada.Lunes,

            DayOfWeek.Tuesday =>
                jornada.Martes,

            DayOfWeek.Wednesday =>
                jornada.Miercoles,

            DayOfWeek.Thursday =>
                jornada.Jueves,

            DayOfWeek.Friday =>
                jornada.Viernes,

            DayOfWeek.Saturday =>
                jornada.Sabado,

            DayOfWeek.Sunday =>
                jornada.Domingo,

            _ =>
                false
        };
    }
}