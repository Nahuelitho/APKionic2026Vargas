// Copia local de la ultima respuesta de cada lectura.
// No conoce el dominio: guarda la respuesta de cualquier endpoint guardable.

const prefijo = 'multitenant_vargas_copia:';

// La clave es el endpoint con su query: cada lectura tiene su propia copia.
export function guardar_copia(endpoint: string, respuesta: unknown) {
  try {
    localStorage.setItem(prefijo + endpoint, JSON.stringify(respuesta));
  } catch (error) {
    // Una copia que no entra no debe cortar la lectura que si funciono.
    console.warn('[copia_local] No se pudo guardar la copia', endpoint, error);
  }
}

// Sin copia devuelve null, y la lectura termina en el error de siempre.
export function obtener_copia<T = unknown>(endpoint: string): T | null {
  try {
    const copia = localStorage.getItem(prefijo + endpoint);
    return copia ? JSON.parse(copia) as T : null;
  } catch {
    return null;
  }
}

// Al cerrar la sesion no queda nada: las copias son de quien las cargo.
export function borrar_copias() {
  Object.keys(localStorage)
    .filter(clave => clave.startsWith(prefijo))
    .forEach(clave => localStorage.removeItem(clave));
}
