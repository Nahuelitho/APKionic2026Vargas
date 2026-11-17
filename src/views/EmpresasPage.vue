<template>
  <comp-page titulo="Empresas" :mostrar_actualizar="true" @actualizar="actualizar">
    <section v-if="es_superadmin" class="crear app-tarjeta">
      <h2 class="app-titulo-seccion">Nueva empresa</h2>
      <p class="app-texto-suave">El nombre es obligatorio y admite hasta 160 caracteres.</p>
      <ion-input
        v-model="nombre"
        label="Nombre de la empresa"
        label-placement="stacked"
        :maxlength="160"
        helper-text="Obligatorio: identifica la tienda en toda la app."
      />
      <div class="app-acciones">
        <ion-button :disabled="guardando || !nombre.trim()" @click="crear">
          {{ guardando ? 'Creando...' : 'Crear empresa' }}
        </ion-button>
      </div>
      <comp-aviso v-if="error_crear" tipo="peligro" :mensaje="error_crear" />
    </section>

    <comp-lista
      titulo="Tus empresas"
      subtitulo="Tocá una empresa para editar sus datos y su ubicación."
      :cargando="cargando"
      :error="error"
      :items="empresas.length"
      texto_vacio="No hay empresas para mostrar."
      @reintentar="cargar"
    >
      <ion-list class="lista-empresas">
        <ion-item v-for="empresa in empresas" :key="empresa.id" button detail @click="ir_a_detalle(empresa.id)">
          <ion-icon slot="start" :icon="storefrontOutline" class="icono-empresa" />
          <ion-label class="ion-text-wrap">
            <div class="empresa-titulo">
              <h2>{{ empresa.nombre_empresa }}</h2>
              <comp-estado
                :definicion="empresa.activo ? ESTADOS_ACTIVO.activo : ESTADOS_ACTIVO.inactivo"
              />
            </div>
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
        </ion-item>
      </ion-list>
    </comp-lista>
  </comp-page>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import { IonButton, IonIcon, IonInput, IonItem, IonLabel, IonList, onIonViewWillEnter } from '@ionic/vue';
import { storefrontOutline } from 'ionicons/icons';
import CompPage from '../components/base/comp-page.vue';
import CompLista from '../components/base/comp-lista.vue';
import CompAviso from '../components/base/comp-aviso.vue';
import CompEstado from '../components/base/comp-estado.vue';
import { ESTADOS_ACTIVO } from '../config/estados';
import { crear_empresa, obtener_empresas, type Empresa } from '../services/empresas_service';
import { formatear_coordenadas, tiene_coordenadas } from '../services/geolocalizacion_service';
import { sesion } from '../services/auth_service';

const router = useRouter();

const empresas = ref<Empresa[]>([]), nombre = ref(''), error = ref('');
const cargando = ref(false), guardando = ref(false), error_crear = ref('');
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
  error_crear.value = '';
  const limpio = nombre.value.trim();
  if (!limpio) { error_crear.value = 'El nombre de la empresa es obligatorio.'; return; }
  guardando.value = true;
  try { await crear_empresa(limpio); nombre.value = ''; await cargar(); }
  catch (e) { error_crear.value = mensaje(e); } finally { guardando.value = false; }
}

async function actualizar(event: CustomEvent) {
  try { await cargar(); }
  finally { await (event.target as HTMLIonRefresherElement).complete(); }
}
onIonViewWillEnter(cargar);
</script>

<style scoped>
.crear {
  display: flex;
  flex-direction: column;
  gap: var(--app-espacio-2);
  padding: var(--app-espacio-4);
}

.crear h2 { margin: 0; }
.crear p { margin: 0; }

.lista-empresas {
  margin: 0;
  border-radius: var(--app-radio-md);
  overflow: hidden;
  border: 1px solid var(--app-borde);
  box-shadow: var(--app-sombra-1);
}

.icono-empresa { color: var(--ion-color-primary); font-size: 24px; }

.empresa-titulo {
  display: flex;
  align-items: center;
  gap: var(--app-espacio-2);
  flex-wrap: wrap;
}

.empresa-titulo h2 { margin: 0; font-size: var(--app-texto-md); font-weight: 700; }
</style>
