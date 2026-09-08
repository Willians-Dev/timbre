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
  finalize
} from 'rxjs';

import {
  ReportesService
} from './reportes.service';

import {
  FormatoReporte,
  ReporteEmpleadoFiltro,
  TipoReporte
} from './reportes.models';

import {
  NotificationService
} from '../../core/services/notification.service';


@Component({
  selector:
    'app-reportes',

  standalone:
    true,

  imports: [
    CommonModule,
    FormsModule
  ],

  templateUrl:
    './reportes.html',

  styleUrl:
    './reportes.css'
})
export class Reportes
  implements OnInit {

  // =====================================================
  // CATÁLOGOS
  // =====================================================

  readonly empleados =
    signal<
      ReporteEmpleadoFiltro[]
    >(
      []
    );


  readonly areas =
    signal<
      string[]
    >(
      []
    );


  // =====================================================
  // FILTROS
  // =====================================================

  readonly tipoReporte =
    signal<TipoReporte>(
      'asistencia'
    );


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


  readonly origen =
    signal(
      ''
    );


  // =====================================================
  // ESTADO
  // =====================================================

  readonly cargandoFiltros =
    signal(
      false
    );


  readonly formatoDescargando =
    signal<
      FormatoReporte |
      null
    >(
      null
    );


  readonly descargando =
    computed(
      () =>
        this.formatoDescargando() !==
        null
    );


  // =====================================================
  // INFO
  // =====================================================

  readonly nombreReporte =
    computed(
      () => {

        switch (
          this.tipoReporte()
        ) {

          case 'atrasos':

            return 'Reporte de atrasos';


          case 'incompletas':

            return 'Jornadas incompletas';


          case 'marcaciones':

            return 'Marcaciones detalladas';


          default:

            return 'Asistencia por período';

        }

      }
    );


  readonly descripcionReporte =
    computed(
      () => {

        switch (
          this.tipoReporte()
        ) {

          case 'atrasos':

            return 'Lista las entradas registradas después del horario permitido.';


          case 'incompletas':

            return 'Lista las jornadas que no cuentan con todas las marcaciones requeridas.';


          case 'marcaciones':

            return 'Muestra el detalle individual de cada marcación registrada.';


          default:

            return 'Genera el consolidado general de asistencia por empleado y fecha.';

        }

      }
    );


  readonly empleadoSeleccionado =
    computed(
      () => {

        const id =
          this.idEmpleado();


        if (
          !id
        ) {

          return 'Todos';

        }


        return this.empleados()
          .find(
            e =>
              e.idEmpleado ===
              id
          )
          ?.nombre ??
          'Empleado seleccionado';

      }
    );


  constructor(

    private readonly reportesService:
      ReportesService,

    private readonly notificationService:
      NotificationService

  ) {
  }


  ngOnInit():
    void {

    this.cargarFiltros();

  }


  // =====================================================
  // CAMBIAR TIPO
  // =====================================================

  seleccionarTipo(
    tipo:
      TipoReporte
  ):
    void {

    if (
      this.descargando()
    ) {

      return;

    }


    this.tipoReporte.set(
      tipo
    );


    if (
      tipo !==
      'marcaciones'
    ) {

      this.origen.set(
        ''
      );

    }

  }


  // =====================================================
  // CARGAR FILTROS
  // =====================================================

  private cargarFiltros():
    void {

    this.cargandoFiltros.set(
      true
    );


    this.reportesService
      .obtenerFiltros()
      .pipe(

        finalize(
          () => {

            this.cargandoFiltros.set(
              false
            );

          }
        )

      )
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
          error => {

            this.notificationService
              .error(

                error?.error?.mensaje ??
                'No fue posible cargar los filtros de reportes.',

                'Reportes'

              );

          }

      });

  }


  // =====================================================
  // EXCEL
  // =====================================================

  descargarExcel():
    void {

    this.descargarReporte(
      'excel'
    );

  }


  // =====================================================
  // PDF
  // =====================================================

  descargarPdf():
    void {

    this.descargarReporte(
      'pdf'
    );

  }


  // =====================================================
  // DESCARGAR
  // =====================================================

  private descargarReporte(
    formato:
      FormatoReporte
  ):
    void {

    if (
      this.descargando()
    ) {

      return;

    }


    if (
      !this.fechaDesde() ||
      !this.fechaHasta()
    ) {

      this.notificationService
        .warning(
          'Seleccione el rango de fechas.',
          'Reportes'
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


    this.formatoDescargando.set(
      formato
    );


    this.reportesService
      .descargar(
        this.tipoReporte(),
        formato,
        {
          fechaDesde:
            this.fechaDesde(),

          fechaHasta:
            this.fechaHasta(),

          idEmpleado:
            this.idEmpleado(),

          area:
            this.area(),

          origen:
            this.origen()
        }
      )
      .pipe(

        finalize(
          () => {

            this.formatoDescargando.set(
              null
            );

          }
        )

      )
      .subscribe({

        next:
          blob => {

            this.guardarArchivo(
              blob,
              formato
            );


            this.notificationService
              .success(

                formato ===
                'pdf'
                  ? 'El reporte PDF se generó correctamente.'
                  : 'El reporte Excel se generó correctamente.',

                'Reporte generado'

              );

          },


        error:
          async error => {

            const mensaje =
              await this.obtenerMensajeError(
                error
              );


            this.notificationService
              .error(
                mensaje,
                'Reportes'
              );

          }

      });

  }


  // =====================================================
  // LIMPIAR FILTROS
  // =====================================================

  limpiarFiltros():
    void {

    if (
      this.descargando()
    ) {

      return;

    }


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


    this.origen.set(
      ''
    );

  }


  // =====================================================
  // GUARDAR ARCHIVO
  // =====================================================

  private guardarArchivo(
    blob:
      Blob,

    formato:
      FormatoReporte
  ):
    void {

    const url =
      window.URL
        .createObjectURL(
          blob
        );


    const enlace =
      document
        .createElement(
          'a'
        );


    enlace.href =
      url;


    enlace.download =
      this.obtenerNombreArchivo(
        formato
      );


    document.body
      .appendChild(
        enlace
      );


    enlace.click();


    enlace.remove();


    window.URL
      .revokeObjectURL(
        url
      );

  }


  // =====================================================
  // NOMBRE ARCHIVO
  // =====================================================

  private obtenerNombreArchivo(
    formato:
      FormatoReporte
  ):
    string {

    let prefijo:
      string;


    switch (
      this.tipoReporte()
    ) {

      case 'atrasos':

        prefijo =
          'Atrasos';

        break;


      case 'incompletas':

        prefijo =
          'Jornadas_Incompletas';

        break;


      case 'marcaciones':

        prefijo =
          'Marcaciones';

        break;


      default:

        prefijo =
          'Asistencia';

        break;

    }


    const desde =
      this.fechaDesde()
        .replaceAll(
          '-',
          ''
        );


    const hasta =
      this.fechaHasta()
        .replaceAll(
          '-',
          ''
        );


    const extension =
      formato ===
      'pdf'
        ? 'pdf'
        : 'xlsx';


    return (
      `${prefijo}_` +
      `${desde}_` +
      `${hasta}.` +
      extension
    );

  }


  // =====================================================
  // ERROR BLOB
  // =====================================================

  private async obtenerMensajeError(
    error:
      any
  ):
    Promise<string> {

    const mensajeDefault =
      'No fue posible generar el reporte.';


    if (
      error?.error instanceof Blob
    ) {

      try {

        const texto =
          await error.error
            .text();


        const respuesta =
          JSON.parse(
            texto
          );


        return (
          respuesta?.mensaje ??
          mensajeDefault
        );

      }
      catch {

        return mensajeDefault;

      }

    }


    return (
      error?.error?.mensaje ??
      mensajeDefault
    );

  }


  // =====================================================
  // FECHA ACTUAL
  // =====================================================

  private fechaActual():
    string {

    return this.fechaInput(
      new Date()
    );

  }


  // =====================================================
  // PRIMER DÍA DEL MES
  // =====================================================

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


  // =====================================================
  // FORMATO INPUT DATE
  // =====================================================

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