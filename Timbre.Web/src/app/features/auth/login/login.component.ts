import {
  Component
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  FormsModule
} from '@angular/forms';

import {
  Router
} from '@angular/router';

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
    FormsModule
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

  cargando =
    false;


  mensajeError =
    '';


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
  // INGRESAR
  // =====================================================

  ingresar():
    void {

    this.mensajeError =
      '';


    // ===================================================
    // VALIDACIONES
    // ===================================================

    if (
      !this.nombreUsuario
        .trim() ||
      !this.password
    ) {

      this.mensajeError =
        'Ingrese usuario y contraseña.';


      return;

    }


    // ===================================================
    // LOGIN
    // ===================================================

    this.cargando =
      true;


    this.authService
      .login({

        nombreUsuario:
          this.nombreUsuario
            .trim(),

        password:
          this.password

      })
      .subscribe({

        // =================================================
        // LOGIN CORRECTO
        // =================================================

        next:
          () => {

            this.cargando =
              false;


            this.notificationService
              .success(

                'Inicio de sesión realizado correctamente.',

                'Bienvenido'

              );


            this.redirigirSegunRol();

          },


        // =================================================
        // ERROR LOGIN
        // =================================================

        error:
          error => {

            this.cargando =
              false;


            const mensaje =
              error?.error?.mensaje ??
              'No fue posible iniciar sesión.';


            this.mensajeError =
              mensaje;


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


    this.mensajeError =
      'El usuario no tiene un rol autorizado para ingresar al sistema.';


    this.notificationService
      .error(

        this.mensajeError,

        'Acceso no autorizado'

      );

  }

}