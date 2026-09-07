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
  JornadasService
} from './jornadas.service';

import {
  CrearJornada,
  JornadaLaboral
} from './jornada.models';


interface FormularioJornada {

  idJornada: number | null;

  nombre: string;

  horaEntrada: string;

  horaInicioAlmuerzo: string;

  horaFinAlmuerzo: string;

  horaSalida: string;

  toleranciaEntradaMinutos: number;

  lunes: boolean;

  martes: boolean;

  miercoles: boolean;

  jueves: boolean;

  viernes: boolean;

  sabado: boolean;

  domingo: boolean;

  activo: boolean;
}


@Component({
  selector: 'app-jornadas',

  standalone: true,

  imports: [
    CommonModule,
    FormsModule
  ],

  templateUrl:
    './jornadas.html',

  styleUrl:
    './jornadas.css'
})
export class Jornadas {

  readonly jornadas =
    signal<JornadaLaboral[]>([]);


  readonly cargando =
    signal(false);


  readonly guardando =
    signal(false);


  readonly mostrarModal =
    signal(false);


  readonly mensajeError =
    signal('');


  readonly mensajeExito =
    signal('');


  readonly terminoBusqueda =
    signal('');


  readonly formulario =
    signal<FormularioJornada>(
      this.formularioInicial()
    );


  readonly jornadasFiltradas =
    computed(
      () => {

        const termino =
          this.terminoBusqueda()
            .trim()
            .toLowerCase();


        if (!termino) {

          return this.jornadas();

        }


        return this.jornadas()
          .filter(
            jornada => {

              return jornada.nombre
                .toLowerCase()
                .includes(
                  termino
                );

            }
          );

      }
    );


  readonly editando =
    computed(
      () =>
        this.formulario()
          .idJornada !== null
    );


  constructor(
    private readonly jornadasService:
      JornadasService
  ) {

    this.cargarJornadas();

  }


  // =====================================================
  // CARGAR
  // =====================================================

  cargarJornadas(): void {

    this.cargando.set(
      true
    );


    this.jornadasService
      .listar()
      .subscribe({

        next:
          jornadas => {

            this.jornadas.set(
              jornadas
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


            this.mostrarMensajeError(
              this.obtenerMensajeError(
                error
              )
            );

          }

      });

  }


  // =====================================================
  // NUEVA
  // =====================================================

  nuevaJornada(): void {

    this.formulario.set(
      this.formularioInicial()
    );


    this.mensajeError.set(
      ''
    );


    this.mostrarModal.set(
      true
    );

  }


  // =====================================================
  // EDITAR
  // =====================================================

  editar(
    jornada: JornadaLaboral
  ): void {

    this.formulario.set({

      idJornada:
        jornada.idJornada,

      nombre:
        jornada.nombre,

      horaEntrada:
        this.normalizarHora(
          jornada.horaEntrada
        ),

      horaInicioAlmuerzo:
        this.normalizarHora(
          jornada.horaInicioAlmuerzo
        ),

      horaFinAlmuerzo:
        this.normalizarHora(
          jornada.horaFinAlmuerzo
        ),

      horaSalida:
        this.normalizarHora(
          jornada.horaSalida
        ),

      toleranciaEntradaMinutos:
        jornada
          .toleranciaEntradaMinutos,

      lunes:
        jornada.lunes,

      martes:
        jornada.martes,

      miercoles:
        jornada.miercoles,

      jueves:
        jornada.jueves,

      viernes:
        jornada.viernes,

      sabado:
        jornada.sabado,

      domingo:
        jornada.domingo,

      activo:
        jornada.activo
    });


    this.mensajeError.set(
      ''
    );


    this.mostrarModal.set(
      true
    );

  }


  // =====================================================
  // CERRAR
  // =====================================================

  cerrarModal(): void {

    if (
      this.guardando()
    ) {

      return;

    }


    this.mostrarModal.set(
      false
    );


    this.mensajeError.set(
      ''
    );

  }


  // =====================================================
  // FORMULARIO
  // =====================================================

  actualizarCampo<
    K extends keyof FormularioJornada
  >(
    campo: K,
    valor: FormularioJornada[K]
  ): void {

    this.formulario.update(
      actual => ({

        ...actual,

        [campo]:
          valor

      })
    );

  }


  // =====================================================
  // GUARDAR
  // =====================================================

  guardar(): void {

    this.mensajeError.set(
      ''
    );


    const formulario =
      this.formulario();


    const validacion =
      this.validarFormulario(
        formulario
      );


    if (validacion) {

      this.mensajeError.set(
        validacion
      );


      return;

    }


    this.guardando.set(
      true
    );


    const dto =
      this.construirDto(
        formulario
      );


    if (
      formulario.idJornada ===
      null
    ) {

      this.jornadasService
        .crear(
          dto
        )
        .subscribe({

          next:
            response => {

              this.guardando.set(
                false
              );


              this.mostrarModal.set(
                false
              );


              this.mostrarMensajeExito(
                response?.mensaje ??
                'Jornada creada correctamente.'
              );


              this.cargarJornadas();

            },


          error:
            error => {

              this.guardando.set(
                false
              );


              this.mensajeError.set(
                this.obtenerMensajeError(
                  error
                )
              );

            }

        });


      return;

    }


    this.jornadasService
      .actualizar(
        formulario.idJornada,
        {
          ...dto,
          activo:
            formulario.activo
        }
      )
      .subscribe({

        next:
          response => {

            this.guardando.set(
              false
            );


            this.mostrarModal.set(
              false
            );


            this.mostrarMensajeExito(
              response?.mensaje ??
              'Jornada actualizada correctamente.'
            );


            this.cargarJornadas();

          },


        error:
          error => {

            this.guardando.set(
              false
            );


            this.mensajeError.set(
              this.obtenerMensajeError(
                error
              )
            );

          }

      });

  }


  // =====================================================
  // ESTADO
  // =====================================================

  cambiarEstado(
    jornada: JornadaLaboral
  ): void {

    const accion =
      jornada.activo
        ? 'desactivar'
        : 'activar';


    const continuar =
      confirm(
        `¿Desea ${accion} la jornada "${jornada.nombre}"?`
      );


    if (!continuar) {

      return;

    }


    this.jornadasService
      .cambiarEstado(
        jornada.idJornada,
        !jornada.activo
      )
      .subscribe({

        next:
          response => {

            this.mostrarMensajeExito(
              response?.mensaje ??
              'Estado actualizado correctamente.'
            );


            this.cargarJornadas();

          },


        error:
          error => {

            this.mostrarMensajeError(
              this.obtenerMensajeError(
                error
              )
            );

          }

      });

  }


  // =====================================================
  // VALIDACIONES
  // =====================================================

  private validarFormulario(
    form: FormularioJornada
  ): string | null {

    if (
      !form.nombre.trim()
    ) {

      return 'El nombre de la jornada es obligatorio.';

    }


    if (
      !form.horaEntrada ||
      !form.horaInicioAlmuerzo ||
      !form.horaFinAlmuerzo ||
      !form.horaSalida
    ) {

      return 'Debe completar todos los horarios.';

    }


    if (
      form.toleranciaEntradaMinutos <
      0
    ) {

      return 'La tolerancia de entrada no puede ser negativa.';

    }


    if (
      form.horaEntrada >=
      form.horaSalida
    ) {

      return 'La hora de entrada debe ser menor que la hora de salida.';

    }


    if (
      form.horaInicioAlmuerzo >=
      form.horaFinAlmuerzo
    ) {

      return 'La hora de inicio de almuerzo debe ser menor que la hora de fin de almuerzo.';

    }


    if (
      form.horaInicioAlmuerzo <=
        form.horaEntrada ||

      form.horaFinAlmuerzo >=
        form.horaSalida
    ) {

      return 'El horario de almuerzo debe estar dentro de la jornada laboral.';

    }


    const tieneDia =
      form.lunes ||
      form.martes ||
      form.miercoles ||
      form.jueves ||
      form.viernes ||
      form.sabado ||
      form.domingo;


    if (!tieneDia) {

      return 'Debe seleccionar al menos un día laborable.';

    }


    return null;

  }


  // =====================================================
  // DTO
  // =====================================================

  private construirDto(
    form: FormularioJornada
  ): CrearJornada {

    return {

      nombre:
        form.nombre.trim(),

      horaEntrada:
        this.horaApi(
          form.horaEntrada
        ),

      horaInicioAlmuerzo:
        this.horaApi(
          form.horaInicioAlmuerzo
        ),

      horaFinAlmuerzo:
        this.horaApi(
          form.horaFinAlmuerzo
        ),

      horaSalida:
        this.horaApi(
          form.horaSalida
        ),

      toleranciaEntradaMinutos:
        Number(
          form.toleranciaEntradaMinutos
        ),

      lunes:
        form.lunes,

      martes:
        form.martes,

      miercoles:
        form.miercoles,

      jueves:
        form.jueves,

      viernes:
        form.viernes,

      sabado:
        form.sabado,

      domingo:
        form.domingo
    };

  }


  // =====================================================
  // HORAS
  // =====================================================

  private horaApi(
    hora: string
  ): string {

    if (
      hora.length === 5
    ) {

      return `${hora}:00`;

    }


    return hora;

  }


  private normalizarHora(
    hora: string
  ): string {

    if (!hora) {

      return '';

    }


    return hora.substring(
      0,
      5
    );

  }


  // =====================================================
  // FORM INICIAL
  // =====================================================

  private formularioInicial():
    FormularioJornada {

    return {

      idJornada:
        null,

      nombre:
        '',

      horaEntrada:
        '08:00',

      horaInicioAlmuerzo:
        '12:30',

      horaFinAlmuerzo:
        '13:15',

      horaSalida:
        '17:00',

      toleranciaEntradaMinutos:
        15,

      lunes:
        true,

      martes:
        true,

      miercoles:
        true,

      jueves:
        true,

      viernes:
        true,

      sabado:
        false,

      domingo:
        false,

      activo:
        true

    };

  }


  // =====================================================
  // MENSAJES
  // =====================================================

  private obtenerMensajeError(
    error: any
  ): string {

    return (
      error?.error?.mensaje ??
      error?.message ??
      'Ocurrió un error al procesar la solicitud.'
    );

  }


  private mostrarMensajeExito(
    mensaje: string
  ): void {

    this.mensajeExito.set(
      mensaje
    );


    setTimeout(
      () => {

        this.mensajeExito.set(
          ''
        );

      },
      3500
    );

  }


  private mostrarMensajeError(
    mensaje: string
  ): void {

    this.mensajeError.set(
      mensaje
    );


    setTimeout(
      () => {

        if (
          !this.mostrarModal()
        ) {

          this.mensajeError.set(
            ''
          );

        }

      },
      4500
    );

  }

}