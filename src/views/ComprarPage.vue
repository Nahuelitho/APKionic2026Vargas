<template>
  <comp-page titulo="Tiendas" :mostrar_actualizar="true" @actualizar="actualizar">
    <div class="ion-padding">
      <h1>Elegí tu tienda</h1>
      <p>Elegí una tienda para ver sus productos y preparar tu pedido.</p>

      <comp-esqueleto v-if="cargando && !empresas.length" />
      <ion-text v-if="error" color="danger"><p role="alert">{{ error }}</p></ion-text>
      <ion-button v-if="error" fill="outline" @click="cargar">Reintentar</ion-button>
      <p v-if="!cargando && !error && !empresas.length">No hay tiendas disponibles por el momento.</p>

      <div v-if="empresas.length" class="grid-empresas">
        <ion-card v-for="empresa in empresas" :key="empresa.id" class="card-empresa">
          <ion-card-header class="card-cabecera">
            <ion-icon :icon="storefrontOutline" />
            <ion-card-title>{{ empresa.nombre_empresa }}</ion-card-title>
          </ion-card-header>

          <ion-card-content class="card-datos">
            <div class="dato">
              <ion-icon :icon="locationOutline"></ion-icon>
              <span>{{ empresa.direccion_linea || 'Sin dirección cargada' }}</span>
            </div>
            <div v-if="empresa.referencia" class="dato">
              <ion-icon :icon="informationCircleOutline"></ion-icon>
              <span>{{ empresa.referencia }}</span>
            </div>
            <div class="dato">
              <ion-icon :icon="navigateOutline"></ion-icon>
              <span v-if="tiene_coordenadas(empresa.latitud, empresa.longitud)">
                {{ formatear_coordenadas(empresa.latitud, empresa.longitud) }}
              </span>
              <span v-else>Sin coordenadas cargadas</span>
            </div>
          </ion-card-content>

          <div class="card-acciones">
            <ion-button expand="block" fill="solid" :router-link="`/app/catalogo/${empresa.id}`">
              Ver productos
            </ion-button>
            <ion-button
              fill="outline"
              data-test="mapa"
              :disabled="!tiene_coordenadas(empresa.latitud, empresa.longitud)"
              @click="ver_mapa(empresa)"
            >
              <ion-icon slot="icon-only" :icon="mapOutline"></ion-icon>
            </ion-button>
          </div>
        </ion-card>
      </div>
    </div>
  </comp-page>
</template>

<script setup lang="ts">
import { onBeforeUnmount, ref } from 'vue';
import { IonButton, IonCard, IonCardContent, IonCardHeader, IonCardTitle, IonIcon, IonText, onIonViewWillEnter, onIonViewWillLeave } from '@ionic/vue';
import { informationCircleOutline, locationOutline, mapOutline, navigateOutline, storefrontOutline } from 'ionicons/icons';
import { Browser } from '@capacitor/browser';
import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import { obtener_empresas, type Empresa } from '../services/empresas_service';
import { formatear_coordenadas, tiene_coordenadas, url_mapa } from '../services/geolocalizacion_service';

const empresas = ref<Empresa[]>([]), cargando = ref(false), error = ref('');
let version = 0;

async function cargar() {
  const actual = ++version;
  cargando.value = true; error.value = '';
  try {
    const respuesta = await obtener_empresas();
    if (actual === version) empresas.value = respuesta.filter(e => e.activo);
  } catch (causa) {
    if (actual === version) error.value = causa && typeof causa === 'object' && 'mensaje' in causa ? String((causa as any).mensaje) : 'No se pudieron cargar las tiendas.';
  } finally { if (actual === version) cargando.value = false; }
}

async function ver_mapa(empresa: Empresa) {
  const url = url_mapa(empresa.latitud, empresa.longitud);
  console.log('[mapa] abriendo', url, 'empresa', empresa.id, empresa.nombre_empresa);
  if (!url) {
    error.value = `${empresa.nombre_empresa} todavia no tiene coordenadas cargadas.`;
    return;
  }
  await Browser.open({ url });
}

async function actualizar(event: CustomEvent) {
  try { await cargar(); }
  finally { await (event.target as HTMLIonRefresherElement).complete(); }
}
function salir() { ++version; empresas.value = []; cargando.value = false; }
onIonViewWillEnter(cargar);
onIonViewWillLeave(salir);
onBeforeUnmount(salir);
</script>

<style scoped>
/* Cards grandes: en el celular entran de a una, en tablet o PC se ponen al lado.
   auto-fill + minmax hace que bajen solas a la fila siguiente cuando no entra otra. */
.grid-empresas {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 14px;
  align-items: stretch;
}

.card-empresa {
  margin: 0;
  display: flex;
  flex-direction: column;
}

.card-cabecera {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  padding: 18px 12px 6px;
  text-align: center;
}

.card-cabecera ion-icon {
  font-size: 44px;
  color: var(--ion-color-primary);
}

.card-cabecera ion-card-title {
  font-size: 1.2rem;
}

.card-datos {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding-top: 0;
}

.dato {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  font-size: 0.95rem;
  line-height: 1.3;
}

.dato ion-icon {
  flex: 0 0 auto;
  margin-top: 2px;
  color: var(--ion-color-medium);
}

.card-acciones {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0 12px 12px;
}

.card-acciones ion-button:first-child {
  flex: 1;
  margin: 0;
}
</style>
