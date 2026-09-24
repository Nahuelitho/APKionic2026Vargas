import { obtener_api_url } from '../config/debug';
import { limpiar_sesion, obtener_access_token, renovar_sesion } from './auth_service';

const api_url = obtener_api_url();

type AjaxError = {
  estado_http: number;
  codigo: string;
  mensaje: string;
  respuesta?: unknown;
};

function construir_url(endpoint: string) {
  if (!api_url) {
    throw new Error('No se configuro la URL de la API.');
  }

  const url = `${api_url}/${endpoint.replace(/^\/+/, '')}`;

  console.log('API BASE:', api_url);
  console.log('REQUEST URL:', url);

  return url;
}

function normalizar_error(error: unknown): AjaxError {
  console.error('ERROR AJAX:', error);

  if (typeof error === 'object' && error !== null && 'estado_http' in error) return error as AjaxError;

  if (error instanceof TypeError) {
    return {
      estado_http: 0,
      codigo: 'sin_conexion',
      mensaje:
        'No se pudo conectar con la API. Revisá que esté levantada y que la URL sea correcta.',
    };
  }

  if (error instanceof Error) {
    return {
      estado_http: 0,
      codigo: 'error_ajax',
      mensaje: error.message,
    };
  }

  return {
    estado_http: 0,
    codigo: 'error_desconocido',
    mensaje: 'No se pudo completar la solicitud.',
  };
}

type AjaxRequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
};

export async function ajax_request<T>(
  endpoint: string,
  options: AjaxRequestOptions = {},
): Promise<T> {
  try {
    const url = construir_url(endpoint);
    const method = options.method || 'GET';

    let respuesta = await ejecutar_fetch(url, method, options.body);

    if (respuesta.status === 401 && await renovar_sesion()) {
      respuesta = await ejecutar_fetch(url, method, options.body);
    } else if (respuesta.status === 401) {
      limpiar_sesion();
    }

    console.log('HTTP STATUS:', respuesta.status);
    const respuesta_json = await respuesta.json().catch(() => null);
    console.log('RESPUESTA API:', respuesta_json);

    if (!respuesta.ok) {
      throw {
        estado_http: respuesta.status,
        codigo: respuesta_json?.codigo || 'error_http',
        mensaje: respuesta_json?.mensaje || (respuesta.status === 403 ? 'No tenés permiso para realizar esta acción.' : 'No se pudo completar la solicitud.'),
        respuesta: respuesta_json,
      };
    }
    return respuesta_json as T;
  } catch (error) {
    throw normalizar_error(error);
  }
}

function ejecutar_fetch(url: string, method: string, body?: unknown) {
  const token = obtener_access_token();
  return fetch(url, {
      method,
      headers: {
        Accept: 'application/json',
        ...(body ? { 'Content-Type': 'application/json' } : {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: body ? JSON.stringify(body) : undefined,
    });
}
