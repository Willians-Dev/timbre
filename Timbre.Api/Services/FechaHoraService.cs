namespace Timbre.Api.Services;

public class FechaHoraService
{
    private readonly TimeZoneInfo _zonaEcuador;
    private readonly IWebHostEnvironment _environment;

    private DateTime? _fechaHoraSimulada;

    public FechaHoraService(
        IWebHostEnvironment environment)
    {
        _environment = environment;
        _zonaEcuador = ObtenerZonaEcuador();
    }

    public DateTime AhoraEcuador()
    {
        if (_environment.IsDevelopment() &&
            _fechaHoraSimulada.HasValue)
        {
            return _fechaHoraSimulada.Value;
        }

        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            _zonaEcuador);
    }

    public bool EstablecerFechaHoraSimulada(
        DateTime fechaHora)
    {
        if (!_environment.IsDevelopment())
        {
            return false;
        }

        _fechaHoraSimulada = fechaHora;

        return true;
    }

    public bool LimpiarFechaHoraSimulada()
    {
        if (!_environment.IsDevelopment())
        {
            return false;
        }

        _fechaHoraSimulada = null;

        return true;
    }

    public DateTime? ObtenerFechaHoraSimulada()
    {
        if (!_environment.IsDevelopment())
        {
            return null;
        }

        return _fechaHoraSimulada;
    }

    private static TimeZoneInfo ObtenerZonaEcuador()
    {
        if (OperatingSystem.IsWindows())
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "SA Pacific Standard Time");
        }

        return TimeZoneInfo.FindSystemTimeZoneById(
            "America/Guayaquil");
    }
}