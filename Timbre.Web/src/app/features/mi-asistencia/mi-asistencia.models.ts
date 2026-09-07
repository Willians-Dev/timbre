export interface MiAsistenciaEmpleado {

  idEmpleado:
    number;

  identificacion:
    string;

  nombre:
    string;

  area?:
    string | null;

  cargo?:
    string | null;
}


export interface MiAsistenciaJornada {

  nombre:
    string;

  horaEntrada:
    string;

  horaInicioAlmuerzo:
    string;

  horaFinAlmuerzo:
    string;

  horaSalida:
    string;

  toleranciaEntradaMinutos:
    number;
}


export interface MiEstadoHoy {

  idEmpleado:
    number;

  empleado:
    string;

  fecha:
    string;

  tieneEntrada:
    boolean;

  tieneInicioAlmuerzo:
    boolean;

  tieneFinAlmuerzo:
    boolean;

  tieneSalida:
    boolean;

  estadoEntrada?:
    string | null;

  minutosAtraso:
    number;

  completa:
    boolean;

  marcacionesFaltantes:
    string[];
}


export interface MiMarcacion {

  idMarcacion:
    number;

  hora:
    string;

  tipoMarcacion:
    string;

  estadoMarcacion:
    string;

  observacion?:
    string | null;

  esFacial:
    boolean;
}


export interface MiAsistenciaDia {

  fecha:
    string;

  estadoEntrada?:
    string | null;

  minutosAtraso:
    number;

  completa:
    boolean;

  marcaciones:
    MiMarcacion[];
}


export interface MiAsistenciaResponse {

  empleado:
    MiAsistenciaEmpleado;

  jornada:
    MiAsistenciaJornada;

  estadoHoy:
    MiEstadoHoy;

  fechaDesde:
    string;

  fechaHasta:
    string;

  dias:
    MiAsistenciaDia[];
}