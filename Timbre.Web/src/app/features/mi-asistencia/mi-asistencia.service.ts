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
  MiAsistenciaResponse
} from './mi-asistencia.models';


@Injectable({
  providedIn:
    'root'
})
export class MiAsistenciaService {

  private readonly apiUrl =
    `${environment.apiUrl}/marcaciones`;


  constructor(
    private readonly http:
      HttpClient
  ) {
  }


  consultar(
    fechaDesde:
      string,

    fechaHasta:
      string
  ):
    Observable<MiAsistenciaResponse> {

    const params =
      new HttpParams()
        .set(
          'fechaDesde',
          fechaDesde
        )
        .set(
          'fechaHasta',
          fechaHasta
        );


    return this.http.get<
      MiAsistenciaResponse
    >(
      `${this.apiUrl}/mi-asistencia`,
      {
        params
      }
    );

  }

}