export interface AuditoriaRegistro {

  idAuditoria: number;

  idUsuario?: number | null;

  usuario?: string | null;

  accion: string;

  entidad?: string | null;

  idEntidad?: number | null;

  valorAnterior?: string | null;

  valorNuevo?: string | null;

  ipOrigen?: string | null;

  fechaAccion: string;
}


export interface AuditoriaFiltros {

  fechaDesde?: string;

  fechaHasta?: string;

  accion?: string;

  entidad?: string;

  idUsuario?: number | null;

  idEntidad?: number | null;
}