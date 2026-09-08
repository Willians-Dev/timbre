export interface HistorialMarcacion {

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


export interface HistorialRegistro {

  idEmpleado:
    number;

  identificacion:
    string;

  empleado:
    string;

  area?:
    string | null;

  cargo?:
    string | null;

  fecha:
    string;

  entrada?:
    string | null;

  inicioAlmuerzo?:
    string | null;

  finAlmuerzo?:
    string | null;

  salida?:
    string | null;

  estadoEntrada?:
    string | null;

  minutosAtraso:
    number;

  completa:
    boolean;

  estadoJornada:
    string;

  tieneFacial:
    boolean;

  tieneManual:
    boolean;

  origen:
    string;

  marcaciones:
    HistorialMarcacion[];
}


export interface HistorialResponse {

  fechaDesde:
    string;

  fechaHasta:
    string;

  total:
    number;

  registros:
    HistorialRegistro[];
}


export interface HistorialEmpleadoFiltro {

  idEmpleado:
    number;

  nombre:
    string;

  area?:
    string | null;
}


export interface HistorialFiltrosResponse {

  empleados:
    HistorialEmpleadoFiltro[];

  areas:
    string[];
}