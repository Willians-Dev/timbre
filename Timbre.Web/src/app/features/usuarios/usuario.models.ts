export interface Usuario {

  idUsuario: number;

  idEmpleado: number;

  empleado: string;

  idRol: number;

  rol: string;

  nombreUsuario: string;

  activo: boolean;

  ultimoAcceso?: string | null;

}


export interface RolUsuario {

  idRol: number;

  nombre: string;

  descripcion?: string | null;

}


export interface EmpleadoDisponible {

  idEmpleado: number;

  identificacion: string;

  nombreCompleto: string;

  area?: string | null;

  cargo?: string | null;

}


export interface CrearUsuarioRequest {

  idEmpleado: number;

  idRol: number;

  nombreUsuario: string;

  password: string;

}


export interface ActualizarUsuarioRequest {

  idRol: number;

  nombreUsuario: string;

  activo: boolean;

}


export interface ResetPasswordRequest {

  passwordNuevo: string;

  confirmarPassword: string;

}


export interface OperacionUsuarioResponse {

  idUsuario?: number;

  mensaje: string;

}