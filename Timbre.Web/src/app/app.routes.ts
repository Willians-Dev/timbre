import {
  Routes
} from '@angular/router';

import {
  authGuard
} from './core/guards/auth.guard';

import {
  roleGuard
} from './core/guards/role.guard';


export const routes: Routes = [

  // =====================================================
  // KIOSKO / PANTALLA PÚBLICA
  //
  // Es la página principal del sistema.
  // No requiere login humano.
  // =====================================================

  {
    path: '',

    pathMatch: 'full',

    loadComponent:
      () =>
        import(
          './features/kiosco/kiosco'
        )
          .then(
            m =>
              m.Kiosco
          )
  },


  // =====================================================
  // LOGIN
  // =====================================================

  {
    path: 'login',

    loadComponent:
      () =>
        import(
          './features/auth/login/login.component'
        )
          .then(
            m =>
              m.LoginComponent
          )
  },


  // =====================================================
  // MI ASISTENCIA
  //
  // Pantalla personal de cualquier usuario humano.
  // =====================================================

  {
    path: 'mi-asistencia',

    canActivate: [
      authGuard,
      roleGuard
    ],

    data: {
      roles: [
        'Administrador',
        'RRHH',
        'Empleado'
      ]
    },

    loadComponent:
      () =>
        import(
          './features/mi-asistencia/mi-asistencia'
        )
          .then(
            m =>
              m.MiAsistencia
          )
  },


  // =====================================================
  // ÁREA ADMINISTRATIVA
  // =====================================================

  {
    path: 'admin',

    canActivate: [
      authGuard
    ],

    loadComponent:
      () =>
        import(
          './shared/admin-layout/admin-layout'
        )
          .then(
            m =>
              m.AdminLayout
          ),

    children: [

      // =================================================
      // /admin
      // =================================================

      {
        path: '',

        pathMatch: 'full',

        redirectTo: 'dashboard'
      },


      // =================================================
      // DASHBOARD
      // Administrador / RRHH
      // =================================================

      {
        path: 'dashboard',

        canActivate: [
          roleGuard
        ],

        data: {
          roles: [
            'Administrador',
            'RRHH'
          ]
        },

        loadComponent:
          () =>
            import(
              './features/dashboard/dashboard.component'
            )
              .then(
                m =>
                  m.DashboardComponent
              )
      },


      // =================================================
      // EMPLEADOS
      // Administrador / RRHH
      // =================================================

      {
        path: 'empleados',

        canActivate: [
          roleGuard
        ],

        data: {
          roles: [
            'Administrador',
            'RRHH'
          ]
        },

        loadComponent:
          () =>
            import(
              './features/empleados/empleados'
            )
              .then(
                m =>
                  m.Empleados
              )
      },


      // =================================================
      // JORNADAS
      // Administrador / RRHH
      // =================================================

      {
        path: 'jornadas',

        canActivate: [
          roleGuard
        ],

        data: {
          roles: [
            'Administrador',
            'RRHH'
          ]
        },

        loadComponent:
          () =>
            import(
              './features/jornadas/jornadas'
            )
              .then(
                m =>
                  m.Jornadas
              )
      },


      // =================================================
      // MARCACIONES
      // Administrador / RRHH
      // =================================================

      {
        path: 'marcaciones',

        canActivate: [
          roleGuard
        ],

        data: {
          roles: [
            'Administrador',
            'RRHH'
          ]
        },

        loadComponent:
          () =>
            import(
              './features/marcaciones/marcaciones'
            )
              .then(
                m =>
                  m.Marcaciones
              )
      },


      // =================================================
      // USUARIOS
      // Solo Administrador
      // =================================================

      {
        path: 'usuarios',

        canActivate: [
          roleGuard
        ],

        data: {
          roles: [
            'Administrador'
          ]
        },

        loadComponent:
          () =>
            import(
              './features/usuarios/usuarios'
            )
              .then(
                m =>
                  m.Usuarios
              )
      },


      // =================================================
      // AUDITORÍA
      // Solo Administrador
      // =================================================

      {
        path: 'auditoria',

        canActivate: [
          roleGuard
        ],

        data: {
          roles: [
            'Administrador'
          ]
        },

        loadComponent:
          () =>
            import(
              './features/auditoria/auditoria'
            )
              .then(
                m =>
                  m.Auditoria
              )
      }

      /*
       * HISTORIAL y REPORTES se agregarán
       * cuando creemos esos componentes.
       *
       * No los ponemos todavía para evitar
       * imports a archivos inexistentes.
       */

    ]
  },


  // =====================================================
  // CUALQUIER RUTA DESCONOCIDA
  // =====================================================

  {
    path: '**',

    redirectTo: ''
  }

];