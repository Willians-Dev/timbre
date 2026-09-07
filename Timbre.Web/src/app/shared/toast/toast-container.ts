import {
  Component,
  inject
} from '@angular/core';

import {
  NotificationService
} from '../../core/services/notification.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [],
  templateUrl: './toast-container.html',
  styleUrl: './toast-container.css'
})
export class ToastContainer {

  readonly notificationService =
    inject(NotificationService);

  cerrar(id: number): void {
    this.notificationService.remove(id);
  }

}