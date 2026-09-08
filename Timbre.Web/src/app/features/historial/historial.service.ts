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
  HistorialFiltrosResponse,
  HistorialResponse
} from './historial.models';


@Injectable({
  providedIn:
    'root'
})
export class HistorialService {

  private readonly apiUrl =
    `${environment.apiUrl}/historial`;


  constructor(
    private readonly http:
      HttpClient
  ) {
  }


  consultar(
    filtros: {
      fechaDesde:
        string;

      fechaHasta:
        string;

      idEmpleado?:
        number | null;

      area?:
        string;

      estadoJornada?:
        string;

      estadoEntrada?:
        string;

      origen?:
        string;
    }
  ):
    Observable<HistorialResponse> {

    let params =
      new HttpParams()
        .set(
          'fechaDesde',
          filtros.fechaDesde
        )
        .set(
          'fechaHasta',
          filtros.fechaHasta
        );


    if (
      filtros.idEmpleado
    ) {

      params =
        params.set(
          'idEmpleado',
          filtros.idEmpleado
            .toString()
        );

    }


    if (
      filtros.area
    ) {

      params =
        params.set(
          'area',
          filtros.area
        );

    }


    if (
      filtros.estadoJornada
    ) {

      params =
        params.set(
          'estadoJornada',
          filtros.estadoJornada
        );

    }


    if (
      filtros.estadoEntrada
    ) {

      params =
        params.set(
          'estadoEntrada',
          filtros.estadoEntrada
        );

    }


    if (
      filtros.origen
    ) {

      params =
        params.set(
          'origen',
          filtros.origen
        );

    }


    return this.http.get<
      HistorialResponse
    >(
      this.apiUrl,
      {
        params
      }
    );

  }


  obtenerFiltros():
    Observable<HistorialFiltrosResponse> {

    return this.http.get<
      HistorialFiltrosResponse
    >(
      `${this.apiUrl}/filtros`
    );

  }

}