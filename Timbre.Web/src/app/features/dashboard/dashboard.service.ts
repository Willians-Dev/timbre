import {
  Injectable
} from '@angular/core';

import {
  HttpClient
} from '@angular/common/http';

import {
  Observable
} from 'rxjs';

import {
  environment
} from '../../../environments/environment';

import {
  DashboardResumen
} from './dashboard.models';


@Injectable({
  providedIn: 'root'
})
export class DashboardService {

  private readonly apiUrl =
    `${environment.apiUrl}/dashboard`;


  constructor(
    private readonly http:
      HttpClient
  ) {
  }


  obtenerResumen():
    Observable<DashboardResumen> {

    return this.http.get<
      DashboardResumen
    >(
      `${this.apiUrl}/resumen`
    );

  }

}