export type TipoMarcacion =
  | 'Entrada'
  | 'InicioAlmuerzo'
  | 'FinAlmuerzo'
  | 'Salida';


export type OrigenMarcacion =
  | 'Facial'
  | 'Manual'
  | 'Regularización';


export interface MarcacionAdmin {

  idMarcacion:
    number;

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

  fechaMarcacion:
    string;

  fechaHora:
    string;

  tipoMarcacion:
    string;

  estadoMarcacion:
    string;

  observacion?:
    string | null;

  esFacial:
    boolean;

  origen?:
    string | null;

  similitud?:
    number | null;

  umbral?:
    number | null;

  aprobadoFacial?:
    boolean | null;

}


export interface ConsultaMarcacionesResponse {

  fecha:
    string;

  total:
    number;

  marcaciones:
    MarcacionAdmin[];

}


export interface CorreccionManualMarcacion {

  idEmpleado:
    number;

  fecha:
    string;

  hora:
    string;

  tipoMarcacion:
    TipoMarcacion;

  motivo:
    string;

}


export interface RegularizarMarcacion {

  idEmpleado:
    number;

  fecha:
    string;

  hora:
    string;

  tipoMarcacion:
    TipoMarcacion;

  motivo:
    string;

}


export interface RegularizarMarcacionResponse {

  idMarcacion:
    number;

  idEmpleado:
    number;

  empleado:
    string;

  fecha:
    string;

  hora:
    string;

  tipoMarcacion:
    string;

  estadoMarcacion:
    string;

  origen:
    string;

  motivo:
    string;

  mensaje:
    string;

}


export interface AnularMarcacion {

  motivo:
    string;

}