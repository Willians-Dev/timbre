export interface LoginRequest {
  nombreUsuario: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiracion?: string;
  idUsuario?: number;
  idEmpleado?: number;
  nombreUsuario?: string;
  rol?: string;
}