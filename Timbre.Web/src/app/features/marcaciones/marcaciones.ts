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
  MarcacionesService
} from './marcaciones.service';

import {
  MarcacionAdmin,
  TipoMarcacion
} from './marcacion.models';

import {
  EmpleadosService
} from '../empleados/empleados.service';

import {
  Empleado
} from '../empleados/empleado.models';

import {
  NotificationService
} from '../../core/services/notification.service';


interface FormCorreccion {

  idEmpleado:
    number | null;

  fecha:
    string;

  hora:
    string;

  tipoMarcacion:
    TipoMarcacion;

  motivo:
    string;

}


interface FormRegularizacion {

  idEmpleado:
    number | null;

  fecha:
    string;

  hora:
    string;

  tipoMarcacion:
    TipoMarcacion | null;

  motivo:
    string;

}


@Component({
  selector:
    'app-marcaciones',

  standalone:
    true,

  imports: [
    CommonModule,
    FormsModule
  ],

  templateUrl:
    './marcaciones.html',

  styleUrl:
    './marcaciones.css'
})
export class Marcaciones {

  // =====================================================
  // TIPOS
  // =====================================================

  readonly tiposMarcacion:
    TipoMarcacion[] =
    [
      'Entrada',
      'InicioAlmuerzo',
      'FinAlmuerzo',
      'Salida'
    ];


  // =====================================================
  // DATOS
  // =====================================================

  readonly marcaciones =
    signal<MarcacionAdmin[]>(
      []
    );


  readonly empleados =
    signal<Empleado[]>(
      []
    );


  readonly cargando =
    signal(
      false
    );


  readonly guardando =
    signal(
      false
    );


  // =====================================================
  // FILTROS
  // =====================================================

  readonly fecha =
    signal(
      this.obtenerFechaActual()
    );


  readonly busqueda =
    signal(
      ''
    );


  readonly tipo =
    signal(
      ''
    );


  readonly estado =
    signal(
      ''
    );


  readonly origen =
    signal(
      ''
    );


  // =====================================================
  // MODALES
  // =====================================================

  readonly modalCorreccion =
    signal(
      false
    );


  readonly modalRegularizar =
    signal(
      false
    );


  readonly modalAnular =
    signal(
      false
    );


  readonly marcacionSeleccionada =
    signal<MarcacionAdmin | null>(
      null
    );


  readonly motivoAnulacion =
    signal(
      ''
    );


  readonly formulario =
    signal<FormCorreccion>(
      this.formularioInicial()
    );


  readonly formularioRegularizacion =
    signal<FormRegularizacion>(
      this.formularioRegularizacionInicial()
    );


  // =====================================================
  // FILTRADO
  // =====================================================

  readonly marcacionesFiltradas =
    computed(
      () => {

        const texto =
          this.busqueda()
            .trim()
            .toLowerCase();


        return this.marcaciones()
          .filter(
            marcacion => {

              const coincideTexto =
                !texto ||

                marcacion.empleado
                  .toLowerCase()
                  .includes(
                    texto
                  ) ||

                marcacion.identificacion
                  .toLowerCase()
                  .includes(
                    texto
                  ) ||

                (
                  marcacion.area ??
                  ''
                )
                  .toLowerCase()
                  .includes(
                    texto
                  );


              const coincideTipo =
                !this.tipo() ||

                marcacion.tipoMarcacion ===
                this.tipo();


              const coincideEstado =
                !this.estado() ||

                marcacion.estadoMarcacion ===
                this.estado();


              const origenMarcacion =
                this.origenMarcacion(
                  marcacion
                );


              const coincideOrigen =
                !this.origen() ||

                origenMarcacion ===
                this.origen();


              return (
                coincideTexto &&
                coincideTipo &&
                coincideEstado &&
                coincideOrigen
              );

            }
          );

      }
    );


  // =====================================================
  // RESUMEN
  // =====================================================

  readonly totalActivas =
    computed(
      () =>
        this.marcaciones()
          .filter(
            x =>
              x.estadoMarcacion ===
              'Activa'
          )
          .length
    );


  readonly totalAnuladas =
    computed(
      () =>
        this.marcaciones()
          .filter(
            x =>
              x.estadoMarcacion ===
              'Anulada'
          )
          .length
    );


  readonly totalFaciales =
    computed(
      () =>
        this.marcaciones()
          .filter(
            x =>
              this.origenMarcacion(x) ===
                'Facial' &&
              x.estadoMarcacion ===
                'Activa'
          )
          .length
    );


  readonly totalManuales =
    computed(
      () =>
        this.marcaciones()
          .filter(
            x =>
              this.origenMarcacion(x) ===
                'Manual' &&
              x.estadoMarcacion ===
                'Activa'
          )
          .length
    );


  readonly totalRegularizaciones =
    computed(
      () =>
        this.marcaciones()
          .filter(
            x =>
              this.origenMarcacion(x) ===
                'Regularización' &&
              x.estadoMarcacion ===
                'Activa'
          )
          .length
    );


  // =====================================================
  // REGULARIZACIÓN - TIPOS FALTANTES
  // =====================================================

  readonly tiposFaltantesRegularizacion =
    computed<TipoMarcacion[]>(
      () => {

        const idEmpleado =
          this.formularioRegularizacion()
            .idEmpleado;


        if (
          !idEmpleado
        ) {

          return [];

        }


        return this.obtenerTiposFaltantes(
          idEmpleado
        );

      }
    );


  readonly empleadoRegularizacion =
    computed(
      () => {

        const idEmpleado =
          this.formularioRegularizacion()
            .idEmpleado;


        if (
          !idEmpleado
        ) {

          return null;

        }


        return this.empleados()
          .find(
            empleado =>
              empleado.idEmpleado ===
              idEmpleado
          ) ??
          null;

      }
    );


  constructor(
    private readonly marcacionesService:
      MarcacionesService,

    private readonly empleadosService:
      EmpleadosService,

    private readonly notificationService:
      NotificationService
  ) {

    this.cargarEmpleados();

    this.consultar();

  }


  // =====================================================
  // CONSULTAR
  // =====================================================

  consultar():
    void {

    if (
      !this.fecha()
    ) {

      return;

    }


    this.cargando.set(
      true
    );


    this.marcacionesService
      .consultar(
        this.fecha()
      )
      .subscribe({

        next:
          response => {

            this.marcaciones.set(
              response.marcaciones
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
                'No fue posible consultar las marcaciones.',

                'Error'

              );

          }

      });

  }


  // =====================================================
  // EMPLEADOS
  // =====================================================

  private cargarEmpleados():
    void {

    this.empleadosService
      .obtenerTodos()
      .subscribe({

        next:
          empleados => {

            this.empleados.set(
              empleados.filter(
                e =>
                  e.activo
              )
            );

          },


        error:
          error => {

            console.error(
              'Error consultando empleados:',
              error
            );

          }

      });

  }


  // =====================================================
  // CORRECCIÓN MANUAL
  // =====================================================

  abrirCorreccion():
    void {

    this.formulario.set(
      {
        ...this.formularioInicial(),

        fecha:
          this.fecha()
      }
    );


    this.modalCorreccion.set(
      true
    );

  }


  cerrarCorreccion():
    void {

    if (
      this.guardando()
    ) {

      return;

    }


    this.modalCorreccion.set(
      false
    );

  }


  guardarCorreccion():
    void {

    const form =
      this.formulario();


    if (
      !form.idEmpleado ||
      !form.fecha ||
      !form.hora ||
      !form.tipoMarcacion ||
      !form.motivo.trim()
    ) {

      this.notificationService
        .warning(
          'Complete todos los campos obligatorios.',
          'Datos incompletos'
        );


      return;

    }


    this.guardando.set(
      true
    );


    this.marcacionesService
      .crearCorreccion(
        {
          idEmpleado:
            form.idEmpleado,

          fecha:
            form.fecha,

          hora:
            this.normalizarHora(
              form.hora
            ),

          tipoMarcacion:
            form.tipoMarcacion,

          motivo:
            form.motivo.trim()
        }
      )
      .subscribe({

        next:
          response => {

            this.guardando.set(
              false
            );


            this.modalCorreccion.set(
              false
            );


            this.notificationService
              .success(
                response.mensaje,
                'Marcación registrada'
              );


            this.consultar();

          },


        error:
          error => {

            this.guardando.set(
              false
            );


            this.notificationService
              .error(

                error?.error?.mensaje ??
                'No fue posible registrar la corrección.',

                'Error'

              );

          }

      });

  }


  // =====================================================
  // ABRIR REGULARIZACIÓN
  //
  // Si viene desde una fila:
  // - preselecciona empleado.
  //
  // Si viene desde el botón superior:
  // - RRHH selecciona empleado.
  // =====================================================

  abrirRegularizacion(
    marcacion?:
      MarcacionAdmin
  ):
    void {

    const idEmpleado =
      marcacion?.idEmpleado ??
      null;


    const tiposFaltantes =
      idEmpleado
        ? this.obtenerTiposFaltantes(
            idEmpleado
          )
        : [];


    if (
      idEmpleado &&
      tiposFaltantes.length ===
        0
    ) {

      this.notificationService
        .warning(
          'El empleado ya tiene todas las marcaciones activas para la fecha seleccionada.',
          'Jornada completa'
        );


      return;

    }


    this.formularioRegularizacion.set(
      {
        idEmpleado,

        fecha:
          this.fecha(),

        hora:
          '',

        tipoMarcacion:
          tiposFaltantes[0] ??
          null,

        motivo:
          ''
      }
    );


    this.modalRegularizar.set(
      true
    );

  }


  cerrarRegularizacion():
    void {

    if (
      this.guardando()
    ) {

      return;

    }


    this.modalRegularizar.set(
      false
    );


    this.formularioRegularizacion.set(
      this.formularioRegularizacionInicial()
    );

  }


  // =====================================================
  // CAMBIO EMPLEADO REGULARIZACIÓN
  // =====================================================

  cambiarEmpleadoRegularizacion(
    valor:
      number | null
  ):
    void {

    const idEmpleado =
      valor
        ? +valor
        : null;


    const faltantes =
      idEmpleado
        ? this.obtenerTiposFaltantes(
            idEmpleado
          )
        : [];


    this.formularioRegularizacion.update(
      actual => ({
        ...actual,

        idEmpleado,

        tipoMarcacion:
          faltantes[0] ??
          null
      })
    );

  }


  actualizarCampoRegularizacion<
    K extends keyof FormRegularizacion
  >(
    campo:
      K,

    valor:
      FormRegularizacion[K]
  ):
    void {

    this.formularioRegularizacion
      .update(
        actual => ({
          ...actual,

          [campo]:
            valor
        })
      );

  }


  // =====================================================
  // GUARDAR REGULARIZACIÓN
  // =====================================================

  guardarRegularizacion():
    void {

    const form =
      this.formularioRegularizacion();


    if (
      !form.idEmpleado ||
      !form.fecha ||
      !form.hora ||
      !form.tipoMarcacion ||
      !form.motivo.trim()
    ) {

      this.notificationService
        .warning(
          'Complete todos los campos obligatorios.',
          'Datos incompletos'
        );


      return;

    }


    if (
      form.motivo
        .trim()
        .length <
      5
    ) {

      this.notificationService
        .warning(
          'Ingrese un motivo más descriptivo para la regularización.',
          'Motivo insuficiente'
        );


      return;

    }


    if (
      !this.tiposFaltantesRegularizacion()
        .includes(
          form.tipoMarcacion
        )
    ) {

      this.notificationService
        .warning(
          'La marcación seleccionada ya no se encuentra pendiente.',
          'Marcación no disponible'
        );


      return;

    }


    this.guardando.set(
      true
    );


    this.marcacionesService
      .regularizar(
        {
          idEmpleado:
            form.idEmpleado,

          fecha:
            form.fecha,

          hora:
            this.normalizarHora(
              form.hora
            ),

          tipoMarcacion:
            form.tipoMarcacion,

          motivo:
            form.motivo
              .trim()
        }
      )
      .subscribe({

        next:
          response => {

            this.guardando.set(
              false
            );


            this.modalRegularizar.set(
              false
            );


            this.formularioRegularizacion
              .set(
                this.formularioRegularizacionInicial()
              );


            this.notificationService
              .success(
                response.mensaje,
                'Marcación regularizada'
              );


            this.consultar();

          },


        error:
          error => {

            this.guardando.set(
              false
            );


            this.notificationService
              .error(

                error?.error?.mensaje ??
                'No fue posible regularizar la marcación.',

                'Regularización'

              );

          }

      });

  }


  // =====================================================
  // ¿TIENE MARCACIONES FALTANTES?
  // =====================================================

  tieneMarcacionesFaltantes(
    idEmpleado:
      number
  ):
    boolean {

    return (
      this.obtenerTiposFaltantes(
        idEmpleado
      ).length >
      0
    );

  }


  private obtenerTiposFaltantes(
    idEmpleado:
      number
  ):
    TipoMarcacion[] {

    const tiposActivos =
      new Set(
        this.marcaciones()
          .filter(
            marcacion =>
              marcacion.idEmpleado ===
                idEmpleado &&

              marcacion.estadoMarcacion ===
                'Activa'
          )
          .map(
            marcacion =>
              marcacion.tipoMarcacion
          )
      );


    return this.tiposMarcacion
      .filter(
        tipo =>
          !tiposActivos.has(
            tipo
          )
      );

  }


  // =====================================================
  // ANULACIÓN
  // =====================================================

  solicitarAnulacion(
    marcacion:
      MarcacionAdmin
  ):
    void {

    if (
      marcacion.estadoMarcacion ===
      'Anulada'
    ) {

      return;

    }


    this.marcacionSeleccionada
      .set(
        marcacion
      );


    this.motivoAnulacion.set(
      ''
    );


    this.modalAnular.set(
      true
    );

  }


  cancelarAnulacion():
    void {

    if (
      this.guardando()
    ) {

      return;

    }


    this.modalAnular.set(
      false
    );


    this.marcacionSeleccionada
      .set(
        null
      );


    this.motivoAnulacion.set(
      ''
    );

  }


  confirmarAnulacion():
    void {

    const marcacion =
      this.marcacionSeleccionada();


    const motivo =
      this.motivoAnulacion()
        .trim();


    if (
      !marcacion
    ) {

      return;

    }


    if (
      !motivo
    ) {

      this.notificationService
        .warning(
          'Debe ingresar el motivo de la anulación.',
          'Motivo obligatorio'
        );


      return;

    }


    this.guardando.set(
      true
    );


    this.marcacionesService
      .anular(
        marcacion.idMarcacion,
        {
          motivo
        }
      )
      .subscribe({

        next:
          response => {

            this.guardando.set(
              false
            );


            this.modalAnular.set(
              false
            );


            this.marcacionSeleccionada
              .set(
                null
              );


            this.motivoAnulacion.set(
              ''
            );


            this.notificationService
              .success(
                response.mensaje,
                'Marcación anulada'
              );


            this.consultar();

          },


        error:
          error => {

            this.guardando.set(
              false
            );


            this.notificationService
              .error(

                error?.error?.mensaje ??
                'No fue posible anular la marcación.',

                'Error'

              );

          }

      });

  }


  // =====================================================
  // FORMULARIO CORRECCIÓN
  // =====================================================

  actualizarCampo<
    K extends keyof FormCorreccion
  >(
    campo:
      K,

    valor:
      FormCorreccion[K]
  ):
    void {

    this.formulario.update(
      actual => ({
        ...actual,

        [campo]:
          valor
      })
    );

  }


  private formularioInicial():
    FormCorreccion {

    return {

      idEmpleado:
        null,

      fecha:
        this.obtenerFechaActual(),

      hora:
        '',

      tipoMarcacion:
        'Entrada',

      motivo:
        ''

    };

  }


  private formularioRegularizacionInicial():
    FormRegularizacion {

    return {

      idEmpleado:
        null,

      fecha:
        this.fecha(),

      hora:
        '',

      tipoMarcacion:
        null,

      motivo:
        ''

    };

  }


  // =====================================================
  // ORIGEN
  // =====================================================

  origenMarcacion(
    marcacion:
      MarcacionAdmin
  ):
    string {

    if (
      marcacion.origen
    ) {

      return marcacion.origen;

    }


    if (
      marcacion.esFacial
    ) {

      return 'Facial';

    }


    if (
      marcacion.observacion
        ?.startsWith(
          'Regularización administrativa:'
        )
    ) {

      return 'Regularización';

    }


    return 'Manual';

  }


  claseOrigen(
    marcacion:
      MarcacionAdmin
  ):
    string {

    const origen =
      this.origenMarcacion(
        marcacion
      );


    switch (
      origen
    ) {

      case 'Facial':

        return 'facial';


      case 'Regularización':

        return 'regularizacion';


      default:

        return 'manual';

    }

  }


  // =====================================================
  // HELPERS
  // =====================================================

  hora(
    fechaHora:
      string
  ):
    string {

    if (
      !fechaHora
    ) {

      return '-';

    }


    const indice =
      fechaHora.indexOf(
        'T'
      );


    if (
      indice >=
      0
    ) {

      return fechaHora.substring(
        indice + 1,
        indice + 6
      );

    }


    if (
      fechaHora.length >=
      16
    ) {

      return fechaHora.substring(
        11,
        16
      );

    }


    return fechaHora;

  }


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


  private normalizarHora(
    hora:
      string
  ):
    string {

    return hora.length ===
      5
        ? `${hora}:00`
        : hora;

  }


  private obtenerFechaActual():
    string {

    const fecha =
      new Date();


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