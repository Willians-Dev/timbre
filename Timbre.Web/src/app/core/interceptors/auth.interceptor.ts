import {
  HttpErrorResponse,
  HttpInterceptorFn
} from '@angular/common/http';

import {
  inject
} from '@angular/core';

import {
  Router
} from '@angular/router';

import {
  catchError,
  throwError
} from 'rxjs';

import {
  AuthService
} from '../auth/auth.service';

import {
  NotificationService
} from '../services/notification.service';


export const authInterceptor: HttpInterceptorFn =
  (
    req,
    next
  ) => {

    const authService =
      inject(
        AuthService
      );


    const router =
      inject(
        Router
      );


    const notificationService =
      inject(
        NotificationService
      );


    const token =
      authService
        .getToken();


    // =====================================================
    // IDENTIFICAR ENDPOINTS ESPECIALES
    // =====================================================

    const esLogin =
      req.url.includes(
        '/auth/login'
      );


    const esMarcacionFacialPublica =
      req.url.includes(
        '/marcaciones-faciales/registrar'
      );


    let request =
      req;


    // =====================================================
    // AGREGAR JWT
    //
    // No enviamos token al login.
    //
    // Tampoco es necesario enviarlo al kiosko público.
    // =====================================================

    if (
      token &&
      !esLogin &&
      !esMarcacionFacialPublica
    ) {

      request =
        req.clone({

          setHeaders: {

            Authorization:
              `Bearer ${token}`

          }

        });

    }


    return next(
      request
    )
      .pipe(

        catchError(
          (
            error:
              HttpErrorResponse
          ) => {

            // =============================================
            // LOGIN INCORRECTO
            //
            // Un 401 aquí significa credenciales inválidas.
            // NO cerramos sesión.
            // NO redirigimos.
            //
            // El LoginComponent debe manejar el mensaje.
            // =============================================

            if (
              esLogin
            ) {

              return throwError(
                () =>
                  error
              );

            }


            // =============================================
            // KIOSKO PÚBLICO
            //
            // Si el endpoint facial devuelve algún error,
            // la propia pantalla del kiosko lo maneja.
            //
            // Nunca debe cerrar una sesión humana.
            // =============================================

            if (
              esMarcacionFacialPublica
            ) {

              return throwError(
                () =>
                  error
              );

            }


            // =============================================
            // SESIÓN EXPIRADA / TOKEN INVÁLIDO
            // =============================================

            if (
              error.status ===
                401 &&
              token
            ) {

              authService
                .logout();


              notificationService
                .warning(

                  'Su sesión ha expirado. Inicie sesión nuevamente.',

                  'Sesión finalizada'

                );


              void router.navigate(
                [
                  '/login'
                ]
              );

            }


            // =============================================
            // PROPAGAR ERROR
            // =============================================

            return throwError(
              () =>
                error
            );

          }
        )

      );

  };