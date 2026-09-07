import {
  Component,
  EventEmitter,
  Input,
  Output
} from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [],
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.css'
})
export class ConfirmDialog {

  @Input()
  abierto = false;

  @Input()
  titulo = 'Confirmar acción';

  @Input()
  mensaje = '';

  @Input()
  textoConfirmar = 'Confirmar';

  @Input()
  tipo: 'danger' | 'primary' = 'primary';

  @Output()
  confirmar = new EventEmitter<void>();

  @Output()
  cancelar = new EventEmitter<void>();

  aceptar(): void {
    this.confirmar.emit();
  }

  cerrar(): void {
    this.cancelar.emit();
  }

  detenerPropagacion(
    event: MouseEvent
  ): void {
    event.stopPropagation();
  }
}