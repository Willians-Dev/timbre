export interface MarcacionFacialResultado {

  reconocido: boolean;

  idEmpleado: number;

  empleado: string;

  similitud?: number;

  umbral?: number;

  confianzaDeteccion?: number;

  idMarcacion: number;

  idValidacion?: number;

  fecha?: string;

  hora?: string;

  tipoMarcacion: string;

  estadoEntrada?: unknown;

  completa?: boolean;

  marcacionesFaltantes?: string[];

  mensaje?: string;

}


export type EstadoKiosco =
  | 'inicializando'
  | 'listo'
  | 'procesando'
  | 'exito'
  | 'error';