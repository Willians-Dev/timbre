using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.DTOs.Marcaciones;
using Timbre.Api.Models;

namespace Timbre.Api.Services;

public class MarcacionService
{
    private readonly TimbreDbContext _context;
    private readonly FechaHoraService _fechaHoraService;

    public MarcacionService(
        TimbreDbContext context,
        FechaHoraService fechaHoraService)
    {
        _context = context;
        _fechaHoraService = fechaHoraService;
    }


    // =====================================================
    // REGISTRAR MARCACIÓN
    // =====================================================
    public async Task<RegistroMarcacionResultadoDto>
        RegistrarAsync(
            long idEmpleado,
            CancellationToken cancellationToken = default)
    {
        // =================================================
        // VALIDAR EMPLEADO
        // =================================================
        if (idEmpleado <= 0)
        {
            throw new ArgumentException(
                "Debe indicar un empleado válido."
            );
        }


        // =================================================
        // EMPLEADO
        // =================================================
        var empleado =
            await _context.Empleado
                .Include(e =>
                    e.IdJornadaNavigation)
                .FirstOrDefaultAsync(
                    e =>
                        e.IdEmpleado == idEmpleado &&
                        e.Activo,
                    cancellationToken
                );


        if (empleado is null)
        {
            throw new InvalidOperationException(
                "El empleado no existe o se encuentra inactivo."
            );
        }


        // =================================================
        // JORNADA
        // =================================================
        var jornada =
            empleado.IdJornadaNavigation;


        if (jornada is null)
        {
            throw new InvalidOperationException(
                "El empleado no tiene una jornada laboral asignada."
            );
        }


        if (!jornada.Activo)
        {
            throw new InvalidOperationException(
                "La jornada laboral asignada al empleado se encuentra inactiva."
            );
        }


        // =================================================
        // FECHA / HORA ECUADOR
        // =================================================
        var ahora =
            _fechaHoraService
                .AhoraEcuador();


        var hoy =
            DateOnly.FromDateTime(
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
                "Hoy no corresponde a un día laborable según la jornada asignada."
            );
        }


        // =================================================
        // MARCACIONES DEL DÍA
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
                        "Activa")
                .OrderBy(m =>
                    m.FechaHora)
                .ToListAsync(
                    cancellationToken
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


        // =================================================
        // NO HAY MARCACIÓN DISPONIBLE
        //
        // Antes se devolvía:
        //
        // "No existe una marcación disponible..."
        //
        // Ahora indicamos al usuario el motivo y cuál
        // sería la siguiente marcación.
        // =================================================
        if (tipoMarcacion is null)
        {
            var mensaje =
                ConstruirMensajeMarcacionNoDisponible(
                    ahora,
                    jornada,
                    marcacionesHoy
                );


            throw new InvalidOperationException(
                mensaje
            );
        }


        // =================================================
        // EVITAR DUPLICADOS
        // =================================================
        var yaExiste =
            marcacionesHoy.Any(m =>
                m.TipoMarcacion ==
                tipoMarcacion);


        if (yaExiste)
        {
            throw new InvalidOperationException(
                ConstruirMensajeMarcacionDuplicada(
                    tipoMarcacion
                )
            );
        }


        // =================================================
        // GUARDAR MARCACIÓN
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
        // ESTADO DEL DÍA
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
                $"{empleado.Nombres} {empleado.Apellidos}",
                hoy,
                jornada,
                marcacionesActualizadas
            );


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
        // RESULTADO
        // =================================================
        return new RegistroMarcacionResultadoDto
        {
            IdMarcacion =
                marcacion.IdMarcacion,

            IdEmpleado =
                empleado.IdEmpleado,

            Empleado =
                $"{empleado.Nombres} {empleado.Apellidos}",

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
                estadoDia.MarcacionesFaltantes,

            Mensaje =
                ConstruirMensajeExito(
                    marcacion.TipoMarcacion,
                    marcacion.FechaHora
                )
        };
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
            TimeOnly.FromDateTime(
                ahora
            );


        var tieneEntrada =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                "Entrada");


        var tieneInicioAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                "InicioAlmuerzo");


        var tieneFinAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                "FinAlmuerzo");


        var tieneSalida =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                "Salida");


        // =============================================
        // SALIDA
        // =============================================
        if (
            horaActual >=
            jornada.HoraSalida
        )
        {
            return !tieneSalida
                ? "Salida"
                : null;
        }


        // =============================================
        // FIN DE ALMUERZO
        // =============================================
        if (
            horaActual >=
            jornada.HoraFinAlmuerzo
        )
        {
            if (
                tieneInicioAlmuerzo &&
                !tieneFinAlmuerzo
            )
            {
                return "FinAlmuerzo";
            }


            return null;
        }


        // =============================================
        // INICIO DE ALMUERZO
        // =============================================
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


        // =============================================
        // ENTRADA
        // =============================================
        if (!tieneEntrada)
        {
            return "Entrada";
        }


        return null;
    }


    // =====================================================
    // MENSAJE CUANDO NO HAY MARCACIÓN DISPONIBLE
    // =====================================================
    private static string
        ConstruirMensajeMarcacionNoDisponible(
            DateTime ahora,
            JornadaLaboral jornada,
            List<MarcacionAsistencia> marcaciones)
    {
        var horaActual =
            TimeOnly.FromDateTime(
                ahora
            );


        var entrada =
            marcaciones.FirstOrDefault(m =>
                m.TipoMarcacion ==
                "Entrada");


        var inicioAlmuerzo =
            marcaciones.FirstOrDefault(m =>
                m.TipoMarcacion ==
                "InicioAlmuerzo");


        var finAlmuerzo =
            marcaciones.FirstOrDefault(m =>
                m.TipoMarcacion ==
                "FinAlmuerzo");


        var salida =
            marcaciones.FirstOrDefault(m =>
                m.TipoMarcacion ==
                "Salida");


        // =================================================
        // JORNADA COMPLETA
        // =================================================
        if (
            entrada is not null &&
            inicioAlmuerzo is not null &&
            finAlmuerzo is not null &&
            salida is not null
        )
        {
            return
                "La jornada de hoy ya tiene todas sus " +
                "marcaciones registradas.";
        }


        // =================================================
        // SALIDA YA REGISTRADA
        // =================================================
        if (salida is not null)
        {
            return
                $"La salida de hoy ya fue registrada a las " +
                $"{salida.FechaHora:HH:mm:ss}. " +
                "No existen más marcaciones disponibles.";
        }


        // =================================================
        // ANTES DEL INICIO DE ALMUERZO
        // =================================================
        if (
            entrada is not null &&
            inicioAlmuerzo is null &&
            horaActual <
                jornada.HoraInicioAlmuerzo
        )
        {
            return
                $"La entrada ya fue registrada a las " +
                $"{entrada.FechaHora:HH:mm:ss}. " +
                $"La siguiente marcación corresponde al " +
                $"inicio de almuerzo y estará disponible " +
                $"a partir de las " +
                $"{FormatearHora(jornada.HoraInicioAlmuerzo)}.";
        }


        // =================================================
        // EN HORARIO DE INICIO DE ALMUERZO, PERO
        // NO EXISTE ENTRADA
        // =================================================
        if (
            entrada is null &&
            horaActual >=
                jornada.HoraInicioAlmuerzo &&
            horaActual <
                jornada.HoraFinAlmuerzo
        )
        {
            return
                "No existe una entrada activa registrada para hoy. " +
                "No es posible registrar el inicio de almuerzo.";
        }


        // =================================================
        // INICIO DE ALMUERZO YA REGISTRADO,
        // ESPERANDO FIN DE ALMUERZO
        // =================================================
        if (
            inicioAlmuerzo is not null &&
            finAlmuerzo is null &&
            horaActual <
                jornada.HoraFinAlmuerzo
        )
        {
            return
                $"El inicio de almuerzo ya fue registrado a las " +
                $"{inicioAlmuerzo.FechaHora:HH:mm:ss}. " +
                $"La siguiente marcación corresponde al fin de " +
                $"almuerzo y estará disponible a partir de las " +
                $"{FormatearHora(jornada.HoraFinAlmuerzo)}.";
        }


        // =================================================
        // FIN DE ALMUERZO YA REGISTRADO,
        // ESPERANDO SALIDA
        // =================================================
        if (
            finAlmuerzo is not null &&
            salida is null &&
            horaActual <
                jornada.HoraSalida
        )
        {
            return
                $"El fin de almuerzo ya fue registrado a las " +
                $"{finAlmuerzo.FechaHora:HH:mm:ss}. " +
                $"La siguiente marcación corresponde a la salida " +
                $"y estará disponible a partir de las " +
                $"{FormatearHora(jornada.HoraSalida)}.";
        }


        // =================================================
        // PASÓ FIN DE ALMUERZO PERO NO HUBO INICIO
        // =================================================
        if (
            inicioAlmuerzo is null &&
            horaActual >=
                jornada.HoraFinAlmuerzo &&
            horaActual <
                jornada.HoraSalida
        )
        {
            return
                "No existe un inicio de almuerzo registrado para hoy. " +
                "No es posible registrar el fin de almuerzo.";
        }


        // =================================================
        // CASO GENERAL
        // =================================================
        return
            "No existe una marcación disponible para registrar " +
            "en este momento.";
    }


    // =====================================================
    // MENSAJE DE DUPLICADO
    // =====================================================
    private static string
        ConstruirMensajeMarcacionDuplicada(
            string tipoMarcacion)
    {
        return tipoMarcacion switch
        {
            "Entrada" =>
                "La entrada ya fue registrada hoy.",

            "InicioAlmuerzo" =>
                "El inicio de almuerzo ya fue registrado hoy.",

            "FinAlmuerzo" =>
                "El fin de almuerzo ya fue registrado hoy.",

            "Salida" =>
                "La salida ya fue registrada hoy.",

            _ =>
                $"La marcación {tipoMarcacion} ya fue registrada hoy."
        };
    }


    // =====================================================
    // MENSAJE DE ÉXITO
    // =====================================================
    private static string
        ConstruirMensajeExito(
            string tipoMarcacion,
            DateTime fechaHora)
    {
        var hora =
            fechaHora.ToString(
                "HH:mm:ss"
            );


        return tipoMarcacion switch
        {
            "Entrada" =>
                $"Entrada registrada correctamente a las {hora}.",

            "InicioAlmuerzo" =>
                $"Inicio de almuerzo registrado correctamente a las {hora}.",

            "FinAlmuerzo" =>
                $"Fin de almuerzo registrado correctamente a las {hora}.",

            "Salida" =>
                $"Salida registrada correctamente a las {hora}.",

            _ =>
                $"{tipoMarcacion} registrada correctamente a las {hora}."
        };
    }


    // =====================================================
    // FORMATEAR HORA
    // =====================================================
    private static string FormatearHora(
        TimeOnly hora)
    {
        return hora.ToString(
            "HH:mm"
        );
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
            marcaciones.FirstOrDefault(m =>
                m.TipoMarcacion ==
                    "Entrada" &&
                m.EstadoMarcacion ==
                    "Activa");


        var tieneEntrada =
            entrada is not null;


        var tieneInicioAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                    "InicioAlmuerzo" &&
                m.EstadoMarcacion ==
                    "Activa");


        var tieneFinAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                    "FinAlmuerzo" &&
                m.EstadoMarcacion ==
                    "Activa");


        var tieneSalida =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                    "Salida" &&
                m.EstadoMarcacion ==
                    "Activa");


        EstadoEntradaDto?
            estadoEntrada =
            null;


        if (entrada is not null)
        {
            estadoEntrada =
                CalcularEstadoEntrada(
                    jornada,
                    entrada.FechaHora
                );
        }


        var faltantes =
            new List<string>();


        if (!tieneEntrada)
        {
            faltantes.Add(
                "Entrada"
            );
        }


        if (!tieneInicioAlmuerzo)
        {
            faltantes.Add(
                "InicioAlmuerzo"
            );
        }


        if (!tieneFinAlmuerzo)
        {
            faltantes.Add(
                "FinAlmuerzo"
            );
        }


        if (!tieneSalida)
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
                estadoEntrada?.Estado,

            MinutosAtraso =
                estadoEntrada?.MinutosAtraso ??
                0,

            Completa =
                faltantes.Count == 0,

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
            TimeOnly.FromDateTime(
                fechaHoraMarcacion
            );


        var horaLimite =
            jornada.HoraEntrada
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
            horaMarcacion.ToTimeSpan() -
            horaLimite.ToTimeSpan();


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