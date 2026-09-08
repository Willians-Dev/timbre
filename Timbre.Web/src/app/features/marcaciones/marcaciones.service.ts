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
  AnularMarcacion,
  ConsultaMarcacionesResponse,
  CorreccionManualMarcacion,
  RegularizarMarcacion,
  RegularizarMarcacionResponse
} from './marcacion.models';


@Injectable({
  providedIn:
    'root'
})
export class MarcacionesService {

  private readonly adminUrl =
    `${environment.apiUrl}/marcaciones-admin`;


  constructor(
    private readonly http:
      HttpClient
  ) {
  }


  // =====================================================
  // CONSULTAR
  // =====================================================

  consultar(
    fecha:
      string
  ):
    Observable<
      ConsultaMarcacionesResponse
    > {

    const params =
      new HttpParams()
        .set(
          'fecha',
          fecha
        );


    return this.http.get<
      ConsultaMarcacionesResponse
    >(
      `${this.adminUrl}/consulta`,
      {
        params
      }
    );

  }


  // =====================================================
  // CORRECCIÓN MANUAL
  // =====================================================

  crearCorreccion(
    dto:
      CorreccionManualMarcacion
  ):
    Observable<{
      idMarcacion: number;
      mensaje: string;
    }> {

    return this.http.post<{
      idMarcacion: number;
      mensaje: string;
    }>(
      `${this.adminUrl}/correccion`,
      dto
    );

  }


  // =====================================================
  // REGULARIZAR MARCACIÓN FALTANTE
  // =====================================================

  regularizar(
    dto:
      RegularizarMarcacion
  ):
    Observable<
      RegularizarMarcacionResponse
    > {

    return this.http.post<
      RegularizarMarcacionResponse
    >(
      `${this.adminUrl}/regularizar`,
      dto
    );

  }


  // =====================================================
  // ANULAR
  // =====================================================

  anular(
    idMarcacion:
      number,

    dto:
      AnularMarcacion
  ):
    Observable<{
      mensaje: string;
    }> {

    return this.http.patch<{
      mensaje: string;
    }>(
      `${this.adminUrl}/${idMarcacion}/anular`,
      dto
    );

  }

}