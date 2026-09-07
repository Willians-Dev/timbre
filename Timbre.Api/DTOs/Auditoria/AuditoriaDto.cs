namespace Timbre.Api.DTOs.Auditoria;

public class AuditoriaDto
{
    public long IdAuditoria { get; set; }

    public long? IdUsuario { get; set; }

    public string? Usuario { get; set; }

    public string Accion { get; set; } = string.Empty;

    public string Entidad { get; set; } = string.Empty;

    public string? IdEntidad { get; set; }

    public string? ValorAnterior { get; set; }

    public string? ValorNuevo { get; set; }

    public string? IpOrigen { get; set; }

    public DateTime FechaHora { get; set; }
}