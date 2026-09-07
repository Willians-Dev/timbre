import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  signal,
  ViewChild
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  RouterLink
} from '@angular/router';

import {
  KioscoService,
  MarcacionFacialResultado
} from './kiosco.service';


type EstadoKiosco =
  | 'inicializando'
  | 'listo'
  | 'procesando'
  | 'exito'
  | 'error';


@Component({
  selector:
    'app-kiosco',

  standalone:
    true,

  imports: [
    CommonModule,
    RouterLink
  ],

  templateUrl:
    './kiosco.html',

  styleUrl:
    './kiosco.css'
})
export class Kiosco
  implements AfterViewInit, OnDestroy {

  // =====================================================
  // ELEMENTOS HTML
  // =====================================================

  @ViewChild(
    'videoCamara'
  )
  videoCamara?:
    ElementRef<HTMLVideoElement>;


  @ViewChild(
    'canvasCaptura'
  )
  canvasCaptura?:
    ElementRef<HTMLCanvasElement>;


  // =====================================================
  // ESTADO
  // =====================================================

  readonly estado =
    signal<EstadoKiosco>(
      'inicializando'
    );


  readonly resultado =
    signal<
      MarcacionFacialResultado |
      null
    >(
      null
    );


  readonly mensajeError =
    signal(
      ''
    );


  readonly horaActual =
    signal(
      ''
    );


  // =====================================================
  // CONFIGURACIÓN
  // =====================================================

  /*
   * Tiempo que permanece visible
   * el resultado de la marcación.
   *
   * 5000 ms = 5 segundos.
   */
  private readonly tiempoResultadoMs =
    5000;


  // =====================================================
  // CÁMARA
  // =====================================================

  private streamCamara:
    MediaStream |
    null =
    null;


  // =====================================================
  // TEMPORIZADORES
  // =====================================================

  private intervaloHora:
    ReturnType<typeof setInterval> |
    null =
    null;


  private temporizadorResultado:
    ReturnType<typeof setTimeout> |
    null =
    null;


  // =====================================================
  // CONTROL DE CICLO DE VIDA
  // =====================================================

  private destruido =
    false;


  // =====================================================
  // CONSTRUCTOR
  // =====================================================

  constructor(
    private readonly kioscoService:
      KioscoService
  ) {
  }


  // =====================================================
  // CICLO DE VIDA
  // =====================================================

  ngAfterViewInit():
    void {

    this.actualizarHora();


    this.intervaloHora =
      setInterval(
        () => {

          this.actualizarHora();

        },
        1000
      );


    void this.iniciarCamara();

  }


  ngOnDestroy():
    void {

    this.destruido =
      true;


    this.limpiarTemporizadorResultado();


    if (
      this.intervaloHora
    ) {

      clearInterval(
        this.intervaloHora
      );


      this.intervaloHora =
        null;

    }


    this.detenerCamara();

  }


  // =====================================================
  // INICIAR CÁMARA
  // =====================================================

  private async iniciarCamara():
    Promise<void> {

    /*
     * Si la cámara ya está activa,
     * no pedimos permisos otra vez.
     */
    if (
      this.streamCamara
    ) {

      await this.adjuntarStreamVideo();

      return;

    }


    if (
      !navigator.mediaDevices ||
      !navigator.mediaDevices
        .getUserMedia
    ) {

      this.estado.set(
        'error'
      );


      this.mensajeError.set(
        'El navegador no permite acceder a la cámara.'
      );


      return;

    }


    try {

      this.estado.set(
        'inicializando'
      );


      this.mensajeError.set(
        ''
      );


      this.streamCamara =
        await navigator
          .mediaDevices
          .getUserMedia({

            video: {

              width: {
                ideal:
                  1280
              },

              height: {
                ideal:
                  720
              },

              facingMode:
                'user'

            },

            audio:
              false

          });


      /*
       * El componente pudo haberse destruido
       * mientras el navegador pedía permisos.
       */
      if (
        this.destruido
      ) {

        this.detenerCamara();

        return;

      }


      await this.adjuntarStreamVideo();


      this.estado.set(
        'listo'
      );

    }
    catch (
      error
    ) {

      console.error(
        'Error iniciando cámara:',
        error
      );


      this.estado.set(
        'error'
      );


      this.mensajeError.set(
        'No fue posible acceder a la cámara. Verifique los permisos del navegador.'
      );

    }

  }


  // =====================================================
  // ADJUNTAR MEDIASTREAM AL VIDEO
  // =====================================================

  private async adjuntarStreamVideo():
    Promise<void> {

    const video =
      this.videoCamara
        ?.nativeElement;


    if (
      !video ||
      !this.streamCamara
    ) {

      return;

    }


    /*
     * Solo reasignamos srcObject
     * si realmente es necesario.
     */
    if (
      video.srcObject !==
      this.streamCamara
    ) {

      video.srcObject =
        this.streamCamara;

    }


    try {

      await video.play();

    }
    catch (
      error
    ) {

      console.error(
        'Error reproduciendo video:',
        error
      );

    }

  }


  // =====================================================
  // REGISTRAR MARCACIÓN
  // =====================================================

  registrarMarcacion():
    void {

    /*
     * Solo permitimos capturar
     * cuando el kiosko está disponible.
     */
    if (
      this.estado() !==
      'listo'
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

      this.mostrarError(
        'La cámara todavía no está disponible.'
      );

      return;

    }


    if (
      video.videoWidth ===
        0 ||
      video.videoHeight ===
        0
    ) {

      this.mostrarError(
        'Espere un momento hasta que la cámara esté completamente preparada.'
      );

      return;

    }


    this.estado.set(
      'procesando'
    );


    this.resultado.set(
      null
    );


    this.mensajeError.set(
      ''
    );


    // ===================================================
    // PREPARAR CANVAS
    // ===================================================

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

      this.mostrarError(
        'No fue posible procesar la imagen de la cámara.'
      );

      return;

    }


    // ===================================================
    // CAPTURA
    // ===================================================

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

          this.mostrarError(
            'No fue posible generar la fotografía para validar la marcación.'
          );

          return;

        }


        this.enviarMarcacion(
          blob
        );

      },

      'image/jpeg',

      0.92
    );

  }


  // =====================================================
  // ENVIAR MARCACIÓN
  // =====================================================

  private enviarMarcacion(
    blob:
      Blob
  ):
    void {

    this.kioscoService
      .registrarMarcacion(
        blob
      )
      .subscribe({

        next:
          response => {

            /*
             * response ya es:
             *
             * MarcacionFacialResultado
             *
             * porque ahora usamos el mismo
             * modelo definido en kiosco.service.ts.
             */
            this.resultado.set(
              response
            );


            this.estado.set(
              'exito'
            );


            this.programarReinicio();

          },


        error:
          error => {

            console.error(
              'Error registrando marcación:',
              error
            );


            const mensaje =
              this.obtenerMensajeError(
                error
              );


            this.mostrarError(
              mensaje
            );

          }

      });

  }


  // =====================================================
  // MOSTRAR ERROR
  // =====================================================

  private mostrarError(
    mensaje:
      string
  ):
    void {

    this.resultado.set(
      null
    );


    this.mensajeError.set(
      mensaje
    );


    this.estado.set(
      'error'
    );


    this.programarReinicio();

  }


  // =====================================================
  // NUEVA MARCACIÓN
  // =====================================================

  nuevaMarcacion():
    void {

    this.reiniciar();

  }


  // =====================================================
  // REINICIAR INTERFAZ
  //
  // IMPORTANTE:
  // la cámara NO se detiene.
  // =====================================================

  reiniciar():
    void {

    this.limpiarTemporizadorResultado();


    this.resultado.set(
      null
    );


    this.mensajeError.set(
      ''
    );


    /*
     * Si por alguna razón se perdió
     * el stream, intentamos recuperarlo.
     */
    if (
      !this.streamCamara
    ) {

      void this.iniciarCamara();

      return;

    }


    this.estado.set(
      'listo'
    );


    /*
     * El <video> permanece siempre en el DOM.
     * Solo comprobamos que siga asociado
     * al MediaStream.
     */
    void this.adjuntarStreamVideo();

  }


  // =====================================================
  // PROGRAMAR REINICIO AUTOMÁTICO
  // =====================================================

  private programarReinicio():
    void {

    this.limpiarTemporizadorResultado();


    this.temporizadorResultado =
      setTimeout(
        () => {

          if (
            !this.destruido
          ) {

            this.reiniciar();

          }

        },

        this.tiempoResultadoMs
      );

  }


  // =====================================================
  // LIMPIAR TEMPORIZADOR RESULTADO
  // =====================================================

  private limpiarTemporizadorResultado():
    void {

    if (
      this.temporizadorResultado
    ) {

      clearTimeout(
        this.temporizadorResultado
      );


      this.temporizadorResultado =
        null;

    }

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
  // ACTUALIZAR RELOJ
  // =====================================================

  private actualizarHora():
    void {

    const ahora =
      new Date();


    this.horaActual.set(
      ahora.toLocaleTimeString(
        'es-EC',
        {
          hour:
            '2-digit',

          minute:
            '2-digit',

          second:
            '2-digit',

          hour12:
            false
        }
      )
    );

  }


  // =====================================================
  // NOMBRE TIPO DE MARCACIÓN
  // =====================================================

  nombreTipoMarcacion(
    tipo:
      string |
      null |
      undefined
  ):
    string {

    switch (
      tipo
    ) {

      case 'Entrada':

        return 'Entrada';


      case 'InicioAlmuerzo':

        return 'Inicio de almuerzo';


      case 'FinAlmuerzo':

        return 'Fin de almuerzo';


      case 'Salida':

        return 'Salida';


      default:

        return tipo ??
          'Marcación';

    }

  }


  // =====================================================
  // OBTENER MENSAJE ERROR
  // =====================================================

  private obtenerMensajeError(
    error:
      any
  ):
    string {

    // ===================================================
    // ERROR COMO TEXTO
    // ===================================================

    if (
      typeof error?.error ===
      'string'
    ) {

      return error.error;

    }


    // ===================================================
    // RESPUESTA API { mensaje: "..." }
    // ===================================================

    if (
      error?.error?.mensaje
    ) {

      return error.error.mensaje;

    }


    // ===================================================
    // RESPUESTA API { detail: "..." }
    // ===================================================

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


    // ===================================================
    // NO AUTORIZADO
    // ===================================================

    if (
      error?.status ===
      401
    ) {

      return (
        'No fue posible autorizar este dispositivo ' +
        'para registrar la marcación.'
      );

    }


    // ===================================================
    // PROHIBIDO
    // ===================================================

    if (
      error?.status ===
      403
    ) {

      return (
        'El dispositivo no tiene permisos ' +
        'para registrar marcaciones.'
      );

    }


    // ===================================================
    // SIN CONEXIÓN API
    // ===================================================

    if (
      error?.status ===
      0
    ) {

      return (
        'No fue posible comunicarse con ' +
        'el servidor de marcaciones.'
      );

    }


    // ===================================================
    // MENSAJE GENERAL
    // ===================================================

    return (
      'No fue posible registrar la marcación. ' +
      'Intente nuevamente.'
    );

  }

}