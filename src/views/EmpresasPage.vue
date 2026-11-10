<template>
  <comp-page titulo="Empresas" :mostrar_actualizar="true" @actualizar="actualizar">
    <ion-card v-if="es_superadmin">
      <ion-card-content>
        <ion-input v-model="nombre" label="Nombre de la empresa" label-placement="stacked" :maxlength="160" />
        <ion-button :disabled="guardando || !nombre.trim()" @click="crear">Crear empresa</ion-button>
      </ion-card-content>
    </ion-card>

    <ion-text v-if="error" color="danger"><p role="alert">{{ error }}</p></ion-text>

    <comp-esqueleto v-if="cargando && !empresas.length" />
    <p v-if="!cargando && !error && !empresas.length">No hay empresas para mostrar.</p>

    <ion-list v-if="empresas.length">
      <ion-item
        v-for="empresa in empresas"
        :key="empresa.id"
        button
        detail
        @click="ir_a_detalle(empresa.id)"
      >
        <ion-icon slot="start" :icon="storefrontOutline" />
        <ion-label class="ion-text-wrap">
          <h2>{{ empresa.nombre_empresa }}</h2>
          <p>{{ empresa.direccion_linea || 'Sin dirección cargada' }}</p>
          <p>
            {{ tiene_coordenadas(empresa.latitud, empresa.longitud)
              ? formatear_coordenadas(empresa.latitud, empresa.longitud)
              : 'Sin coordenadas cargadas' }}
          </p>
        </ion-label>
        <ion-button slot="end" fill="clear" size="small" @click.stop="ir_a_productos(empresa.id)">
          Productos
        </ion-button>
        <ion-badge slot="end" :color="empresa.activo ? 'success' : 'medium'">
          {{ empresa.activo ? 'Activa' : 'Inactiva' }}
        </ion-badge>
      </ion-item>
    </ion-list>

    <p>Tocá una empresa para editar sus datos y su ubicación. Usá "Productos" para ir directo al listado.</p>
  </comp-page>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import { IonBadge, IonButton, IonCard, IonCardContent, IonIcon, IonInput, IonItem, IonLabel, IonList, IonText, onIonViewWillEnter } from '@ionic/vue';
import { storefrontOutline } from 'ionicons/icons';
import CompPage from '../components/base/comp-page.vue';
import CompEsqueleto from '../components/base/comp-esqueleto.vue';
import { crear_empresa, obtener_empresas, type Empresa } from '../services/empresas_service';
import { formatear_coordenadas, tiene_coordenadas } from '../services/geolocalizacion_service';
import { sesion } from '../services/auth_service';

const router = useRouter();

const empresas = ref<Empresa[]>([]), nombre = ref(''), error = ref('');
const cargando = ref(false), guardando = ref(false);
const es_superadmin = computed(() => sesion.usuario.value?.rol === 'superadmin');
const mensaje = (e: unknown) => e && typeof e === 'object' && 'mensaje' in e ? String((e as { mensaje: unknown }).mensaje) : 'No se pudo completar la operación.';

function ir_a_detalle(id: number) {
  router.push(`/app/empresas/${id}`);
}

function ir_a_productos(id: number) {
  router.push(`/app/empresas/${id}/productos`);
}

async function cargar() {
  cargando.value = true; error.value = '';
  try { empresas.value = await obtener_empresas(); }
  catch (e) { error.value = mensaje(e); }
  finally { cargando.value = false; }
}

async function crear() {
  guardando.value = true; error.value = '';
  try { await crear_empresa(nombre.value.trim()); nombre.value = ''; await cargar(); }
  catch (e) { error.value = mensaje(e); } finally { guardando.value = false; }
}

async function actualizar(event: CustomEvent) {
  try { await cargar(); }
  finally { await (event.target as HTMLIonRefresherElement).complete(); }
}
onIonViewWillEnter(cargar);
</script>