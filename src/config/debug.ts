const api_url_debug_predeterminada = 'http://localhost:5002';

// La API publicada: una URL publica no es un secreto, puede ir en el codigo.
const api_url_produccion = 'https://multitenantvargas.runasp.net';

export const debug_config = {
  debug_activado: import.meta.env.VITE_DEBUG_ACTIVADO !== 'false',

  api_url_debug: (
    import.meta.env.VITE_API_URL_DEBUG || api_url_debug_predeterminada
  ).replace(/\/+$/, ''),
};

export function obtener_api_url() {
  const api_url_entorno = (import.meta.env.VITE_API_URL || '').replace(/\/+$/, '');

  // Desarrollo: la API de la PC.
  if (debug_config.debug_activado) return debug_config.api_url_debug;

  // Release: el entorno llega vacio y queda la URL publicada.
  return api_url_entorno || api_url_produccion;
}
