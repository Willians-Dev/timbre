import {
  Component
} from '@angular/core';

import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

import {
  AuthService
} from '../../core/auth/auth.service';

import {
  NotificationService
} from '../../core/services/notification.service';


@Component({
  selector:
    'app-admin-layout',

  standalone:
    true,

  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive
  ],

  templateUrl:
    './admin-layout.html',

  styleUrl:
    './admin-layout.css'
})
export class AdminLayout {

  // =====================================================
  // SIDEBAR
  // =====================================================

  sidebarCollapsed =
    false;


  // =====================================================
  // USUARIO
  // =====================================================

  readonly rol:
    string;


  readonly nombreUsuario:
    string;


  readonly inicialUsuario:
    string;


  // =====================================================
  // PERMISOS
  // =====================================================

  readonly esAdministrador:
    boolean;


  readonly esRRHH:
    boolean;


  readonly puedeAdministrar:
    boolean;


  constructor(

    private readonly authService:
      AuthService,

    private readonly router:
      Router,

    private readonly notificationService:
      NotificationService

  ) {

    // ===================================================
    // INFORMACIÓN DEL JWT
    // ===================================================

    this.rol =
      this.authService
        .getRole() ??
      'Sin rol';


    this.nombreUsuario =
      this.authService
        .getUsername();


    this.inicialUsuario =
      this.authService
        .getUserInitial();


    // ===================================================
    // PERMISOS
    // ===================================================

    this.esAdministrador =
      this.authService
        .hasRole(
          'Administrador'
        );


    this.esRRHH =
      this.authService
        .hasRole(
          'RRHH'
        );


    this.puedeAdministrar =
      this.authService
        .hasAnyRole(
          [
            'Administrador',
            'RRHH'
          ]
        );

  }


  // =====================================================
  // SIDEBAR
  // =====================================================

  toggleSidebar():
    void {

    this.sidebarCollapsed =
      !this.sidebarCollapsed;

  }


  // =====================================================
  // IR A PANTALLA DE MARCACIÓN
  // =====================================================

  irAKiosco():
    void {

    void this.router.navigate(
      [
        '/'
      ]
    );

  }


  // =====================================================
  // LOGOUT
  // =====================================================

  cerrarSesion():
    void {

    this.authService
      .logout();


    this.notificationService.info(

      'La sesión se cerró correctamente.',

      'Sesión finalizada'

    );


    void this.router.navigate(
      [
        '/login'
      ]
    );

  }

}