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
  AnularMarcacion,
  ConsultaMarcacionesResponse,
  CorreccionManualMarcacion
} from './marcacion.models';


@Injectable({
  providedIn: 'root'
})
export class MarcacionesService {

  private readonly adminUrl =
    `${environment.apiUrl}/marcaciones-admin`;


  constructor(
    private readonly http:
      HttpClient
  ) {
  }


  consultar(
    fecha: string
  ):
    Observable<
      ConsultaMarcacionesResponse
    > {

    return this.http.get<
      ConsultaMarcacionesResponse
    >(
      `${this.adminUrl}/consulta?fecha=${fecha}`
    );
  }


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


  anular(
    idMarcacion: number,
    dto: AnularMarcacion
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