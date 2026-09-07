import {
  Component,
  OnDestroy,
  OnInit,
  signal
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  DashboardService
} from './dashboard.service';

import {
  DashboardResumen
} from './dashboard.models';

import {
  NotificationService
} from '../../core/services/notification.service';


@Component({
  selector:
    'app-dashboard',

  standalone:
    true,

  imports: [
    CommonModule
  ],

  styleUrl:
    './dashboard.component.css',

  templateUrl:
    './dashboard.component.html'
})
export class DashboardComponent
  implements OnInit, OnDestroy {

  readonly resumen =
    signal<DashboardResumen | null>(
      null
    );


  readonly cargando =
    signal(false);


  private intervalo:
    ReturnType<typeof setInterval> |
    null =
    null;


  constructor(
    private readonly dashboardService:
      DashboardService,

    private readonly notificationService:
      NotificationService
  ) {
  }


  ngOnInit(): void {

    this.cargar();


    /*
     * Refresco operativo.
     *
     * El dashboard se actualiza cada 60 segundos.
     */
    this.intervalo =
      setInterval(
        () => {

          this.cargar(
            false
          );

        },
        60000
      );

  }


  ngOnDestroy(): void {

    if (
      this.intervalo
    ) {

      clearInterval(
        this.intervalo
      );


      this.intervalo =
        null;

    }

  }


  cargar(
    mostrarCarga = true
  ): void {

    if (
      mostrarCarga
    ) {

      this.cargando.set(
        true
      );

    }


    this.dashboardService
      .obtenerResumen()
      .subscribe({

        next:
          response => {

            this.resumen.set(
              response
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


            this.notificationService
              .error(

                error?.error?.mensaje ??
                'No fue posible cargar el dashboard.',

                'Dashboard'

              );

          }

      });

  }


  nombreTipo(
    tipo: string
  ): string {

    switch (
      tipo
    ) {

      case 'InicioAlmuerzo':

        return 'Inicio almuerzo';


      case 'FinAlmuerzo':

        return 'Fin almuerzo';


      default:

        return tipo;

    }

  }

}