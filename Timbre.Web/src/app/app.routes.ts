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
  // Página principal del sistema.
  // No requiere autenticación humana.
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
  // MI ASISTENCIA - EMPLEADO
  //
  // Ruta personal para el rol Empleado.
  // No utiliza el layout administrativo.
  //
  // URL:
  // /mi-asistencia
  // =====================================================

  {
    path: 'mi-asistencia',

    canActivate: [
      authGuard,
      roleGuard
    ],

    data: {
      roles: [
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
  //
  // Utiliza AdminLayout:
  // - sidebar
  // - topbar
  // - router-outlet
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
      //
      // Redirección inicial.
      // =================================================

      {
        path: '',

        pathMatch: 'full',

        redirectTo: 'dashboard'
      },


      // =================================================
      // DASHBOARD
      //
      // Administrador / RRHH
      //
      // URL:
      // /admin/dashboard
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
      //
      // Administrador / RRHH
      //
      // URL:
      // /admin/empleados
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
      //
      // Administrador / RRHH
      //
      // URL:
      // /admin/jornadas
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
      //
      // Administrador / RRHH
      //
      // URL:
      // /admin/marcaciones
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
      // HISTORIAL ADMINISTRATIVO
      //
      // Administrador / RRHH
      //
      // URL:
      // /admin/historial
      // =================================================

      {
        path: 'historial',

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
              './features/historial/historial'
            )
              .then(
                m =>
                  m.Historial
              )
      },


      // =================================================
      // REPORTES
      //
      // Administrador / RRHH
      //
      // URL:
      // /admin/reportes
      // =================================================

      {
        path: 'reportes',

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
              './features/reportes/reportes'
            )
              .then(
                m =>
                  m.Reportes
              )
      },


      // =================================================
      // MI ASISTENCIA
      //
      // Administrador / RRHH.
      //
      // Utiliza el mismo componente personal,
      // pero dentro de AdminLayout.
      //
      // URL:
      // /admin/mi-asistencia
      // =================================================

      {
        path: 'mi-asistencia',

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
              './features/mi-asistencia/mi-asistencia'
            )
              .then(
                m =>
                  m.MiAsistencia
              )
      },


      // =================================================
      // USUARIOS
      //
      // Solo Administrador
      //
      // URL:
      // /admin/usuarios
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
      //
      // Solo Administrador
      //
      // URL:
      // /admin/auditoria
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

    ]
  },


  // =====================================================
  // RUTA DESCONOCIDA
  //
  // Cualquier URL inexistente regresa al kiosko.
  // =====================================================

  {
    path: '**',

    redirectTo: ''
  }

];