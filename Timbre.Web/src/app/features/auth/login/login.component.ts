import {
  Component,
  signal
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  FormsModule
} from '@angular/forms';

import {
  Router,
  RouterLink
} from '@angular/router';

import {
  finalize
} from 'rxjs';

import {
  AuthService
} from '../../../core/auth/auth.service';

import {
  NotificationService
} from '../../../core/services/notification.service';


@Component({
  selector:
    'app-login',

  standalone:
    true,

  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],

  templateUrl:
    './login.component.html',

  styleUrl:
    './login.component.css'
})
export class LoginComponent {

  // =====================================================
  // FORMULARIO
  // =====================================================

  nombreUsuario =
    '';


  password =
    '';


  // =====================================================
  // ESTADO
  // =====================================================

  readonly cargando =
    signal(
      false
    );


  readonly mensajeError =
    signal(
      ''
    );


  readonly intentoEnviar =
    signal(
      false
    );


  readonly mostrarPassword =
    signal(
      false
    );


  constructor(

    private readonly authService:
      AuthService,

    private readonly router:
      Router,

    private readonly notificationService:
      NotificationService

  ) {
  }


  // =====================================================
  // MOSTRAR / OCULTAR CONTRASEÑA
  // =====================================================

  alternarPassword():
    void {

    this.mostrarPassword.update(
      valor =>
        !valor
    );

  }


  // =====================================================
  // LIMPIAR ERROR AL ESCRIBIR
  // =====================================================

  limpiarError():
    void {

    if (
      this.mensajeError()
    ) {

      this.mensajeError.set(
        ''
      );

    }

  }


  // =====================================================
  // INGRESAR
  // =====================================================

  ingresar():
    void {

    // ===================================================
    // EVITAR DOBLE ENVÍO
    // ===================================================

    if (
      this.cargando()
    ) {

      return;

    }


    this.intentoEnviar.set(
      true
    );


    this.mensajeError.set(
      ''
    );


    const usuario =
      this.nombreUsuario
        .trim();


    // ===================================================
    // VALIDACIONES DEL FORMULARIO
    // ===================================================

    if (
      !usuario ||
      !this.password
    ) {

      this.mensajeError.set(
        'Complete el usuario y la contraseña para continuar.'
      );


      return;

    }


    // ===================================================
    // LOGIN
    // ===================================================

    this.cargando.set(
      true
    );


    this.authService
      .login({

        nombreUsuario:
          usuario,

        password:
          this.password

      })
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

        // =================================================
        // LOGIN CORRECTO
        // =================================================

        next:
          () => {

            this.intentoEnviar.set(
              false
            );


            this.mensajeError.set(
              ''
            );


            this.notificationService
              .success(
                'Inicio de sesión realizado correctamente.',
                'Bienvenido'
              );


            this.redirigirSegunRol();

          },


        // =================================================
        // LOGIN INCORRECTO / ERROR
        // =================================================

        error:
          error => {

            let mensaje:
              string;


            // =============================================
            // CREDENCIALES INCORRECTAS
            //
            // No revelamos si falló:
            // - usuario
            // - contraseña
            //
            // Evita enumeración de usuarios.
            // =============================================

            if (
              error?.status ===
              401
            ) {

              mensaje =
                'Usuario o contraseña incorrectos.';

            }


            // =============================================
            // SIN CONEXIÓN CON API
            // =============================================

            else if (
              error?.status ===
              0
            ) {

              mensaje =
                'No fue posible comunicarse con el servidor.';

            }


            // =============================================
            // OTROS ERRORES
            // =============================================

            else {

              mensaje =
                error?.error?.mensaje ??
                'No fue posible iniciar sesión. Intente nuevamente.';

            }


            // =============================================
            // MOSTRAR MENSAJE
            // =============================================

            this.mensajeError.set(
              mensaje
            );


            // =============================================
            // LIMPIAR SOLO CONTRASEÑA
            //
            // Conservamos el nombre de usuario para
            // facilitar un nuevo intento.
            // =============================================

            this.password =
              '';


            this.mostrarPassword.set(
              false
            );


            // =============================================
            // IMPORTANTE
            //
            // Evita que inmediatamente aparezca:
            //
            // "La contraseña es obligatoria."
            //
            // Esa validación se mostrará nuevamente
            // solo si el usuario pulsa Ingresar con
            // el campo vacío.
            // =============================================

            this.intentoEnviar.set(
              false
            );


            // =============================================
            // NOTIFICACIÓN
            // =============================================

            this.notificationService
              .error(
                mensaje,
                'No se pudo iniciar sesión'
              );

          }

      });

  }


  // =====================================================
  // REDIRECCIÓN SEGÚN ROL
  // =====================================================

  private redirigirSegunRol():
    void {

    const rol =
      this.authService
        .getRole();


    // ===================================================
    // EMPLEADO
    // ===================================================

    if (
      rol ===
      'Empleado'
    ) {

      void this.router.navigate(
        [
          '/mi-asistencia'
        ]
      );


      return;

    }


    // ===================================================
    // ADMINISTRADOR / RRHH
    // ===================================================

    if (
      rol ===
        'Administrador' ||
      rol ===
        'RRHH'
    ) {

      void this.router.navigate(
        [
          '/admin/dashboard'
        ]
      );


      return;

    }


    // ===================================================
    // ROL NO SOPORTADO
    // ===================================================

    this.authService
      .logout();


    const mensaje =
      'El usuario no tiene un rol autorizado para ingresar al sistema.';


    this.mensajeError.set(
      mensaje
    );


    this.notificationService
      .error(
        mensaje,
        'Acceso no autorizado'
      );

  }

}