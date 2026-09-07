import {
  Injectable
} from '@angular/core';

import {
  HttpClient
} from '@angular/common/http';

import {
  Observable
} from 'rxjs';

import {
  environment
} from '../../../environments/environment';

import {
  ActualizarUsuarioRequest,
  CrearUsuarioRequest,
  EmpleadoDisponible,
  OperacionUsuarioResponse,
  ResetPasswordRequest,
  RolUsuario,
  Usuario
} from './usuario.models';


@Injectable({
  providedIn: 'root'
})
export class UsuariosService {

  private readonly baseUrl =
    `${environment.apiUrl}/usuarios`;


  constructor(
    private readonly http:
      HttpClient
  ) {
  }


  // =====================================================
  // USUARIOS
  // =====================================================

  obtenerTodos():
    Observable<Usuario[]> {

    return this.http.get<Usuario[]>(
      this.baseUrl
    );

  }


  obtenerPorId(
    idUsuario: number
  ): Observable<Usuario> {

    return this.http.get<Usuario>(
      `${this.baseUrl}/${idUsuario}`
    );

  }


  crear(
    request: CrearUsuarioRequest
  ): Observable<OperacionUsuarioResponse> {

    return this.http.post<OperacionUsuarioResponse>(
      this.baseUrl,
      request
    );

  }


  actualizar(
    idUsuario: number,
    request: ActualizarUsuarioRequest
  ): Observable<OperacionUsuarioResponse> {

    return this.http.put<OperacionUsuarioResponse>(
      `${this.baseUrl}/${idUsuario}`,
      request
    );

  }


  cambiarEstado(
    idUsuario: number,
    activo: boolean
  ): Observable<OperacionUsuarioResponse> {

    return this.http.patch<OperacionUsuarioResponse>(
      `${this.baseUrl}/${idUsuario}/estado?activo=${activo}`,
      {}
    );

  }


  resetPassword(
    idUsuario: number,
    request: ResetPasswordRequest
  ): Observable<OperacionUsuarioResponse> {

    return this.http.post<OperacionUsuarioResponse>(
      `${this.baseUrl}/${idUsuario}/reset-password`,
      request
    );

  }


  // =====================================================
  // CATÁLOGOS
  // =====================================================

  obtenerRoles():
    Observable<RolUsuario[]> {

    return this.http.get<RolUsuario[]>(
      `${this.baseUrl}/catalogos/roles`
    );

  }


  obtenerEmpleadosDisponibles(
    idUsuarioActual?: number
  ): Observable<EmpleadoDisponible[]> {

    const url =
      idUsuarioActual
        ? `${this.baseUrl}/catalogos/empleados-disponibles?idUsuarioActual=${idUsuarioActual}`
        : `${this.baseUrl}/catalogos/empleados-disponibles`;

    return this.http.get<EmpleadoDisponible[]>(
      url
    );

  }

}