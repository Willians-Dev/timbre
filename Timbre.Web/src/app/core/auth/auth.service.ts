import {
  Injectable
} from '@angular/core';

import {
  HttpClient
} from '@angular/common/http';

import {
  Observable,
  tap
} from 'rxjs';

import {
  environment
} from '../../../environments/environment';

import {
  LoginRequest,
  LoginResponse
} from './auth.models';


interface JwtPayload {

  exp?: number;

  sub?: string;

  name?: string;

  unique_name?: string;

  role?: string | string[];

  [
    key: string
  ]: unknown;

}


@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private readonly tokenKey =
    'timbre_token';


  private readonly claimNombre =
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';


  private readonly claimRol =
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';


  constructor(
    private readonly http:
      HttpClient
  ) {
  }


  // =====================================================
  // LOGIN
  // =====================================================

  login(
    request: LoginRequest
  ): Observable<LoginResponse> {

    return this.http
      .post<LoginResponse>(
        `${environment.apiUrl}/auth/login`,
        request
      )
      .pipe(

        tap(
          response => {

            if (
              response.token
            ) {

              localStorage.setItem(
                this.tokenKey,
                response.token
              );

            }

          }
        )

      );

  }


  // =====================================================
  // LOGOUT
  // =====================================================

  logout(): void {

    localStorage.removeItem(
      this.tokenKey
    );

  }


  // =====================================================
  // TOKEN
  // =====================================================

  getToken():
    string | null {

    return localStorage.getItem(
      this.tokenKey
    );

  }


  // =====================================================
  // AUTENTICADO
  // =====================================================

  isAuthenticated(): boolean {

    const token =
      this.getToken();


    if (
      !token
    ) {

      return false;

    }


    if (
      this.isTokenExpired(
        token
      )
    ) {

      this.logout();

      return false;

    }


    return true;

  }


  // =====================================================
  // TOKEN EXPIRADO
  // =====================================================

  isTokenExpired(
    token?: string | null
  ): boolean {

    const tokenActual =
      token ??
      this.getToken();


    if (
      !tokenActual
    ) {

      return true;

    }


    const payload =
      this.getPayload(
        tokenActual
      );


    if (
      !payload
    ) {

      return true;

    }


    if (
      !payload.exp
    ) {

      /*
       * Si el token no incluye exp,
       * no podemos declararlo vencido
       * desde el frontend.
       */
      return false;

    }


    const ahora =
      Math.floor(
        Date.now() / 1000
      );


    return payload.exp <=
      ahora;

  }


  // =====================================================
  // ROL
  // =====================================================

  getRole():
    string | null {

    const payload =
      this.getPayload();


    if (
      !payload
    ) {

      return null;

    }


    const rol =
      payload.role ??
      payload[
        this.claimRol
      ];


    if (
      Array.isArray(
        rol
      )
    ) {

      return rol.length > 0
        ? String(
            rol[0]
          )
        : null;

    }


    if (
      typeof rol ===
      'string'
    ) {

      return rol;

    }


    return null;

  }


  // =====================================================
  // ROLES
  // =====================================================

  getRoles():
    string[] {

    const payload =
      this.getPayload();


    if (
      !payload
    ) {

      return [];

    }


    const rol =
      payload.role ??
      payload[
        this.claimRol
      ];


    if (
      Array.isArray(
        rol
      )
    ) {

      return rol.map(
        valor =>
          String(
            valor
          )
      );

    }


    if (
      typeof rol ===
      'string'
    ) {

      return [
        rol
      ];

    }


    return [];

  }


  // =====================================================
  // TIENE ROL
  // =====================================================

  hasRole(
    rol: string
  ): boolean {

    return this.getRoles()
      .some(
        rolUsuario =>
          rolUsuario === rol
      );

  }


  // =====================================================
  // TIENE ALGUNO DE LOS ROLES
  // =====================================================

  hasAnyRole(
    roles: string[]
  ): boolean {

    const rolesUsuario =
      this.getRoles();


    return roles.some(
      rol =>
        rolesUsuario.includes(
          rol
        )
    );

  }


  // =====================================================
  // NOMBRE DE USUARIO
  // =====================================================

  getUsername():
    string {

    const payload =
      this.getPayload();


    if (
      !payload
    ) {

      return 'Usuario';

    }


    const nombre =
      payload.name ??
      payload.unique_name ??
      payload[
        this.claimNombre
      ] ??
      payload.sub;


    if (
      typeof nombre ===
      'string' &&
      nombre.trim()
    ) {

      return nombre;

    }


    return 'Usuario';

  }


  // =====================================================
  // INICIAL
  // =====================================================

  getUserInitial():
    string {

    const usuario =
      this.getUsername()
        .trim();


    if (
      !usuario
    ) {

      return 'U';

    }


    return usuario
      .charAt(0)
      .toUpperCase();

  }


  // =====================================================
  // PAYLOAD JWT
  // =====================================================

  private getPayload(
    token?: string | null
  ): JwtPayload | null {

    try {

      const tokenActual =
        token ??
        this.getToken();


      if (
        !tokenActual
      ) {

        return null;

      }


      const partes =
        tokenActual.split(
          '.'
        );


      if (
        partes.length !== 3
      ) {

        return null;

      }


      let payloadBase64 =
        partes[1]
          .replace(
            /-/g,
            '+'
          )
          .replace(
            /_/g,
            '/'
          );


      while (
        payloadBase64.length %
        4 !== 0
      ) {

        payloadBase64 +=
          '=';

      }


      const json =
        decodeURIComponent(

          Array.prototype.map
            .call(
              atob(
                payloadBase64
              ),
              (
                caracter:
                  string
              ) => {

                return (
                  '%' +
                  (
                    '00' +
                    caracter
                      .charCodeAt(0)
                      .toString(16)
                  ).slice(-2)
                );

              }
            )
            .join('')

        );


      return JSON.parse(
        json
      ) as JwtPayload;

    }
    catch (
      error
    ) {

      console.error(
        'No fue posible interpretar el JWT.',
        error
      );


      return null;

    }

  }

}