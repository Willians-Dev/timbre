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
  ActualizarJornada,
  CrearJornada,
  Jornada
} from './jornada.models';


@Injectable({
  providedIn: 'root'
})
export class JornadasService {

  private readonly apiUrl =
    `${environment.apiUrl}/jornadas`;


  constructor(
    private readonly http:
      HttpClient
  ) {
  }


  // =====================================================
  // LISTAR
  //
  // Método utilizado por la nueva pantalla Jornadas.
  // =====================================================

  listar():
    Observable<Jornada[]> {

    return this.http.get<
      Jornada[]
    >(
      this.apiUrl
    );

  }


  // =====================================================
  // OBTENER TODAS
  //
  // Se mantiene por compatibilidad con Empleados.
  // =====================================================

  obtenerTodas():
    Observable<Jornada[]> {

    return this.listar();

  }


  // =====================================================
  // OBTENER POR ID
  // =====================================================

  obtener(
    id: number
  ):
    Observable<Jornada> {

    return this.http.get<
      Jornada
    >(
      `${this.apiUrl}/${id}`
    );

  }


  // =====================================================
  // CREAR
  // =====================================================

  crear(
    dto: CrearJornada
  ):
    Observable<{
      idJornada: number;
      mensaje: string;
    }> {

    return this.http.post<{
      idJornada: number;
      mensaje: string;
    }>(
      this.apiUrl,
      dto
    );

  }


  // =====================================================
  // ACTUALIZAR
  // =====================================================

  actualizar(
    id: number,
    dto: ActualizarJornada
  ):
    Observable<{
      mensaje: string;
    }> {

    return this.http.put<{
      mensaje: string;
    }>(
      `${this.apiUrl}/${id}`,
      dto
    );

  }


  // =====================================================
  // ACTIVAR / DESACTIVAR
  // =====================================================

  cambiarEstado(
    id: number,
    activo: boolean
  ):
    Observable<{
      mensaje: string;
    }> {

    return this.http.patch<{
      mensaje: string;
    }>(
      `${this.apiUrl}/${id}/estado?activo=${activo}`,
      {}
    );

  }

}