<template>
  <div class="editor-empresa">
    <ion-card>
      <ion-card-header>
        <ion-card-title>Datos de la empresa</ion-card-title>
      </ion-card-header>
      <ion-card-content>
        <ion-input v-model="nombre" label="Nombre" label-placement="stacked" :maxlength="160" :disabled="guardando" />
        <ion-item v-if="mostrar_activo" lines="none">
          <ion-toggle v-model="activo" :disabled="guardando">Empresa activa</ion-toggle>
        </ion-item>
        <p v-if="mostrar_activo" class="ayuda">
          Desactivar bloquea las membresías y el catálogo público; conserva los pedidos.
        </p>
      </ion-card-content>
    </ion-card>

    <ion-card>
      <ion-card-header>
        <ion-card-title>Ubicación del local</ion-card-title>
      </ion-card-header>
      <ion-card-content>
        <ion-input v-model="direccion_linea" label="Dirección" label-placement="stacked" :disabled="guardando" />
        <ion-input v-model="referencia" label="Referencia (opcional)" label-placement="stacked" :disabled="guardando" />

        <div class="coordenadas">
          <h3>Coordenadas</h3>
          <p v-if="tiene_coordenadas(latitud, longitud)">{{ formatear_coordenadas(latitud, longitud) }}</p>
          <p v-else>Sin coordenadas cargadas</p>
        </div>

        <div class="acciones">
          <ion-button fill="outline" :disabled="capturando || guardando" @click="capturar">
            <ion-icon slot="start" :icon="locateOutline"></ion-icon>
            {{ capturando ? 'Capturando...' : 'Estoy en el local' }}
          </ion-button>
          <ion-button fill="outline" :disabled="!tiene_coordenadas(latitud, longitud) || guardando" @click="ver_mapa">
            <ion-icon slot="start" :icon="mapOutline"></ion-icon>
            Ver en el mapa
          </ion-button>
        </div>

        <ion-button expand="block" :disabled="guardando || !nombre.trim()" @click="guardar">
          {{ guardando ? 'Guardando...' : 'Guardar cambios' }}
        </ion-button>

        <ion-text v-if="mensaje" color="success"><p>{{ mensaje }}</p></ion-text>
        <ion-text v-if="error" color="danger"><p role="alert">{{ error }}</p></ion-text>
      </ion-card-content>
    </ion-card>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { IonButton, IonCard, IonCardContent, IonCardHeader, IonCardTitle, IonIcon, IonInput, IonItem, IonText, IonToggle } from '@ionic/vue';
import { locateOutline, mapOutline } from 'ionicons/icons';
import { Browser } from '@capacitor/browser';
import { actualizar_empresa, type Empresa } from '../services/empresas_service';
import { formatear_coordenadas, obtener_ubicacion_actual, tiene_coordenadas, url_mapa } from '../services/geolocalizacion_service';

const props = withDefaults(defineProps<{ empresa: Empresa; mostrar_activo?: boolean }>(), { mostrar_activo: false });
const emit = defineEmits<{ guardado: [Empresa] }>();

const nombre = ref(''), activo = ref(true);
const direccion_linea = ref(''), referencia = ref('');
const latitud = ref<number | null>(null), longitud = ref<number | null>(null);
const guardando = ref(false), capturando = ref(false);
const mensaje = ref(''), error = ref('');
const texto_error = (e: unknown) => e && typeof e === 'object' && 'mensaje' in e
  ? String((e as { mensaje: unknown }).mensaje)
  : 'No se pudieron guardar los cambios.';

watch(() => props.empresa, (empresa) => {
  nombre.value = empresa.nombre_empresa;
  activo.value = empresa.activo;
  direccion_linea.value = empresa.direccion_linea ?? '';
  referencia.value = empresa.referencia ?? '';
  latitud.value = empresa.latitud ?? null;
  longitud.value = empresa.longitud ?? null;
  error.value = '';
}, { immediate: true });

async function capturar() {
  capturando.value = true; mensaje.value = ''; error.value = '';
  console.log('[empresa] capturando ubicación de', props.empresa.id);
  try {
    const res = await obtener_ubicacion_actual();
    console.log('[empresa] resultado captura', res);
    if (res.ok && res.ubicacion) {
      latitud.value = res.ubicacion.latitud;
      longitud.value = res.ubicacion.longitud;
      mensaje.value = 'Coordenadas capturadas. Acordate de guardar.';
    } else {
      error.value = res.mensaje || 'No se pudo obtener la ubicación.';
    }
  } catch (e) {
    console.error('[empresa] error al capturar', e);
    error.value = 'Error al obtener la ubicación.';
  } finally { capturando.value = false; }
}

async function ver_mapa() {
  const url = url_mapa(latitud.value, longitud.value);
  console.log('[empresa] abriendo mapa', url);
  if (!url) { error.value = 'Todavía no hay coordenadas cargadas.'; return; }
  await Browser.open({ url });
}

async function guardar() {
  if (guardando.value || !nombre.value.trim()) return;
  guardando.value = true; mensaje.value = ''; error.value = '';
  const cuerpo: Empresa = {
    ...props.empresa,
    nombre_empresa: nombre.value.trim(),
    activo: activo.value,
    direccion_linea: direccion_linea.value.trim() || null,
    referencia: referencia.value.trim() || null,
    latitud: latitud.value,
    longitud: longitud.value,
  };
  console.log('[empresa] guardando', cuerpo);
  try {
    const guardada = await actualizar_empresa(cuerpo);
    console.log('[empresa] guardada', guardada);
    mensaje.value = 'Cambios guardados correctamente.';
    emit('guardado', guardada ?? cuerpo);
  } catch (e) {
    console.error('[empresa] error al guardar', e);
    error.value = texto_error(e);
  } finally { guardando.value = false; }
}
</script>

<style scoped>
.coordenadas h3 { margin: 16px 0 4px; font-size: 0.95rem; font-weight: 600; }
.coordenadas p { margin: 0 0 8px; color: var(--ion-color-medium); }
.acciones { display: flex; flex-wrap: wrap; gap: 8px; }
.ayuda { color: var(--ion-color-medium); font-size: 0.85rem; }
</style>
