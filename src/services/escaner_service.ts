import { Capacitor } from '@capacitor/core';
import { BarcodeScanner, BarcodeFormat } from '@capacitor-mlkit/barcode-scanning';
import { parsear_codigo_pedido } from './pedidos_service';

export async function escanear_pedido(signal?: AbortSignal): Promise<number | null> {
  if (!Capacitor.isNativePlatform()) return null;
  try {
    signal?.throwIfAborted();
    if (!(await BarcodeScanner.isSupported()).supported) throw new Error('El escaner no esta disponible. Ingresa el codigo manualmente.');
    signal?.throwIfAborted();
    if (Capacitor.getPlatform() === 'android') {
      if (!(await BarcodeScanner.isGoogleBarcodeScannerModuleAvailable()).available) {
        signal?.throwIfAborted();
        await BarcodeScanner.installGoogleBarcodeScannerModule();
        // Installation is asynchronous; never launch scan before the module is ready.
        const limite = Date.now() + 60000;
        while (!(await BarcodeScanner.isGoogleBarcodeScannerModuleAvailable()).available) {
          signal?.throwIfAborted();
          if (Date.now() >= limite) throw new Error('El modulo del escaner sigue instalando. Reintenta o ingresa el codigo.');
          await new Promise(resolve => setTimeout(resolve, 500));
        }
      }
      // Google Code Scanner provides its own UI and needs no camera permission.
    } else {
      let permiso = await BarcodeScanner.checkPermissions();
      if (permiso.camera === 'prompt' || permiso.camera === 'prompt-with-rationale') permiso = await BarcodeScanner.requestPermissions();
      if (permiso.camera !== 'granted' && permiso.camera !== 'limited') throw new Error('Permiso de camara denegado. Ingresa el codigo manualmente.');
    }
    signal?.throwIfAborted();
    const resultado = await BarcodeScanner.scan({ formats: [BarcodeFormat.QrCode] });
    signal?.throwIfAborted();
    if (!resultado.barcodes.length) return null;
    return parsear_codigo_pedido(resultado.barcodes[0].rawValue || resultado.barcodes[0].displayValue);
  } catch (error) {
    const mensaje = error instanceof Error ? error.message : String(error);
    if (signal?.aborted || /cancel|dismiss/i.test(mensaje)) return null;
    throw error;
  }
}
