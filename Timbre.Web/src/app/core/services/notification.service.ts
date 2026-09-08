import { Injectable, signal } from '@angular/core';

import {
  AppNotification
} from './notification.models';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {

  private readonly notificationsSignal =
    signal<AppNotification[]>([]);

  readonly notifications =
    this.notificationsSignal.asReadonly();

  private sequence = 0;

  success(
    message: string,
    title = 'Operación exitosa',
    duration = 3500
  ): void {

    this.show(
      'success',
      title,
      message,
      duration
    );
  }

  error(
    message: string,
    title = 'Error',
    duration = 5000
  ): void {

    this.show(
      'error',
      title,
      message,
      duration
    );
  }

  warning(
    message: string,
    title = 'Atención',
    duration = 4500
  ): void {

    this.show(
      'warning',
      title,
      message,
      duration
    );
  }

  info(
    message: string,
    title = 'Información',
    duration = 3500
  ): void {

    this.show(
      'info',
      title,
      message,
      duration
    );
  }

  remove(id: number): void {

    this.notificationsSignal.update(
      notifications =>
        notifications.filter(
          notification =>
            notification.id !== id
        )
    );
  }

  private show(
    type: AppNotification['type'],
    title: string,
    message: string,
    duration: number
  ): void {

    const id =
      ++this.sequence;

    const notification: AppNotification = {
      id,
      type,
      title,
      message,
      duration
    };

    this.notificationsSignal.update(
      notifications => [
        ...notifications,
        notification
      ]
    );

    window.setTimeout(
      () =>
        this.remove(id),
      duration
    );
  }
}