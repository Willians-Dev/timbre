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
  FormatoReporte,
  ReporteFiltrosResponse,
  TipoReporte
} from './reportes.models';


@Injectable({
  providedIn:
    'root'
})
export class ReportesService {

  private readonly apiReportes =
    `${environment.apiUrl}/reportes`;


  private readonly apiHistorial =
    `${environment.apiUrl}/historial`;


  constructor(
    private readonly http:
      HttpClient
  ) {
  }


  // =====================================================
  // FILTROS
  // =====================================================

  obtenerFiltros():
    Observable<ReporteFiltrosResponse> {

    return this.http.get<
      ReporteFiltrosResponse
    >(
      `${this.apiHistorial}/filtros`
    );

  }


  // =====================================================
  // DESCARGAR
  // =====================================================

  descargar(
    tipo:
      TipoReporte,

    formato:
      FormatoReporte,

    filtros: {
      fechaDesde:
        string;

      fechaHasta:
        string;

      idEmpleado?:
        number | null;

      area?:
        string;

      origen?:
        string;
    }
  ):
    Observable<Blob> {

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
      tipo ===
        'marcaciones' &&
      filtros.origen
    ) {

      params =
        params.set(
          'origen',
          filtros.origen
        );

    }


    return this.http.get(
      `${this.apiReportes}/${tipo}/${formato}`,
      {
        params,

        responseType:
          'blob'
      }
    );

  }

}