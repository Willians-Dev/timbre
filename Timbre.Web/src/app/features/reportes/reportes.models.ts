export type TipoReporte =
  | 'asistencia'
  | 'atrasos'
  | 'incompletas'
  | 'marcaciones';


export type FormatoReporte =
  | 'excel'
  | 'pdf';


export interface ReporteEmpleadoFiltro {

  idEmpleado:
    number;

  nombre:
    string;

  area?:
    string | null;
}


export interface ReporteFiltrosResponse {

  empleados:
    ReporteEmpleadoFiltro[];

  areas:
    string[];
}