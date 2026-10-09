import { Capacitor } from '@capacitor/core';
import { Camera, CameraResultType, CameraSource } from '@capacitor/camera';

export type ResultadoFoto = {
  ok: boolean;
  archivo: File | null;
  mensaje: string | null;
};

export function validar_foto(archivo: File): string | null {
  if (!['image/jpeg', 'image/png', 'image/webp'].includes(archivo.type)) {
    return 'La foto debe ser JPEG, PNG o WebP.';
  }
  if (archivo.size === 0) {
    return 'La foto esta vacia.';
  }
  if (archivo.size > 5 * 1024 * 1024) {
    return 'La foto no puede superar los 5 MB.';
  }
  return null;
}

export async function obtener_foto(origen: 'camara' | 'galeria'): Promise<ResultadoFoto> {
  try {
    if (origen === 'camara') {
      let permisos = await Camera.checkPermissions();
      if (permisos.camera === 'prompt' || permisos.camera === 'prompt-with-rationale') {
        permisos = await Camera.requestPermissions({ permissions: ['camera'] });
      }
      if (permisos.camera !== 'granted') {
        return { ok: false, archivo: null, mensaje: 'Habilita el permiso de camara en los ajustes para tomar una foto.' };
      }
    } else if (Capacitor.getPlatform() === 'ios') {
      // Android v8 usa Photo Picker (o ACTION_OPEN_DOCUMENT), sin permiso de almacenamiento.
      let permisos = await Camera.checkPermissions();
      if (permisos.photos === 'prompt' || permisos.photos === 'prompt-with-rationale') {
        permisos = await Camera.requestPermissions({ permissions: ['photos'] });
      }
      if (permisos.photos !== 'granted' && permisos.photos !== 'limited') {
        return { ok: false, archivo: null, mensaje: 'Habilita el acceso a fotos en los ajustes para elegir una foto.' };
      }
    }

    const foto = await Camera.getPhoto({
      source: origen === 'camara' ? CameraSource.Camera : CameraSource.Photos,
      resultType: CameraResultType.Uri,
      quality: 90,
      saveToGallery: false,
      allowEditing: false,
    });
    const uri = foto.webPath || (foto.path ? Capacitor.convertFileSrc(foto.path) : null);
    if (!uri) {
      return { ok: false, archivo: null, mensaje: 'No se pudo leer la foto seleccionada.' };
    }
    const respuesta = await fetch(uri);
    if (!respuesta.ok) {
      return { ok: false, archivo: null, mensaje: 'No se pudo leer la foto seleccionada.' };
    }
    const blob = await respuesta.blob();
    const formato = foto.format.toLowerCase().replace(/^jpg$/, 'jpeg');
    const tipo = blob.type && blob.type !== 'application/octet-stream' ? blob.type : `image/${formato}`;
    const extension = tipo === 'image/jpeg' ? 'jpg' : tipo.split('/')[1];
    const archivo = new File([blob], `producto-${Date.now()}.${extension}`, { type: tipo });
    const mensaje = validar_foto(archivo);
    return { ok: !mensaje, archivo: mensaje ? null : archivo, mensaje };
  } catch (excepcion) {
    const error = excepcion as { message?: string; code?: string };
    const cancelado = /cancel|no image picked|no photo selected/i.test(error?.message || String(excepcion))
      || ['OS-PLUG-CAMR-0006', 'OS-PLUG-CAMR-0020'].includes(error?.code || '');
    return {
      ok: false,
      archivo: null,
      mensaje: cancelado ? null : 'No se pudo obtener la foto. Revisa los permisos e intenta nuevamente.',
    };
  }
}
