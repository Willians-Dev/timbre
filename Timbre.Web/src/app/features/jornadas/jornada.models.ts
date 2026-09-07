export interface Jornada {

  idJornada: number;

  nombre: string;

  horaEntrada: string;

  horaInicioAlmuerzo: string;

  horaFinAlmuerzo: string;

  horaSalida: string;

  toleranciaEntradaMinutos: number;

  lunes: boolean;

  martes: boolean;

  miercoles: boolean;

  jueves: boolean;

  viernes: boolean;

  sabado: boolean;

  domingo: boolean;

  activo: boolean;
}


/*
 * Alias para el nuevo módulo de Jornadas.
 *
 * Permite utilizar:
 *
 * Jornada
 *
 * o:
 *
 * JornadaLaboral
 *
 * sin romper el módulo de Empleados.
 */
export type JornadaLaboral =
  Jornada;


export interface CrearJornada {

  nombre: string;

  horaEntrada: string;

  horaInicioAlmuerzo: string;

  horaFinAlmuerzo: string;

  horaSalida: string;

  toleranciaEntradaMinutos: number;

  lunes: boolean;

  martes: boolean;

  miercoles: boolean;

  jueves: boolean;

  viernes: boolean;

  sabado: boolean;

  domingo: boolean;
}


export interface ActualizarJornada
  extends CrearJornada {

  activo: boolean;
}