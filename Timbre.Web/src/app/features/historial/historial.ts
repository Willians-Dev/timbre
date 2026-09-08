import {
  Component,
  computed,
  OnInit,
  signal
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  FormsModule
} from '@angular/forms';

import {
  HistorialService
} from './historial.service';

import {
  HistorialEmpleadoFiltro,
  HistorialMarcacion,
  HistorialRegistro
} from './historial.models';

import {
  NotificationService
} from '../../core/services/notification.service';


@Component({
  selector:
    'app-historial',

  standalone:
    true,

  imports: [
    CommonModule,
    FormsModule
  ],

  templateUrl:
    './historial.html',

  styleUrl:
    './historial.css'
})
export class Historial
  implements OnInit {

  // =====================================================
  // DATOS
  // =====================================================

  readonly registros =
    signal<
      HistorialRegistro[]
    >(
      []
    );


  readonly empleados =
    signal<
      HistorialEmpleadoFiltro[]
    >(
      []
    );


  readonly areas =
    signal<
      string[]
    >(
      []
    );


  readonly cargando =
    signal(
      false
    );


  // =====================================================
  // FILTROS
  // =====================================================

  readonly fechaDesde =
    signal(
      this.primerDiaMes()
    );


  readonly fechaHasta =
    signal(
      this.fechaActual()
    );


  readonly idEmpleado =
    signal<
      number |
      null
    >(
      null
    );


  readonly area =
    signal(
      ''
    );


  readonly estadoJornada =
    signal(
      ''
    );


  readonly estadoEntrada =
    signal(
      ''
    );


  readonly origen =
    signal(
      ''
    );


  readonly buscar =
    signal(
      ''
    );


  // =====================================================
  // DETALLE
  // =====================================================

  readonly registroDetalle =
    signal<
      HistorialRegistro |
      null
    >(
      null
    );


  // =====================================================
  // REGISTROS VISIBLES
  //
  // Búsqueda textual local.
  // =====================================================

  readonly registrosFiltrados =
    computed(
      () => {

        const texto =
          this.buscar()
            .trim()
            .toLowerCase();


        if (
          !texto
        ) {

          return this.registros();

        }


        return this.registros()
          .filter(
            registro => {

              return (
                registro
                  .empleado
                  .toLowerCase()
                  .includes(
                    texto
                  ) ||

                registro
                  .identificacion
                  .toLowerCase()
                  .includes(
                    texto
                  ) ||

                (
                  registro.area ??
                  ''
                )
                  .toLowerCase()
                  .includes(
                    texto
                  )
              );

            }
          );

      }
    );


  // =====================================================
  // CONTADORES
  // =====================================================

  readonly total =
    computed(
      () =>
        this.registrosFiltrados()
          .length
    );


  readonly completas =
    computed(
      () =>
        this.registrosFiltrados()
          .filter(
            r =>
              r.completa
          )
          .length
    );


  readonly incompletas =
    computed(
      () =>
        this.registrosFiltrados()
          .filter(
            r =>
              !r.completa
          )
          .length
    );


  readonly atrasos =
    computed(
      () =>
        this.registrosFiltrados()
          .filter(
            r =>
              r.estadoEntrada ===
              'Atraso'
          )
          .length
    );


  constructor(

    private readonly historialService:
      HistorialService,

    private readonly notificationService:
      NotificationService

  ) {
  }


  ngOnInit():
    void {

    this.cargarFiltros();

    this.consultar();

  }


  // =====================================================
  // FILTROS
  // =====================================================

  private cargarFiltros():
    void {

    this.historialService
      .obtenerFiltros()
      .subscribe({

        next:
          response => {

            this.empleados.set(
              response.empleados
            );


            this.areas.set(
              response.areas
            );

          },


        error:
          () => {

            this.notificationService
              .error(
                'No fue posible cargar los filtros del historial.',
                'Historial'
              );

          }

      });

  }


  // =====================================================
  // CONSULTAR
  // =====================================================

  consultar():
    void {

    if (
      !this.fechaDesde() ||
      !this.fechaHasta()
    ) {

      this.notificationService
        .warning(
          'Seleccione el rango de fechas.',
          'Historial'
        );


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


    this.historialService
      .consultar({

        fechaDesde:
          this.fechaDesde(),

        fechaHasta:
          this.fechaHasta(),

        idEmpleado:
          this.idEmpleado(),

        area:
          this.area(),

        estadoJornada:
          this.estadoJornada(),

        estadoEntrada:
          this.estadoEntrada(),

        origen:
          this.origen()

      })
      .subscribe({

        next:
          response => {

            this.registros.set(
              response.registros
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
                'No fue posible consultar el historial.',

                'Historial'

              );

          }

      });

  }


  // =====================================================
  // LIMPIAR FILTROS
  // =====================================================

  limpiarFiltros():
    void {

    this.fechaDesde.set(
      this.primerDiaMes()
    );


    this.fechaHasta.set(
      this.fechaActual()
    );


    this.idEmpleado.set(
      null
    );


    this.area.set(
      ''
    );


    this.estadoJornada.set(
      ''
    );


    this.estadoEntrada.set(
      ''
    );


    this.origen.set(
      ''
    );


    this.buscar.set(
      ''
    );


    this.consultar();

  }


  // =====================================================
  // DETALLE
  // =====================================================

  abrirDetalle(
    registro:
      HistorialRegistro
  ):
    void {

    this.registroDetalle.set(
      registro
    );

  }


  cerrarDetalle():
    void {

    this.registroDetalle.set(
      null
    );

  }


  // =====================================================
  // TIPO MARCACIÓN
  // =====================================================

  nombreTipo(
    tipo:
      string
  ):
    string {

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
  // ORIGEN
  // =====================================================

  origenMarcacion(
    marcacion:
      HistorialMarcacion
  ):
    string {

    return marcacion.esFacial
      ? 'Facial'
      : 'Manual';

  }


  // =====================================================
  // FECHA
  // =====================================================

  formatearFecha(
    fecha:
      string
  ):
    string {

    if (
      !fecha
    ) {

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
      partes.length !==
      3
    ) {

      return fecha;

    }


    return (
      `${partes[2]}/` +
      `${partes[1]}/` +
      partes[0]
    );

  }


  // =====================================================
  // HORA
  // =====================================================

  formatearHora(
    hora:
      string |
      null |
      undefined
  ):
    string {

    if (
      !hora
    ) {

      return '--:--';

    }


    return hora.substring(
      0,
      5
    );

  }


  // =====================================================
  // FECHAS DEFAULT
  // =====================================================

  private fechaActual():
    string {

    return this.fechaInput(
      new Date()
    );

  }


  private primerDiaMes():
    string {

    const hoy =
      new Date();


    return this.fechaInput(
      new Date(
        hoy.getFullYear(),
        hoy.getMonth(),
        1
      )
    );

  }


  private fechaInput(
    fecha:
      Date
  ):
    string {

    const year =
      fecha.getFullYear();


    const month =
      String(
        fecha.getMonth() +
        1
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


    return (
      `${year}-` +
      `${month}-` +
      day
    );

  }

}