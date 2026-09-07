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


export interface EstadoEntradaMarcacion {

  estado: string;

  minutosAtraso: number;

  horaEntradaProgramada: string;

  toleranciaMinutos: number;

  horaLimitePuntual: string;

  horaMarcacion: string;
}


export interface MarcacionFacialResultado {

  reconocido: boolean;

  idEmpleado: number;

  empleado: string;

  similitud?: number | null;

  umbral?: number | null;

  confianzaDeteccion?: number | null;

  idMarcacion: number;

  idValidacion?: number | null;

  fecha: string;

  hora: string;

  tipoMarcacion: string;

  estadoEntrada?:
    EstadoEntradaMarcacion |
    null;

  completa: boolean;

  marcacionesFaltantes:
    string[];

  mensaje: string;
}


@Injectable({
  providedIn: 'root'
})
export class KioscoService {

  private readonly apiUrl =
    `${environment.apiUrl}/marcaciones-faciales`;


  constructor(
    private readonly http:
      HttpClient
  ) {
  }


  // =====================================================
  // REGISTRAR MARCACIÓN FACIAL
  // =====================================================

  registrarMarcacion(
    imagen:
      Blob
  ):
    Observable<MarcacionFacialResultado> {

    const formData =
      new FormData();


    formData.append(
      'file',
      imagen,
      'captura.jpg'
    );


    return this.http.post<
      MarcacionFacialResultado
    >(
      `${this.apiUrl}/registrar`,
      formData
    );

  }

}