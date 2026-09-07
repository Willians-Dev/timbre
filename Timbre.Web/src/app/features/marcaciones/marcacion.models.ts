export type TipoMarcacion =
  | 'Entrada'
  | 'InicioAlmuerzo'
  | 'FinAlmuerzo'
  | 'Salida';


export interface MarcacionAdmin {

  idMarcacion: number;

  idEmpleado: number;

  identificacion: string;

  empleado: string;

  area?: string | null;

  cargo?: string | null;

  fechaMarcacion: string;

  fechaHora: string;

  tipoMarcacion: string;

  estadoMarcacion: string;

  observacion?: string | null;

  esFacial: boolean;

  similitud?: number | null;

  umbral?: number | null;

  aprobadoFacial?: boolean | null;
}


export interface ConsultaMarcacionesResponse {

  fecha: string;

  total: number;

  marcaciones:
    MarcacionAdmin[];
}


export interface CorreccionManualMarcacion {

  idEmpleado: number;

  fecha: string;

  hora: string;

  tipoMarcacion:
    TipoMarcacion;

  motivo: string;
}


export interface AnularMarcacion {

  motivo: string;
}