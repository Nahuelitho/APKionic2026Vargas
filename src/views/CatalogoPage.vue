<template>
  <comp-page :titulo="empresa ? `Catalogo: ${empresa.nombre_empresa}` : 'Catalogo'">
    <ion-button fill="outline" router-link="/app/comprar">Volver a elegir empresa</ion-button>
    <p v-if="cargando">Validando empresa...</p>
    <ion-text v-if="error" color="danger"><p role="alert">{{ error }}</p></ion-text>
    <ion-button v-if="error" fill="outline" @click="cargar_empresa">Reintentar</ion-button>
    <template v-if="activo && empresa">
      <h1>{{ empresa.nombre_empresa }}</h1>
      <comp-pedido-formulario :key="empresa.id" :empresa="empresa" @creado="pedido_creado" />
    </template>
  </comp-page>
</template>

<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { IonButton, IonText, onIonViewWillEnter, onIonViewWillLeave } from '@ionic/vue';
import CompPage from '../components/base/comp-page.vue';
import CompPedidoFormulario from '../components/comp-pedido-formulario.vue';
import { obtener_empresas, type Empresa } from '../services/empresas_service';

const route = useRoute(), router = useRouter();
const activo = ref(false), cargando = ref(false), error = ref('');
const empresa = ref<Empresa | null>(null);
let version = 0;
async function cargar_empresa() {
  const actual = ++version;
  empresa.value = null; error.value = ''; cargando.value = true;
  try {
    const parametro = route.params.empresaId;
    const id = typeof parametro === 'string' && /^[1-9]\d*$/.test(parametro) ? Number(parametro) : NaN;
    if (!Number.isSafeInteger(id)) throw new Error('Empresa invalida. Vuelve a elegir una empresa.');
    const empresas = await obtener_empresas();
    if (!activo.value || actual !== version) return;
    const elegida = empresas.find(e => e.id === id && e.activo);
    if (!elegida) throw new Error('La empresa no existe o no esta activa. Vuelve a elegir una empresa.');
    empresa.value = elegida;
  } catch (causa) {
    if (!activo.value || actual !== version) return;
    error.value = causa && typeof causa === 'object' && 'mensaje' in causa ? String(causa.mensaje) : causa instanceof Error ? causa.message : 'No se pudo validar la empresa.';
  } finally { if (activo.value && actual === version) cargando.value = false; }
}
function pedido_creado(id: number) { void router.push({ path: '/app/pedidos', query: { pedido: String(id) } }); }
function salir() { activo.value = false; ++version; empresa.value = null; cargando.value = false; }
watch(() => route.params.empresaId, () => { if (activo.value) void cargar_empresa(); });
onIonViewWillEnter(() => { activo.value = true; void cargar_empresa(); });
onIonViewWillLeave(salir);
onBeforeUnmount(salir);
</script>
