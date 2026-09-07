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
  AuditoriaService
} from './auditoria.service';

import {
  AuditoriaRegistro
} from './auditoria.models';

import {
  NotificationService
} from '../../core/services/notification.service';


@Component({
  selector:
    'app-auditoria',

  standalone:
    true,

  imports: [
    CommonModule,
    FormsModule
  ],

  templateUrl:
    './auditoria.html',

  styleUrl:
    './auditoria.css'
})
export class Auditoria {

  // =====================================================
  // DATOS
  // =====================================================

  readonly registros =
    signal<AuditoriaRegistro[]>([]);


  readonly cargando =
    signal(false);


  // =====================================================
  // FILTROS
  // =====================================================

  readonly fechaDesde =
    signal(
      this.obtenerFechaActual()
    );


  readonly fechaHasta =
    signal(
      this.obtenerFechaActual()
    );


  readonly accion =
    signal('');


  readonly entidad =
    signal('');


  readonly usuario =
    signal('');


  readonly idEntidad =
    signal('');


  readonly busqueda =
    signal('');


  // =====================================================
  // MODALES
  // =====================================================

  readonly modalDetalle =
    signal(false);


  readonly modalTrazabilidad =
    signal(false);


  readonly registroSeleccionado =
    signal<AuditoriaRegistro | null>(
      null
    );


  readonly trazabilidad =
    signal<AuditoriaRegistro[]>([]);


  readonly cargandoTrazabilidad =
    signal(false);


  // =====================================================
  // LISTAS DINÁMICAS
  // =====================================================

  readonly acciones =
    computed(
      () => {

        return [
          ...new Set(
            this.registros()
              .map(
                x =>
                  x.accion
              )
              .filter(
                x =>
                  !!x
              )
          )
        ]
          .sort();

      }
    );


  readonly entidades =
    computed(
      () => {

        return [
          ...new Set(
            this.registros()
              .map(
                x =>
                  x.entidad
              )
              .filter(
                (
                  x
                ): x is string =>
                  !!x
              )
          )
        ]
          .sort();

      }
    );


  // =====================================================
  // FILTRADO LOCAL
  // =====================================================

  readonly registrosFiltrados =
    computed(
      () => {

        const texto =
          this.busqueda()
            .trim()
            .toLowerCase();


        const usuarioFiltro =
          this.usuario()
            .trim()
            .toLowerCase();


        return this.registros()
          .filter(
            registro => {

              const coincideTexto =
                !texto ||

                registro.accion
                  .toLowerCase()
                  .includes(
                    texto
                  ) ||

                (
                  registro.entidad ??
                  ''
                )
                  .toLowerCase()
                  .includes(
                    texto
                  ) ||

                (
                  registro.usuario ??
                  ''
                )
                  .toLowerCase()
                  .includes(
                    texto
                  ) ||

                (
                  registro.ipOrigen ??
                  ''
                )
                  .toLowerCase()
                  .includes(
                    texto
                  );


              const coincideUsuario =
                !usuarioFiltro ||

                (
                  registro.usuario ??
                  ''
                )
                  .toLowerCase()
                  .includes(
                    usuarioFiltro
                  );


              return (
                coincideTexto &&
                coincideUsuario
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


  readonly totalUsuarios =
    computed(
      () => {

        return new Set(
          this.registrosFiltrados()
            .map(
              x =>
                x.idUsuario
            )
            .filter(
              x =>
                x !== null &&
                x !== undefined
            )
        )
          .size;

      }
    );


  readonly totalEntidades =
    computed(
      () => {

        return new Set(
          this.registrosFiltrados()
            .map(
              x =>
                x.entidad
            )
            .filter(
              x =>
                !!x
            )
        )
          .size;

      }
    );


  constructor(
    private readonly auditoriaService:
      AuditoriaService,

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
      this.fechaDesde() &&
      this.fechaHasta() &&
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


    const idEntidadNumero =
      this.idEntidad()
        ? Number(
            this.idEntidad()
          )
        : null;


    this.auditoriaService
      .consultar({

        fechaDesde:
          this.fechaDesde(),

        fechaHasta:
          this.fechaHasta(),

        accion:
          this.accion() ||
          undefined,

        entidad:
          this.entidad() ||
          undefined,

        idEntidad:
          idEntidadNumero

      })
      .subscribe({

        next:
          registros => {

            this.registros.set(
              registros
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
                'No fue posible consultar la auditoría.',

                'Error'

              );

          }

      });

  }


  // =====================================================
  // LIMPIAR FILTROS
  // =====================================================

  limpiarFiltros(): void {

    this.fechaDesde.set(
      this.obtenerFechaActual()
    );


    this.fechaHasta.set(
      this.obtenerFechaActual()
    );


    this.accion.set(
      ''
    );


    this.entidad.set(
      ''
    );


    this.usuario.set(
      ''
    );


    this.idEntidad.set(
      ''
    );


    this.busqueda.set(
      ''
    );


    this.consultar();

  }


  // =====================================================
  // DETALLE
  // =====================================================

  verDetalle(
    registro:
      AuditoriaRegistro
  ): void {

    this.registroSeleccionado.set(
      registro
    );


    this.modalDetalle.set(
      true
    );

  }


  cerrarDetalle(): void {

    this.modalDetalle.set(
      false
    );


    this.registroSeleccionado.set(
      null
    );

  }


  // =====================================================
  // TRAZABILIDAD
  // =====================================================

  verTrazabilidad(
    registro:
      AuditoriaRegistro
  ): void {

    if (
      !registro.entidad ||
      !registro.idEntidad
    ) {

      this.notificationService
        .warning(
          'El registro no tiene una entidad asociada.',
          'Trazabilidad'
        );


      return;

    }


    this.registroSeleccionado
      .set(
        registro
      );


    this.trazabilidad.set(
      []
    );


    this.modalTrazabilidad.set(
      true
    );


    this.cargandoTrazabilidad.set(
      true
    );


    this.auditoriaService
      .obtenerTrazabilidad(
        registro.entidad,
        registro.idEntidad
      )
      .subscribe({

        next:
          registros => {

            this.trazabilidad.set(
              registros
            );


            this.cargandoTrazabilidad
              .set(
                false
              );

          },


        error:
          error => {

            this.cargandoTrazabilidad
              .set(
                false
              );


            this.notificationService
              .error(

                error?.error?.mensaje ??
                'No fue posible consultar la trazabilidad.',

                'Error'

              );

          }

      });

  }


  cerrarTrazabilidad():
    void {

    this.modalTrazabilidad.set(
      false
    );


    this.trazabilidad.set(
      []
    );


    this.registroSeleccionado.set(
      null
    );

  }


  // =====================================================
  // JSON
  // =====================================================

  formatearJson(
    valor:
      string |
      null |
      undefined
  ): string {

    if (!valor) {

      return '-';

    }


    try {

      const objeto =
        JSON.parse(
          valor
        );


      return JSON.stringify(
        objeto,
        null,
        2
      );

    }
    catch {

      return valor;

    }

  }


  // =====================================================
  // FECHA / HORA
  // =====================================================

  fechaHora(
    fecha:
      string
  ): string {

    if (!fecha) {

      return '-';

    }


    const valor =
      new Date(
        fecha
      );


    if (
      Number.isNaN(
        valor.getTime()
      )
    ) {

      return fecha;

    }


    return valor.toLocaleString(
      'es-EC',
      {
        year:
          'numeric',

        month:
          '2-digit',

        day:
          '2-digit',

        hour:
          '2-digit',

        minute:
          '2-digit',

        second:
          '2-digit',

        hour12:
          false
      }
    );

  }


  nombreAccion(
    accion:
      string
  ): string {

    switch (
      accion
    ) {

      case 'MARCACION_FACIAL':

        return 'Marcación facial';


      case 'CREAR_MARCACION_MANUAL':

        return 'Marcación manual';


      case 'ANULAR_MARCACION':

        return 'Anulación de marcación';


      case 'CREAR_USUARIO':

        return 'Creación de usuario';


      case 'ACTUALIZAR_USUARIO':

        return 'Actualización de usuario';


      case 'CAMBIAR_ESTADO_USUARIO':

        return 'Cambio estado usuario';


      case 'RESET_PASSWORD':

        return 'Restablecimiento de contraseña';


      default:

        return accion
          .replaceAll(
            '_',
            ' '
          )
          .toLowerCase()
          .replace(
            /^\w/,
            letra =>
              letra.toUpperCase()
          );

    }

  }


  // =====================================================
  // FECHA ACTUAL
  // =====================================================

  private obtenerFechaActual():
    string {

    const fecha =
      new Date();


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