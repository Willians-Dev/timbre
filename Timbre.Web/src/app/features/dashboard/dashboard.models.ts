export interface DashboardMarcacion {

  idMarcacion: number;

  idEmpleado: number;

  empleado: string;

  hora: string;

  tipoMarcacion: string;

  origen: string;

  estadoEntrada?: string | null;
}


export interface DashboardResumen {

  fecha: string;

  horaActual: string;

  empleadosActivos: number;

  presentesHoy: number;

  atrasos: number;

  rostrosEnrolados: number;

  marcacionesHoy: number;

  jornadasCompletas: number;

  sinMarcacion: number;

  faciales: number;

  manuales: number;

  ultimasMarcaciones:
    DashboardMarcacion[];
}