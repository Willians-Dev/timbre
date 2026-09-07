import {
  Component,
  OnInit,
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
  forkJoin
} from 'rxjs';

import {
  finalize
} from 'rxjs/operators';

import {
  Modal
} from '../../shared/modal/modal';

import {
  ConfirmDialog
} from '../../shared/confirm-dialog/confirm-dialog';

import {
  NotificationService
} from '../../core/services/notification.service';

import {
  UsuariosService
} from './usuarios.service';

import {
  ActualizarUsuarioRequest,
  CrearUsuarioRequest,
  EmpleadoDisponible,
  ResetPasswordRequest,
  RolUsuario,
  Usuario
} from './usuario.models';


interface UsuarioForm {

  idEmpleado:
    number | null;

  nombreUsuario:
    string;

  idRol:
    number | null;

  password:
    string;

  confirmarPassword:
    string;

  activo:
    boolean;

}


@Component({
  selector: 'app-usuarios',

  standalone: true,

  imports: [
    CommonModule,
    FormsModule,
    Modal,
    ConfirmDialog
  ],

  templateUrl:
    './usuarios.html',

  styleUrl:
    './usuarios.css'
})
export class Usuarios
  implements OnInit {

  // =====================================================
  // LISTADO
  // =====================================================

  readonly usuarios =
    signal<Usuario[]>([]);


  readonly filtro =
    signal('');


  readonly cargando =
    signal(false);


  readonly usuariosFiltrados =
    computed(
      () => {

        const texto =
          this.filtro()
            .trim()
            .toLowerCase();

        const lista =
          this.usuarios();

        if (!texto)
        {
          return lista;
        }

        return lista.filter(
          usuario =>

            usuario.empleado
              .toLowerCase()
              .includes(texto) ||

            usuario.nombreUsuario
              .toLowerCase()
              .includes(texto) ||

            usuario.rol
              .toLowerCase()
              .includes(texto)
        );

      }
    );


  // =====================================================
  // CATÁLOGOS
  // =====================================================

  readonly roles =
    signal<RolUsuario[]>([]);


  readonly empleadosDisponibles =
    signal<EmpleadoDisponible[]>([]);


  // =====================================================
  // MODAL USUARIO
  // =====================================================

  readonly modalUsuarioAbierto =
    signal(false);


  readonly modoEdicion =
    signal(false);


  readonly guardando =
    signal(false);


  usuarioEditando:
    Usuario | null =
    null;


  formulario:
    UsuarioForm =
    this.crearFormularioVacio();


  // =====================================================
  // CONFIRMACIÓN ESTADO
  // =====================================================

  readonly confirmacionAbierta =
    signal(false);


  usuarioCambioEstado:
    Usuario | null =
    null;


  readonly procesandoEstado =
    signal(false);


  // =====================================================
  // RESET PASSWORD
  // =====================================================

  readonly modalPasswordAbierto =
    signal(false);


  readonly guardandoPassword =
    signal(false);


  usuarioPassword:
    Usuario | null =
    null;


  passwordNuevo =
    '';


  confirmarPassword =
    '';


  // =====================================================
  // CONSTRUCTOR
  // =====================================================

  constructor(

    private readonly usuariosService:
      UsuariosService,

    private readonly notificationService:
      NotificationService

  ) {
  }


  // =====================================================
  // INIT
  // =====================================================

  ngOnInit(): void {

    this.cargarUsuarios();

  }


  // =====================================================
  // FILTRO
  // =====================================================

  cambiarFiltro(
    valor: string
  ): void {

    this.filtro.set(
      valor
    );

  }


  // =====================================================
  // CARGAR USUARIOS
  // =====================================================

  cargarUsuarios(): void {

    this.cargando.set(
      true
    );


    this.usuariosService
      .obtenerTodos()
      .pipe(

        finalize(
          () => {

            this.cargando.set(
              false
            );

          }
        )

      )
      .subscribe({

        next:
          usuarios => {

            this.usuarios.set(
              usuarios
            );

          },


        error:
          error => {

            if (
              error?.status === 401
            ) {
              return;
            }

            this.notificationService.error(

              this.obtenerMensajeError(
                error
              ),

              'Error al consultar usuarios'

            );

          }

      });

  }


  // =====================================================
  // NUEVO USUARIO
  // =====================================================

  nuevoUsuario(): void {

    this.modoEdicion.set(
      false
    );


    this.usuarioEditando =
      null;


    this.formulario =
      this.crearFormularioVacio();


    this.cargarCatalogos(
      undefined,
      () => {

        this.modalUsuarioAbierto.set(
          true
        );

      }
    );

  }


  // =====================================================
  // EDITAR
  // =====================================================

  editarUsuario(
    usuario: Usuario
  ): void {

    this.modoEdicion.set(
      true
    );


    this.usuarioEditando =
      usuario;


    this.formulario = {

      idEmpleado:
        usuario.idEmpleado,

      nombreUsuario:
        usuario.nombreUsuario,

      idRol:
        usuario.idRol,

      password:
        '',

      confirmarPassword:
        '',

      activo:
        usuario.activo

    };


    this.cargarCatalogos(
      usuario.idUsuario,
      () => {

        this.modalUsuarioAbierto.set(
          true
        );

      }
    );

  }


  // =====================================================
  // CATÁLOGOS
  // =====================================================

  private cargarCatalogos(
    idUsuarioActual:
      number | undefined,
    callback:
      () => void
  ): void {

    forkJoin({

      roles:
        this.usuariosService
          .obtenerRoles(),

      empleados:
        this.usuariosService
          .obtenerEmpleadosDisponibles(
            idUsuarioActual
          )

    })
      .subscribe({

        next:
          resultado => {

            this.roles.set(
              resultado.roles
            );


            this.empleadosDisponibles.set(
              resultado.empleados
            );


            callback();

          },


        error:
          error => {

            this.notificationService.error(

              this.obtenerMensajeError(
                error
              ),

              'No fue posible cargar los catálogos'

            );

          }

      });

  }


  // =====================================================
  // CERRAR MODAL
  // =====================================================

  cerrarModalUsuario(): void {

    if (
      this.guardando()
    ) {
      return;
    }


    this.modalUsuarioAbierto.set(
      false
    );


    this.usuarioEditando =
      null;

  }


  // =====================================================
  // GUARDAR
  // =====================================================

  guardarUsuario(): void {

    if (
      !this.formulario.idEmpleado ||
      !this.formulario.idRol ||
      !this.formulario.nombreUsuario.trim()
    ) {

      this.notificationService.warning(

        'Complete todos los campos obligatorios.',

        'Datos incompletos'

      );

      return;

    }


    if (
      !this.modoEdicion()
    ) {

      if (
        !this.formulario.password
      ) {

        this.notificationService.warning(

          'Ingrese una contraseña.',

          'Contraseña requerida'

        );

        return;

      }


      if (
        this.formulario.password.length <
        8
      ) {

        this.notificationService.warning(

          'La contraseña debe contener al menos 8 caracteres.',

          'Contraseña inválida'

        );

        return;

      }


      if (
        this.formulario.password !==
        this.formulario.confirmarPassword
      ) {

        this.notificationService.warning(

          'La confirmación de la contraseña no coincide.',

          'Contraseñas diferentes'

        );

        return;

      }

    }


    this.guardando.set(
      true
    );


    if (
      this.modoEdicion() &&
      this.usuarioEditando
    ) {

      this.actualizarUsuario();

    }
    else {

      this.crearUsuario();

    }

  }


  // =====================================================
  // CREAR
  // =====================================================

  private crearUsuario(): void {

    const request:
      CrearUsuarioRequest = {

      idEmpleado:
        this.formulario.idEmpleado!,

      idRol:
        this.formulario.idRol!,

      nombreUsuario:
        this.formulario.nombreUsuario
          .trim(),

      password:
        this.formulario.password

    };


    this.usuariosService
      .crear(
        request
      )
      .pipe(

        finalize(
          () => {

            this.guardando.set(
              false
            );

          }
        )

      )
      .subscribe({

        next:
          respuesta => {

            this.modalUsuarioAbierto.set(
              false
            );


            this.notificationService.success(

              respuesta.mensaje ??
              'Usuario creado correctamente.',

              'Usuario creado'

            );


            this.cargarUsuarios();

          },


        error:
          error => {

            this.notificationService.error(

              this.obtenerMensajeError(
                error
              ),

              'Error al crear usuario'

            );

          }

      });

  }


  // =====================================================
  // ACTUALIZAR
  // =====================================================

  private actualizarUsuario(): void {

    if (
      !this.usuarioEditando
    ) {
      return;
    }


    const request:
      ActualizarUsuarioRequest = {

      idRol:
        this.formulario.idRol!,

      nombreUsuario:
        this.formulario.nombreUsuario
          .trim(),

      activo:
        this.formulario.activo

    };


    this.usuariosService
      .actualizar(
        this.usuarioEditando.idUsuario,
        request
      )
      .pipe(

        finalize(
          () => {

            this.guardando.set(
              false
            );

          }
        )

      )
      .subscribe({

        next:
          respuesta => {

            this.modalUsuarioAbierto.set(
              false
            );


            this.notificationService.success(

              respuesta.mensaje ??
              'Usuario actualizado correctamente.',

              'Cambios guardados'

            );


            this.cargarUsuarios();

          },


        error:
          error => {

            this.notificationService.error(

              this.obtenerMensajeError(
                error
              ),

              'Error al actualizar usuario'

            );

          }

      });

  }


  // =====================================================
  // ESTADO
  // =====================================================

  cambiarEstado(
    usuario: Usuario
  ): void {

    this.usuarioCambioEstado =
      usuario;


    this.confirmacionAbierta.set(
      true
    );

  }


  cancelarCambioEstado(): void {

    if (
      this.procesandoEstado()
    ) {
      return;
    }


    this.confirmacionAbierta.set(
      false
    );


    this.usuarioCambioEstado =
      null;

  }


  confirmarCambioEstado(): void {

    const usuario =
      this.usuarioCambioEstado;


    if (!usuario)
    {
      return;
    }


    const nuevoEstado =
      !usuario.activo;


    this.procesandoEstado.set(
      true
    );


    this.usuariosService
      .cambiarEstado(
        usuario.idUsuario,
        nuevoEstado
      )
      .pipe(

        finalize(
          () => {

            this.procesandoEstado.set(
              false
            );

          }
        )

      )
      .subscribe({

        next:
          respuesta => {

            this.confirmacionAbierta.set(
              false
            );


            this.usuarioCambioEstado =
              null;


            this.notificationService.success(

              respuesta.mensaje,

              'Estado actualizado'

            );


            this.cargarUsuarios();

          },


        error:
          error => {

            this.confirmacionAbierta.set(
              false
            );


            this.usuarioCambioEstado =
              null;


            this.notificationService.error(

              this.obtenerMensajeError(
                error
              ),

              'No fue posible modificar el estado'

            );

          }

      });

  }


  // =====================================================
  // RESET PASSWORD
  // =====================================================

  abrirResetPassword(
    usuario: Usuario
  ): void {

    this.usuarioPassword =
      usuario;


    this.passwordNuevo =
      '';


    this.confirmarPassword =
      '';


    this.modalPasswordAbierto.set(
      true
    );

  }


  cerrarResetPassword(): void {

    if (
      this.guardandoPassword()
    ) {
      return;
    }


    this.modalPasswordAbierto.set(
      false
    );


    this.usuarioPassword =
      null;


    this.passwordNuevo =
      '';


    this.confirmarPassword =
      '';

  }


  guardarResetPassword(): void {

    if (
      !this.usuarioPassword
    ) {
      return;
    }


    if (
      this.passwordNuevo.length <
      8
    ) {

      this.notificationService.warning(

        'La contraseña debe contener al menos 8 caracteres.',

        'Contraseña inválida'

      );

      return;

    }


    if (
      this.passwordNuevo !==
      this.confirmarPassword
    ) {

      this.notificationService.warning(

        'La confirmación de la contraseña no coincide.',

        'Contraseñas diferentes'

      );

      return;

    }


    const request:
      ResetPasswordRequest = {

      passwordNuevo:
        this.passwordNuevo,

      confirmarPassword:
        this.confirmarPassword

    };


    this.guardandoPassword.set(
      true
    );


    this.usuariosService
      .resetPassword(
        this.usuarioPassword.idUsuario,
        request
      )
      .pipe(

        finalize(
          () => {

            this.guardandoPassword.set(
              false
            );

          }
        )

      )
      .subscribe({

        next:
          respuesta => {

            this.modalPasswordAbierto.set(
              false
            );


            this.usuarioPassword =
              null;


            this.passwordNuevo =
              '';


            this.confirmarPassword =
              '';


            this.notificationService.success(

              respuesta.mensaje,

              'Contraseña actualizada'

            );

          },


        error:
          error => {

            this.notificationService.error(

              this.obtenerMensajeError(
                error
              ),

              'Error al restablecer contraseña'

            );

          }

      });

  }


  // =====================================================
  // FORMULARIO
  // =====================================================

  private crearFormularioVacio():
    UsuarioForm {

    return {

      idEmpleado:
        null,

      nombreUsuario:
        '',

      idRol:
        null,

      password:
        '',

      confirmarPassword:
        '',

      activo:
        true

    };

  }


  // =====================================================
  // ERROR
  // =====================================================

  private obtenerMensajeError(
    error: any
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
      return error.error.detail;
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


    return 'Ocurrió un error inesperado.';
  }

}