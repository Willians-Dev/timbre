import {
  Injectable
} from '@angular/core';

import {
  HttpClient,
  HttpParams
} from '@angular/common/http';

import {
  Observable
} from 'rxjs';

import {
  environment
} from '../../../environments/environment';

import {
  AuditoriaFiltros,
  AuditoriaRegistro
} from './auditoria.models';


@Injectable({
  providedIn: 'root'
})
export class AuditoriaService {

  private readonly apiUrl =
    `${environment.apiUrl}/auditoria`;


  constructor(
    private readonly http:
      HttpClient
  ) {
  }


  // =====================================================
  // CONSULTAR
  // =====================================================

  consultar(
    filtros:
      AuditoriaFiltros
  ):
    Observable<AuditoriaRegistro[]> {

    let params =
      new HttpParams();


    if (
      filtros.fechaDesde
    ) {

      params =
        params.set(
          'fechaDesde',
          filtros.fechaDesde
        );

    }


    if (
      filtros.fechaHasta
    ) {

      params =
        params.set(
          'fechaHasta',
          filtros.fechaHasta
        );

    }


    if (
      filtros.accion
    ) {

      params =
        params.set(
          'accion',
          filtros.accion
        );

    }


    if (
      filtros.entidad
    ) {

      params =
        params.set(
          'entidad',
          filtros.entidad
        );

    }


    if (
      filtros.idUsuario
    ) {

      params =
        params.set(
          'idUsuario',
          filtros.idUsuario
            .toString()
        );

    }


    if (
      filtros.idEntidad
    ) {

      params =
        params.set(
          'idEntidad',
          filtros.idEntidad
            .toString()
        );

    }


    return this.http.get<
      AuditoriaRegistro[]
    >(
      this.apiUrl,
      {
        params
      }
    );

  }


  // =====================================================
  // DETALLE
  // =====================================================

  obtenerPorId(
    idAuditoria:
      number
  ):
    Observable<AuditoriaRegistro> {

    return this.http.get<
      AuditoriaRegistro
    >(
      `${this.apiUrl}/${idAuditoria}`
    );

  }


  // =====================================================
  // TRAZABILIDAD
  // =====================================================

  obtenerTrazabilidad(
    entidad:
      string,

    idEntidad:
      number
  ):
    Observable<AuditoriaRegistro[]> {

    return this.http.get<
      AuditoriaRegistro[]
    >(
      `${this.apiUrl}/entidad/${encodeURIComponent(entidad)}/${idEntidad}`
    );

  }

}