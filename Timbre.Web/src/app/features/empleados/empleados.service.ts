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
  ActualizarEmpleadoRequest,
  CrearEmpleadoRequest,
  Empleado,
  RostroEmpleadoEstado
} from './empleado.models';


@Injectable({
  providedIn: 'root'
})
export class EmpleadosService {

  private readonly baseUrl =
    `${environment.apiUrl}/empleados`;


  constructor(
    private readonly http:
      HttpClient
  ) {
  }


  // =====================================================
  // LISTAR
  // =====================================================

  obtenerTodos():
    Observable<Empleado[]> {

    return this.http.get<Empleado[]>(
      this.baseUrl
    );

  }


  // =====================================================
  // OBTENER POR ID
  // =====================================================

  obtenerPorId(
    idEmpleado: number
  ): Observable<Empleado> {

    return this.http.get<Empleado>(
      `${this.baseUrl}/${idEmpleado}`
    );

  }


  // =====================================================
  // CREAR
  // =====================================================

  crear(
    request:
      CrearEmpleadoRequest
  ): Observable<Empleado> {

    return this.http.post<Empleado>(
      this.baseUrl,
      request
    );

  }


  // =====================================================
  // ACTUALIZAR
  // =====================================================

  actualizar(
    idEmpleado: number,
    request:
      ActualizarEmpleadoRequest
  ): Observable<Empleado> {

    return this.http.put<Empleado>(
      `${this.baseUrl}/${idEmpleado}`,
      request
    );

  }


  // =====================================================
  // ACTIVAR / DESACTIVAR
  // =====================================================

  cambiarEstado(
    idEmpleado: number,
    activo: boolean
  ): Observable<unknown> {

    return this.http.patch(
      `${this.baseUrl}/${idEmpleado}/estado?activo=${activo}`,
      {}
    );

  }


  // =====================================================
  // ROSTRO ACTIVO
  // =====================================================

  obtenerRostroActivo(
    idEmpleado: number
  ): Observable<RostroEmpleadoEstado> {

    return this.http.get<RostroEmpleadoEstado>(
      `${environment.apiUrl}/rostros/empleado/${idEmpleado}`
    );

  }


  // =====================================================
  // ENROLAR ROSTRO
  // =====================================================

  enrolarRostro(
    idEmpleado: number,
    archivos: Blob[]
  ): Observable<unknown> {

    if (
      archivos.length !== 3
    ) {

      throw new Error(
        'El enrolamiento requiere exactamente 3 fotografías.'
      );

    }


    const formData =
      new FormData();


    formData.append(
      'file1',
      archivos[0],
      'captura_1.jpg'
    );

    formData.append(
      'file2',
      archivos[1],
      'captura_2.jpg'
    );

    formData.append(
      'file3',
      archivos[2],
      'captura_3.jpg'
    );


    return this.http.post(
      `${environment.apiUrl}/rostros/empleado/${idEmpleado}/enrolar`,
      formData
    );

  }

}