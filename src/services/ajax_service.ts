import { obtener_api_url } from '../config/debug';
import { limpiar_sesion, obtener_access_token, renovar_sesion } from './auth_service';
import { guardar_copia, obtener_copia } from './copia_local_service';
import { vibrar_error } from './vibracion_service';

type AjaxError = {
  estado_http: number;
  codigo: string;
  mensaje: string;
  respuesta?: unknown;
};

function construir_url(endpoint: string) {
  // Se resuelve en cada peticion: el unico lugar que decide la URL es debug.ts.
  const api_url = obtener_api_url();

  if (!api_url) {
    throw new Error('No se configuro la URL de la API.');
  }

  const url = `${api_url}/${endpoint.replace(/^\/+/, '')}`;

  console.log('API BASE:', api_url);
  console.log('REQUEST URL:', url);

  return url;
}

function normalizar_error(error: unknown, silencioso = false): AjaxError {
  if (!silencioso) void vibrar_error();
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
  binario?: boolean;
  silencioso?: boolean;
  // Marca una lectura: con red guarda copia y sin red la entrega.
  guardable?: boolean;
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
    if (respuesta.ok && options.binario) return await respuesta.blob() as T;
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
    // Con red la lectura se guarda por endpoint para poder leerla sin conexion.
    if (options.guardable && !options.binario) guardar_copia(endpoint, respuesta_json);
    return respuesta_json as T;
  } catch (error) {
    // Sin red (status 0) y con copia, se entrega la ultima lectura guardada.
    const es_error_de_red = error instanceof TypeError
      || (typeof error === 'object' && error !== null && 'estado_http' in error && (error as AjaxError).estado_http === 0);
    if (options.guardable && !options.binario && es_error_de_red) {
      const copia = obtener_copia<T>(endpoint);
      if (copia !== null) return copia;
    }
    throw normalizar_error(error, options.silencioso);
  }
}

export function ajax_binario(endpoint: string): Promise<Blob> {
  return ajax_request<Blob>(endpoint, { binario: true });
}

function ejecutar_fetch(url: string, method: string, body?: unknown) {
  const token = obtener_access_token();
  const es_form_data = body instanceof FormData;
  return fetch(url, {
      method,
      headers: {
        Accept: 'application/json, application/pdf, image/png',
        ...(body && !es_form_data ? { 'Content-Type': 'application/json' } : {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: es_form_data ? body : body ? JSON.stringify(body) : undefined,
    });
}
