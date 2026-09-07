import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
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
} from 'rxjs/operators';

import {
  ActualizarEmpleadoRequest,
  CrearEmpleadoRequest,
  Empleado
} from './empleado.models';

import {
  EmpleadosService
} from './empleados.service';

import {
  NotificationService
} from '../../core/services/notification.service';

import {
  JornadasService
} from '../jornadas/jornadas.service';

import {
  Jornada
} from '../jornadas/jornada.models';

import {
  Modal
} from '../../shared/modal/modal';

import {
  ConfirmDialog
} from '../../shared/confirm-dialog/confirm-dialog';


interface EmpleadoVista
  extends Empleado {

  rostroEnrolado:
    boolean;

  consultandoRostro:
    boolean;

}


interface EmpleadoForm {

  identificacion:
    string;

  nombres:
    string;

  apellidos:
    string;

  correo:
    string;

  telefono:
    string;

  area:
    string;

  cargo:
    string;

  fechaIngreso:
    string;

  fechaSalida:
    string;

  idJornada:
    number | null;

  activo:
    boolean;

}


@Component({
  selector:
    'app-empleados',

  standalone:
    true,

  imports: [
    CommonModule,
    FormsModule,
    Modal,
    ConfirmDialog
  ],

  templateUrl:
    './empleados.html',

  styleUrl:
    './empleados.css'
})
export class Empleados
  implements OnInit, OnDestroy {

  // =====================================================
  // ELEMENTOS HTML
  // =====================================================

  @ViewChild('videoCamara')
  videoCamara?:
    ElementRef<HTMLVideoElement>;


  @ViewChild('canvasCaptura')
  canvasCaptura?:
    ElementRef<HTMLCanvasElement>;


  @ViewChild('inputFotosEmpleado')
  inputFotosEmpleado?:
    ElementRef<HTMLInputElement>;


  // =====================================================
  // EMPLEADOS
  // =====================================================

  empleados:
    EmpleadoVista[] = [];


  empleadosFiltrados:
    EmpleadoVista[] = [];


  jornadas:
    Jornada[] = [];


  filtro =
    '';


  cargando =
    false;


  // =====================================================
  // FORMULARIO
  // =====================================================

  guardando =
    false;


  modalEmpleadoAbierto =
    false;


  modoEdicion =
    false;


  empleadoEditando:
    EmpleadoVista | null =
    null;


  formulario:
    EmpleadoForm =
    this.crearFormularioVacio();


  // =====================================================
  // ACTIVAR / DESACTIVAR
  // =====================================================

  confirmacionAbierta =
    false;


  empleadoCambioEstado:
    EmpleadoVista | null =
    null;


  // =====================================================
  // ENROLAMIENTO
  // =====================================================

  readonly modalEnrolamientoAbierto =
    signal(false);


  readonly modoEnrolamiento =
    signal<
      'camara' |
      'archivos'
    >(
      'camara'
    );


  readonly iniciandoCamara =
    signal(false);


  readonly procesandoEnrolamiento =
    signal(false);


  readonly capturas =
    signal<string[]>([]);


  readonly nombresArchivos =
    signal<string[]>([]);


  empleadoEnrolamiento:
    EmpleadoVista | null =
    null;


  private streamCamara:
    MediaStream | null =
    null;


  private blobsCaptura:
    Blob[] = [];


  // =====================================================
  // CONSTRUCTOR
  // =====================================================

  constructor(

    private readonly empleadosService:
      EmpleadosService,

    private readonly jornadasService:
      JornadasService,

    private readonly notificationService:
      NotificationService,

    private readonly cdr:
      ChangeDetectorRef

  ) {
  }


  // =====================================================
  // CICLO DE VIDA
  // =====================================================

  ngOnInit(): void {

    this.cargarEmpleados();

    this.cargarJornadas();

  }


  ngOnDestroy(): void {

    this.detenerCamara();

    this.liberarPreviews();

  }


  // =====================================================
  // CARGAR EMPLEADOS
  // =====================================================

  cargarEmpleados(): void {

    this.cargando =
      true;


    this.cdr.markForCheck();


    this.empleadosService
      .obtenerTodos()
      .pipe(

        finalize(
          () => {

            this.cargando =
              false;

            this.cdr.markForCheck();

          }
        )

      )
      .subscribe({

        next:
          empleados => {

            this.empleados =
              empleados.map(
                empleado => ({

                  ...empleado,

                  rostroEnrolado:
                    false,

                  consultandoRostro:
                    true

                })
              );


            this.aplicarFiltro();


            this.cdr.markForCheck();


            this.cargarEstadosFaciales();

          },


        error:
          error => {

            this.empleados =
              [];


            this.empleadosFiltrados =
              [];


            this.notificationService.error(

              error?.error?.mensaje ??
              'No fue posible cargar los empleados.',

              'Error al consultar empleados'

            );


            this.cdr.markForCheck();

          }

      });

  }


  // =====================================================
  // CONSULTAR ROSTROS
  // =====================================================

  private cargarEstadosFaciales():
    void {

    for (
      const empleado
      of this.empleados
    ) {

      this.empleadosService
        .obtenerRostroActivo(
          empleado.idEmpleado
        )
        .pipe(

          finalize(
            () => {

              empleado.consultandoRostro =
                false;

              this.cdr.markForCheck();

            }
          )

        )
        .subscribe({

          next:
            () => {

              empleado.rostroEnrolado =
                true;


              this.cdr.markForCheck();

            },


          error:
            error => {

              empleado.rostroEnrolado =
                false;


              if (
                error?.status !== 404
              ) {

                console.error(

                  'Error consultando rostro:',

                  empleado.idEmpleado,

                  error

                );

              }


              this.cdr.markForCheck();

            }

        });

    }

  }


  // =====================================================
  // JORNADAS
  // =====================================================

  private cargarJornadas():
    void {

    this.jornadasService
      .obtenerTodas()
      .subscribe({

        next:
          jornadas => {

            this.jornadas =
              jornadas.filter(
                jornada =>
                  jornada.activo
              );


            this.cdr.markForCheck();

          },


        error:
          error => {

            console.error(
              'Error consultando jornadas:',
              error
            );


            this.notificationService.error(

              'No fue posible cargar las jornadas.',

              'Error'

            );


            this.cdr.markForCheck();

          }

      });

  }


  // =====================================================
  // FILTRO
  // =====================================================

  aplicarFiltro():
    void {

    const texto =
      this.filtro
        .trim()
        .toLowerCase();


    if (
      !texto
    ) {

      this.empleadosFiltrados =
        [
          ...this.empleados
        ];

      return;

    }


    this.empleadosFiltrados =
      this.empleados.filter(
        empleado => {

          const nombre =
            empleado.nombreCompleto
              ?.toLowerCase() ??
            '';


          const identificacion =
            empleado.identificacion
              ?.toLowerCase() ??
            '';


          const area =
            empleado.area
              ?.toLowerCase() ??
            '';


          const cargo =
            empleado.cargo
              ?.toLowerCase() ??
            '';


          return (

            nombre.includes(
              texto
            ) ||

            identificacion.includes(
              texto
            ) ||

            area.includes(
              texto
            ) ||

            cargo.includes(
              texto
            )

          );

        }
      );

  }


  // =====================================================
  // NUEVO EMPLEADO
  // =====================================================

  nuevoEmpleado():
    void {

    this.modoEdicion =
      false;


    this.empleadoEditando =
      null;


    this.formulario =
      this.crearFormularioVacio();


    this.modalEmpleadoAbierto =
      true;

  }


  // =====================================================
  // EDITAR EMPLEADO
  // =====================================================

  editarEmpleado(
    empleado:
      EmpleadoVista
  ): void {

    this.modoEdicion =
      true;


    this.empleadoEditando =
      empleado;


    this.formulario = {

      identificacion:
        empleado.identificacion,

      nombres:
        empleado.nombres,

      apellidos:
        empleado.apellidos,

      correo:
        empleado.correo ?? '',

      telefono:
        empleado.telefono ?? '',

      area:
        empleado.area ?? '',

      cargo:
        empleado.cargo ?? '',

      fechaIngreso:
        this.normalizarFecha(
          empleado.fechaIngreso
        ),

      fechaSalida:
        this.normalizarFecha(
          empleado.fechaSalida
        ),

      idJornada:
        empleado.idJornada,

      activo:
        empleado.activo

    };


    this.modalEmpleadoAbierto =
      true;

  }


  // =====================================================
  // CERRAR MODAL
  // =====================================================

  cerrarModalEmpleado():
    void {

    if (
      this.guardando
    ) {

      return;

    }


    this.modalEmpleadoAbierto =
      false;


    this.empleadoEditando =
      null;

  }


  // =====================================================
  // GUARDAR EMPLEADO
  // =====================================================

  guardarEmpleado():
    void {

    if (

      !this.formulario.identificacion.trim() ||

      !this.formulario.nombres.trim() ||

      !this.formulario.apellidos.trim() ||

      !this.formulario.fechaIngreso ||

      !this.formulario.idJornada

    ) {

      this.notificationService.warning(

        'Complete todos los campos obligatorios antes de continuar.',

        'Datos incompletos'

      );

      return;

    }


    if (

      this.formulario.fechaSalida &&

      this.formulario.fechaSalida <
      this.formulario.fechaIngreso

    ) {

      this.notificationService.warning(

        'La fecha de salida no puede ser anterior a la fecha de ingreso.',

        'Fecha incorrecta'

      );

      return;

    }


    this.guardando =
      true;


    this.cdr.markForCheck();


    if (

      this.modoEdicion &&

      this.empleadoEditando

    ) {

      this.actualizarEmpleado();

    }
    else {

      this.crearEmpleado();

    }

  }


  // =====================================================
  // CREAR EMPLEADO
  // =====================================================

  private crearEmpleado():
    void {

    const request:
      CrearEmpleadoRequest = {

      identificacion:
        this.formulario.identificacion.trim(),

      nombres:
        this.formulario.nombres.trim(),

      apellidos:
        this.formulario.apellidos.trim(),

      correo:
        this.valorOpcional(
          this.formulario.correo
        ),

      telefono:
        this.valorOpcional(
          this.formulario.telefono
        ),

      area:
        this.valorOpcional(
          this.formulario.area
        ),

      cargo:
        this.valorOpcional(
          this.formulario.cargo
        ),

      fechaIngreso:
        this.formulario.fechaIngreso,

      idJornada:
        this.formulario.idJornada!

    };


    this.empleadosService
      .crear(
        request
      )
      .pipe(

        finalize(
          () => {

            this.guardando =
              false;

            this.cdr.markForCheck();

          }
        )

      )
      .subscribe({

        next:
          () => {

            this.modalEmpleadoAbierto =
              false;


            this.notificationService.success(

              'Empleado creado correctamente.',

              'Operación exitosa'

            );


            this.cdr.markForCheck();


            this.cargarEmpleados();

          },


        error:
          error => {

            this.notificationService.error(

              error?.error?.mensaje ??
              'No fue posible crear el empleado.',

              'Error al crear empleado'

            );


            this.cdr.markForCheck();

          }

      });

  }


  // =====================================================
  // ACTUALIZAR EMPLEADO
  // =====================================================

  private actualizarEmpleado():
    void {

    if (
      !this.empleadoEditando
    ) {

      this.guardando =
        false;

      this.cdr.markForCheck();

      return;

    }


    const request:
      ActualizarEmpleadoRequest = {

      identificacion:
        this.formulario.identificacion.trim(),

      nombres:
        this.formulario.nombres.trim(),

      apellidos:
        this.formulario.apellidos.trim(),

      correo:
        this.valorOpcional(
          this.formulario.correo
        ),

      telefono:
        this.valorOpcional(
          this.formulario.telefono
        ),

      area:
        this.valorOpcional(
          this.formulario.area
        ),

      cargo:
        this.valorOpcional(
          this.formulario.cargo
        ),

      fechaIngreso:
        this.formulario.fechaIngreso,

      fechaSalida:
        this.valorOpcional(
          this.formulario.fechaSalida
        ),

      idJornada:
        this.formulario.idJornada!,

      activo:
        this.formulario.activo

    };


    this.empleadosService
      .actualizar(

        this.empleadoEditando.idEmpleado,

        request

      )
      .pipe(

        finalize(
          () => {

            this.guardando =
              false;

            this.cdr.markForCheck();

          }
        )

      )
      .subscribe({

        next:
          () => {

            this.modalEmpleadoAbierto =
              false;


            this.notificationService.success(

              'Empleado actualizado correctamente.',

              'Operación exitosa'

            );


            this.cdr.markForCheck();


            this.cargarEmpleados();

          },


        error:
          error => {

            this.notificationService.error(

              error?.error?.mensaje ??
              'No fue posible actualizar el empleado.',

              'Error al actualizar'

            );


            this.cdr.markForCheck();

          }

      });

  }


  // =====================================================
  // CAMBIO DE ESTADO
  // =====================================================

  cambiarEstado(
    empleado:
      EmpleadoVista
  ): void {

    this.empleadoCambioEstado =
      empleado;


    this.confirmacionAbierta =
      true;

  }


  cancelarCambioEstado():
    void {

    this.confirmacionAbierta =
      false;


    this.empleadoCambioEstado =
      null;

  }


  confirmarCambioEstado():
    void {

    const empleado =
      this.empleadoCambioEstado;


    if (
      !empleado
    ) {

      return;

    }


    const nuevoEstado =
      !empleado.activo;


    this.confirmacionAbierta =
      false;


    this.cdr.markForCheck();


    this.empleadosService
      .cambiarEstado(

        empleado.idEmpleado,

        nuevoEstado

      )
      .subscribe({

        next:
          () => {

            empleado.activo =
              nuevoEstado;


            this.notificationService.success(

              nuevoEstado
                ? 'Empleado activado correctamente.'
                : 'Empleado desactivado correctamente.',

              'Operación exitosa'

            );


            this.empleadoCambioEstado =
              null;


            this.cdr.markForCheck();

          },


        error:
          error => {

            this.notificationService.error(

              error?.error?.mensaje ??
              'No fue posible modificar el estado del empleado.',

              'Error'

            );


            this.empleadoCambioEstado =
              null;


            this.cdr.markForCheck();

          }

      });

  }


  // =====================================================
  // ABRIR ENROLAMIENTO
  // =====================================================

  enrolarRostro(
    empleado:
      EmpleadoVista
  ): void {

    if (
      !empleado.activo
    ) {

      this.notificationService.warning(

        'El empleado debe estar activo para realizar el enrolamiento facial.',

        'Empleado inactivo'

      );

      return;

    }


    this.detenerCamara();

    this.liberarPreviews();


    this.empleadoEnrolamiento =
      empleado;


    this.blobsCaptura =
      [];


    this.capturas.set(
      []
    );


    this.nombresArchivos.set(
      []
    );


    this.modoEnrolamiento.set(
      'camara'
    );


    this.modalEnrolamientoAbierto.set(
      true
    );


    setTimeout(
      () => {

        void this.iniciarCamara();

      },
      100
    );

  }


  // =====================================================
  // SELECCIONAR MODO CÁMARA
  // =====================================================

  seleccionarModoCamara():
    void {

    if (
      this.procesandoEnrolamiento()
    ) {

      return;

    }


    if (
      this.modoEnrolamiento() ===
      'camara'
    ) {

      return;

    }


    this.detenerCamara();

    this.liberarPreviews();


    this.blobsCaptura =
      [];


    this.capturas.set(
      []
    );


    this.nombresArchivos.set(
      []
    );


    this.modoEnrolamiento.set(
      'camara'
    );


    setTimeout(
      () => {

        void this.iniciarCamara();

      },
      100
    );

  }


  // =====================================================
  // SELECCIONAR MODO ARCHIVOS
  // =====================================================

  seleccionarModoArchivos():
    void {

    if (
      this.procesandoEnrolamiento()
    ) {

      return;

    }


    if (
      this.modoEnrolamiento() ===
      'archivos'
    ) {

      return;

    }


    this.detenerCamara();

    this.liberarPreviews();


    this.blobsCaptura =
      [];


    this.capturas.set(
      []
    );


    this.nombresArchivos.set(
      []
    );


    this.modoEnrolamiento.set(
      'archivos'
    );

  }


  // =====================================================
  // ABRIR SELECTOR DE ARCHIVOS
  // =====================================================

  abrirSelectorFotografias():
    void {

    if (
      this.procesandoEnrolamiento()
    ) {

      return;

    }


    if (
      this.capturas().length >= 3
    ) {

      return;

    }


    this.inputFotosEmpleado
      ?.nativeElement
      .click();

  }


  // =====================================================
  // SELECCIONAR ARCHIVOS
  // =====================================================

  seleccionarArchivos(
    event: Event
  ): void {

    if (
      this.procesandoEnrolamiento()
    ) {
      return;
    }

    const input =
      event.target as HTMLInputElement;

    if (
      !input.files ||
      input.files.length === 0
    ) {
      return;
    }

    const nuevosArchivos: File[] =
      Array.from(
        input.files
      );

    const tiposPermitidos = [
      'image/jpeg',
      'image/png'
    ];

    const maximoBytes =
      5 * 1024 * 1024;


    // ===================================================
    // VALIDAR ARCHIVOS
    // ===================================================

    for (
      const archivo
      of nuevosArchivos
    ) {

      if (
        !tiposPermitidos.includes(
          archivo.type
        )
      ) {

        this.notificationService.warning(
          `El archivo "${archivo.name}" no es una imagen JPG, JPEG o PNG.`,
          'Formato no permitido'
        );

        input.value = '';

        return;
      }


      if (
        archivo.size >
        maximoBytes
      ) {

        this.notificationService.warning(
          `La fotografía "${archivo.name}" supera el máximo permitido de 5 MB.`,
          'Archivo demasiado grande'
        );

        input.value = '';

        return;
      }

    }


    // ===================================================
    // VALIDAR MÁXIMO DE 3
    // ===================================================

    const cantidadActual =
      this.blobsCaptura.length;

    const cantidadNueva =
      nuevosArchivos.length;


    if (
      cantidadActual +
      cantidadNueva >
      3
    ) {

      this.notificationService.warning(
        `Solo se requieren 3 fotografías. Actualmente tiene ${cantidadActual} seleccionada(s).`,
        'Máximo de fotografías'
      );

      input.value = '';

      return;
    }


    // ===================================================
    // AGREGAR FOTOGRAFÍAS
    // ===================================================

    for (
      const archivo
      of nuevosArchivos
    ) {

      this.blobsCaptura.push(
        archivo
      );

      const preview =
        URL.createObjectURL(
          archivo
        );

      this.capturas.update(
        actuales => [
          ...actuales,
          preview
        ]
      );

      this.nombresArchivos.update(
        actuales => [
          ...actuales,
          archivo.name
        ]
      );

    }


    input.value = '';


    // ===================================================
    // MENSAJE
    // ===================================================

    const cantidad =
      this.blobsCaptura.length;


    if (
      cantidad === 3
    ) {

      this.notificationService.success(
        'Las tres fotografías están listas para el enrolamiento.',
        'Fotografías completas'
      );

    }
    else {

      this.notificationService.info(
        `Fotografía cargada. Faltan ${3 - cantidad}.`,
        `${cantidad} de 3 fotografías`
      );

    }

  }


  // =====================================================
  // INICIAR CÁMARA
  // =====================================================

  async iniciarCamara():
    Promise<void> {

    if (

      this.iniciandoCamara() ||

      this.streamCamara

    ) {

      return;

    }


    if (
      this.modoEnrolamiento() !==
      'camara'
    ) {

      return;

    }


    if (

      !navigator.mediaDevices ||

      !navigator.mediaDevices
        .getUserMedia

    ) {

      this.notificationService.error(

        'El navegador no permite acceder a la cámara.',

        'Cámara no disponible'

      );

      return;

    }


    try {

      this.iniciandoCamara.set(
        true
      );


      this.streamCamara =
        await navigator.mediaDevices
          .getUserMedia({

            video: {

              width: {
                ideal: 1280
              },

              height: {
                ideal: 720
              },

              facingMode:
                'user'

            },

            audio:
              false

          });


      if (
        this.modoEnrolamiento() !==
        'camara'
      ) {

        this.detenerCamara();

        return;

      }


      const video =
        this.videoCamara
          ?.nativeElement;


      if (
        !video
      ) {

        throw new Error(
          'No se encontró el elemento de video.'
        );

      }


      video.srcObject =
        this.streamCamara;


      await video.play();

    }
    catch (
      error
    ) {

      console.error(
        'Error iniciando cámara:',
        error
      );


      this.notificationService.error(

        'No fue posible acceder a la cámara. Verifique los permisos del navegador.',

        'Error de cámara'

      );


      this.detenerCamara();

    }
    finally {

      this.iniciandoCamara.set(
        false
      );

    }

  }


  // =====================================================
  // CAPTURAR FOTO
  // =====================================================

  capturarRostro():
    void {

    if (
      this.modoEnrolamiento() !==
      'camara'
    ) {

      return;

    }


    if (
      this.capturas().length >= 3
    ) {

      return;

    }


    const video =
      this.videoCamara
        ?.nativeElement;


    const canvas =
      this.canvasCaptura
        ?.nativeElement;


    if (
      !video ||
      !canvas
    ) {

      this.notificationService.error(

        'La cámara todavía no está preparada.',

        'Cámara'

      );

      return;

    }


    if (

      video.videoWidth === 0 ||

      video.videoHeight === 0

    ) {

      this.notificationService.warning(

        'Espere un momento hasta que la cámara esté completamente lista.',

        'Cámara iniciando'

      );

      return;

    }


    canvas.width =
      video.videoWidth;


    canvas.height =
      video.videoHeight;


    const context =
      canvas.getContext(
        '2d'
      );


    if (
      !context
    ) {

      this.notificationService.error(
        'No fue posible procesar la fotografía.'
      );

      return;

    }


    context.drawImage(

      video,

      0,
      0,

      canvas.width,
      canvas.height

    );


    canvas.toBlob(

      blob => {

        if (
          !blob
        ) {

          this.notificationService.error(

            'No fue posible generar la fotografía.',

            'Captura'

          );

          return;

        }


        this.blobsCaptura.push(
          blob
        );


        const numero =
          this.blobsCaptura.length;


        this.capturas.update(
          actuales => [

            ...actuales,

            URL.createObjectURL(
              blob
            )

          ]
        );


        this.nombresArchivos.update(
          actuales => [

            ...actuales,

            `Captura ${numero}`

          ]
        );


        if (
          numero === 3
        ) {

          this.notificationService.success(

            'Las tres fotografías están listas para procesarse.',

            'Capturas completadas'

          );

        }

      },

      'image/jpeg',

      0.92

    );

  }


  // =====================================================
  // LIMPIAR FOTOS
  // =====================================================

  reiniciarCapturas():
    void {

    if (
      this.procesandoEnrolamiento()
    ) {

      return;

    }


    this.liberarPreviews();


    this.blobsCaptura =
      [];


    this.capturas.set(
      []
    );


    this.nombresArchivos.set(
      []
    );

  }


  // =====================================================
  // GUARDAR ENROLAMIENTO
  // =====================================================

  guardarEnrolamiento():
    void {

    const empleado =
      this.empleadoEnrolamiento;


    if (
      !empleado
    ) {

      return;

    }


    if (
      this.blobsCaptura.length !== 3
    ) {

      this.notificationService.warning(

        'Debe disponer de exactamente tres fotografías antes de enrolar el rostro.',

        'Fotografías incompletas'

      );

      return;

    }


    this.procesandoEnrolamiento.set(
      true
    );


    this.empleadosService
      .enrolarRostro(

        empleado.idEmpleado,

        this.blobsCaptura

      )
      .subscribe({

        next:
          () => {

            empleado.rostroEnrolado =
              true;


            empleado.consultandoRostro =
              false;


            this.procesandoEnrolamiento.set(
              false
            );


            this.notificationService.success(

              `El rostro de ${empleado.nombreCompleto} fue enrolado correctamente.`,

              'Enrolamiento completado'

            );


            this.cerrarEnrolamiento();


            this.cdr.markForCheck();

          },


        error:
          error => {

            this.procesandoEnrolamiento.set(
              false
            );


            console.error(
              'Error completo de enrolamiento:',
              error
            );


            /*
             * El interceptor manejará el 401 y
             * enviará al usuario al login.
             */
            if (
              error?.status === 401
            ) {

              return;

            }


            this.notificationService.error(

              this.obtenerMensajeError(
                error
              ),

              'Error de enrolamiento'

            );

          }

      });

  }


  // =====================================================
  // CERRAR ENROLAMIENTO
  // =====================================================

  cerrarEnrolamiento():
    void {

    if (
      this.procesandoEnrolamiento()
    ) {

      return;

    }


    this.detenerCamara();

    this.liberarPreviews();


    this.blobsCaptura =
      [];


    this.capturas.set(
      []
    );


    this.nombresArchivos.set(
      []
    );


    this.modoEnrolamiento.set(
      'camara'
    );


    this.modalEnrolamientoAbierto.set(
      false
    );


    this.empleadoEnrolamiento =
      null;

  }


  // =====================================================
  // DETENER CÁMARA
  // =====================================================

  private detenerCamara():
    void {

    if (
      this.streamCamara
    ) {

      this.streamCamara
        .getTracks()
        .forEach(
          track => {

            track.stop();

          }
        );

    }


    this.streamCamara =
      null;


    const video =
      this.videoCamara
        ?.nativeElement;


    if (
      video
    ) {

      video.pause();

      video.srcObject =
        null;

    }

  }


  // =====================================================
  // LIBERAR PREVIEWS
  // =====================================================

  private liberarPreviews():
    void {

    for (
      const preview
      of this.capturas()
    ) {

      URL.revokeObjectURL(
        preview
      );

    }

  }


  // =====================================================
  // OBTENER MENSAJE ERROR
  // =====================================================

  private obtenerMensajeError(
    error:
      any
  ): string {

    if (
      typeof error?.error ===
      'string'
    ) {

      return error.error;

    }


    if (
      error?.error?.mensaje
    ) {

      return error.error.mensaje;

    }


    if (
      error?.error?.detail
    ) {

      return typeof error.error.detail ===
        'string'

        ? error.error.detail

        : JSON.stringify(
            error.error.detail
          );

    }


    if (
      error?.error?.errors
    ) {

      const mensajes:
        string[] = [];


      for (
        const valor
        of Object.values(
          error.error.errors
        )
      ) {

        if (
          Array.isArray(
            valor
          )
        ) {

          for (
            const mensaje
            of valor
          ) {

            mensajes.push(
              String(
                mensaje
              )
            );

          }

        }
        else {

          mensajes.push(
            String(
              valor
            )
          );

        }

      }


      if (
        mensajes.length > 0
      ) {

        return mensajes.join(
          ' '
        );

      }

    }


    if (
      error?.error?.title
    ) {

      return error.error.title;

    }


    if (
      error?.message
    ) {

      return error.message;

    }


    return 'No fue posible realizar el enrolamiento facial.';

  }


  // =====================================================
  // FORMULARIO VACÍO
  // =====================================================

  private crearFormularioVacio():
    EmpleadoForm {

    const hoy =
      new Date()
        .toISOString()
        .slice(
          0,
          10
        );


    return {

      identificacion:
        '',

      nombres:
        '',

      apellidos:
        '',

      correo:
        '',

      telefono:
        '',

      area:
        '',

      cargo:
        '',

      fechaIngreso:
        hoy,

      fechaSalida:
        '',

      idJornada:
        null,

      activo:
        true

    };

  }


  // =====================================================
  // VALOR OPCIONAL
  // =====================================================

  private valorOpcional(
    valor:
      string
  ): string | null {

    const limpio =
      valor.trim();


    return limpio
      ? limpio
      : null;

  }


  // =====================================================
  // NORMALIZAR FECHA
  // =====================================================

  private normalizarFecha(
    fecha:
      string |
      null |
      undefined
  ): string {

    if (
      !fecha
    ) {

      return '';

    }


    return fecha.substring(
      0,
      10
    );

  }

}