export interface Empleado {
  idEmpleado: number;
  identificacion: string;
  nombres: string;
  apellidos: string;
  nombreCompleto: string;
  correo?: string | null;
  telefono?: string | null;
  area?: string | null;
  cargo?: string | null;
  fechaIngreso: string;
  fechaSalida?: string | null;
  idJornada: number;
  jornada?: string | null;
  activo: boolean;
}

export interface CrearEmpleadoRequest {
  identificacion: string;
  nombres: string;
  apellidos: string;
  correo?: string | null;
  telefono?: string | null;
  area?: string | null;
  cargo?: string | null;
  fechaIngreso: string;
  idJornada: number;
}

export interface ActualizarEmpleadoRequest {
  identificacion: string;
  nombres: string;
  apellidos: string;
  correo?: string | null;
  telefono?: string | null;
  area?: string | null;
  cargo?: string | null;
  fechaIngreso: string;
  fechaSalida?: string | null;
  idJornada: number;
  activo: boolean;
}

export interface RostroEmpleadoEstado {
  idRostro: number;
  idEmpleado: number;
  modelo: string;
  dimensionEmbedding: number;
  activo: boolean;
  fechaRegistro: string;
  fechaModificacion?: string | null;
}