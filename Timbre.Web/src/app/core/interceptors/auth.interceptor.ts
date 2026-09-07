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
  (req, next) => {

    const authService =
      inject(AuthService);

    const router =
      inject(Router);

    const notificationService =
      inject(NotificationService);

    const token =
      authService.getToken();

    let request =
      req;


    // =====================================================
    // AGREGAR JWT
    // =====================================================

    if (token) {

      request =
        req.clone({
          setHeaders: {
            Authorization:
              `Bearer ${token}`
          }
        });

    }


    return next(request)
      .pipe(

        catchError(
          (
            error:
              HttpErrorResponse
          ) => {

            // =============================================
            // SESIÓN EXPIRADA / TOKEN INVÁLIDO
            // =============================================

            if (
              error.status === 401 &&
              token &&
              !req.url.includes(
                '/auth/login'
              )
            ) {

              authService.logout();

              notificationService.warning(
                'Su sesión ha expirado. Inicie sesión nuevamente.',
                'Sesión finalizada'
              );

              void router.navigate(
                ['/login']
              );

            }


            return throwError(
              () => error
            );

          }
        )

      );

  };