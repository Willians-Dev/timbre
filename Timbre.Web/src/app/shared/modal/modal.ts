import {
  Component,
  EventEmitter,
  Input,
  Output
} from '@angular/core';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [],
  templateUrl: './modal.html',
  styleUrl: './modal.css'
})
export class Modal {

  @Input()
  abierto = false;

  @Input()
  titulo = '';

  @Input()
  ancho = '720px';

  @Output()
  cerrarModal =
    new EventEmitter<void>();

  cerrar(): void {
    this.cerrarModal.emit();
  }

  detenerPropagacion(
    event: MouseEvent
  ): void {
    event.stopPropagation();
  }
}