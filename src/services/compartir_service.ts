import { Capacitor } from '@capacitor/core';
import { Directory, Filesystem } from '@capacitor/filesystem';
import { Share } from '@capacitor/share';

export async function compartir_pdf(blob: Blob, nombre: string, signal?: AbortSignal) {
  if (!/^[A-Za-z0-9_-]+\.pdf$/.test(nombre)) throw new Error('Nombre de comprobante invalido.');
  signal?.throwIfAborted();
  if (!Capacitor.isNativePlatform()) {
    const url = URL.createObjectURL(blob);
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = nombre;
    document.body.appendChild(enlace);
    try { enlace.click(); }
    finally {
      enlace.remove();
      // Give the browser time to consume the download URL.
      setTimeout(() => URL.revokeObjectURL(url), 60000);
    }
    return;
  }
  if (!(await Share.canShare()).value) throw new Error('No se puede compartir en este dispositivo.');
  const base64 = await new Promise<string>((resolve, reject) => {
    const lector = new FileReader();
    lector.onerror = () => reject(new Error('No se pudo leer el comprobante.'));
    lector.onload = () => resolve(String(lector.result).split(',')[1]);
    lector.readAsDataURL(blob);
  });
  signal?.throwIfAborted();
  const path = nombre;
  const archivo = await Filesystem.writeFile({ path, data: base64, directory: Directory.Cache });
  try {
    signal?.throwIfAborted();
    await Share.share({ title: 'Comprobante de pedido', files: [archivo.uri], dialogTitle: 'Compartir comprobante' });
  } catch (error) {
    const mensaje = error instanceof Error ? error.message : String(error);
    if (!/cancel|dismiss/i.test(mensaje)) throw error;
  }
  // El receptor puede leer la URI despues de cerrar el dialogo. Android limpia Cache.
}
