import {
  Component,
  computed,
  signal
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  FormsModule
} from '@angular/forms';

import {
  MiAsistenciaService
} from './mi-asistencia.service';

import {
  MiAsistenciaResponse,
  MiMarcacion
} from './mi-asistencia.models';

import {
  NotificationService
} from '../../core/services/notification.service';


@Component({
  selector:
    'app-mi-asistencia',

  standalone:
    true,

  imports: [
    CommonModule,
    FormsModule
  ],

  templateUrl:
    './mi-asistencia.html',

  styleUrl:
    './mi-asistencia.css'
})
export class MiAsistencia {

  // =====================================================
  // DATOS
  // =====================================================

  readonly datos =
    signal<
      MiAsistenciaResponse |
      null
    >(
      null
    );


  readonly cargando =
    signal(
      false
    );


  // =====================================================
  // RANGO
  // =====================================================

  readonly fechaDesde =
    signal(
      this.primerDiaMes()
    );


  readonly fechaHasta =
    signal(
      this.fechaActual()
    );


  // =====================================================
  // CONTADORES
  // =====================================================

  readonly diasRegistrados =
    computed(
      () =>
        this.datos()
          ?.dias
          .length ??
        0
    );


  readonly diasCompletos =
    computed(
      () =>
        this.datos()
          ?.dias
          .filter(
            dia =>
              dia.completa
          )
          .length ??
        0
    );


  readonly atrasos =
    computed(
      () =>
        this.datos()
          ?.dias
          .filter(
            dia =>
              dia.estadoEntrada ===
              'Atraso'
          )
          .length ??
        0
    );


  readonly totalMarcaciones =
    computed(
      () =>
        this.datos()
          ?.dias
          .reduce(
            (
              total,
              dia
            ) =>
              total +
              dia.marcaciones
                .filter(
                  marcacion =>
                    marcacion
                      .estadoMarcacion ===
                    'Activa'
                )
                .length,
            0
          ) ??
        0
    );


  constructor(
    private readonly miAsistenciaService:
      MiAsistenciaService,

    private readonly notificationService:
      NotificationService
  ) {

    this.consultar();

  }


  // =====================================================
  // CONSULTAR
  // =====================================================

  consultar(): void {

    if (
      !this.fechaDesde() ||
      !this.fechaHasta()
    ) {

      return;

    }


    if (
      this.fechaDesde() >
      this.fechaHasta()
    ) {

      this.notificationService
        .warning(
          'La fecha desde no puede ser mayor que la fecha hasta.',
          'Fechas incorrectas'
        );


      return;

    }


    this.cargando.set(
      true
    );


    this.miAsistenciaService
      .consultar(
        this.fechaDesde(),
        this.fechaHasta()
      )
      .subscribe({

        next:
          response => {

            this.datos.set(
              response
            );


            this.cargando.set(
              false
            );

          },


        error:
          error => {

            this.cargando.set(
              false
            );


            this.notificationService
              .error(

                error?.error?.mensaje ??
                'No fue posible consultar su asistencia.',

                'Mi asistencia'

              );

          }

      });

  }


  // =====================================================
  // TIPO MARCACIÓN
  // =====================================================

  nombreTipo(
    tipo:
      string
  ): string {

    switch (
      tipo
    ) {

      case 'InicioAlmuerzo':

        return 'Inicio almuerzo';


      case 'FinAlmuerzo':

        return 'Fin almuerzo';


      default:

        return tipo;

    }

  }


  // =====================================================
  // MARCACIÓN DEL DÍA
  // =====================================================

  obtenerMarcacion(
    tipo:
      string
  ):
    MiMarcacion |
    undefined {

    const hoy =
      this.datos()
        ?.estadoHoy
        .fecha;


    const dia =
      this.datos()
        ?.dias
        .find(
          item =>
            item.fecha ===
            hoy
        );


    return dia
      ?.marcaciones
      .find(
        marcacion =>
          marcacion
            .tipoMarcacion ===
            tipo &&
          marcacion
            .estadoMarcacion ===
            'Activa'
      );

  }


  // =====================================================
  // FECHA
  // =====================================================

  formatearFecha(
    fecha:
      string
  ): string {

    if (!fecha) {

      return '-';

    }


    const partes =
      fecha
        .substring(
          0,
          10
        )
        .split(
          '-'
        );


    if (
      partes.length !== 3
    ) {

      return fecha;

    }


    return `${partes[2]}/${partes[1]}/${partes[0]}`;

  }


  // =====================================================
  // HORAS
  // =====================================================

  formatearHora(
    hora:
      string |
      null |
      undefined
  ): string {

    if (!hora) {

      return '--:--';

    }


    return hora.substring(
      0,
      5
    );

  }


  // =====================================================
  // FECHA ACTUAL
  // =====================================================

  private fechaActual():
    string {

    return this.formatearFechaInput(
      new Date()
    );

  }


  private primerDiaMes():
    string {

    const hoy =
      new Date();


    const fecha =
      new Date(
        hoy.getFullYear(),
        hoy.getMonth(),
        1
      );


    return this.formatearFechaInput(
      fecha
    );

  }


  private formatearFechaInput(
    fecha:
      Date
  ): string {

    const year =
      fecha.getFullYear();


    const month =
      String(
        fecha.getMonth() + 1
      )
        .padStart(
          2,
          '0'
        );


    const day =
      String(
        fecha.getDate()
      )
        .padStart(
          2,
          '0'
        );


    return `${year}-${month}-${day}`;

  }

}