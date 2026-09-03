import { obtener_api_url } from '../config/debug';

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

export async function ajax_request<T>(endpoint: string): Promise<T> {
  try {
    const url = construir_url(endpoint);

    const respuesta = await fetch(url, {
      method: 'GET',
      headers: {
        Accept: 'application/json',
      },
    });

    console.log('HTTP STATUS:', respuesta.status);

    const respuesta_json = await respuesta.json().catch(() => null);

    console.log('RESPUESTA API:', respuesta_json);

    if (!respuesta.ok) {
      throw {
        estado_http: respuesta.status,
        codigo: respuesta_json?.codigo || 'error_http',
        mensaje:
          respuesta_json?.mensaje ||
          'No se pudo completar la solicitud.',
        respuesta: respuesta_json,
      };
    }

    return respuesta_json as T;
  } catch (error) {
    throw normalizar_error(error);
  }
}