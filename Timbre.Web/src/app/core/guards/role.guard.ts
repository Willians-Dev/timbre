import {
  inject
} from '@angular/core';

import {
  CanActivateFn,
  Router
} from '@angular/router';

import {
  AuthService
} from '../auth/auth.service';


export const roleGuard:
  CanActivateFn =
  route => {

    const authService =
      inject(
        AuthService
      );


    const router =
      inject(
        Router
      );


    // =====================================================
    // PRIMERO AUTENTICACIÓN
    // =====================================================

    if (
      !authService
        .isAuthenticated()
    ) {

      return router.createUrlTree(
        [
          '/login'
        ]
      );

    }


    // =====================================================
    // ROLES PERMITIDOS
    // =====================================================

    const rolesPermitidos =
      route.data[
        'roles'
      ] as string[] |
        undefined;


    /*
     * Si la ruta no especifica roles,
     * basta con estar autenticado.
     */
    if (
      !rolesPermitidos ||
      rolesPermitidos.length === 0
    ) {

      return true;

    }


    if (
      authService.hasAnyRole(
        rolesPermitidos
      )
    ) {

      return true;

    }


    /*
     * Está autenticado pero no tiene
     * autorización para esa pantalla.
     */
    return router.createUrlTree(
      [
        '/admin'
      ]
    );

  };