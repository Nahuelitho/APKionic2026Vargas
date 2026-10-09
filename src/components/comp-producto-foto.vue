<template>
  <img v-if="imagen" :src="imagen" :alt="alt" loading="lazy" />
</template>

<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue';
import { sesion } from '../services/auth_service';
import { obtener_foto_producto } from '../services/productos_service';

const props = defineProps<{ fotoUrl: string; alt: string }>();
const imagen = ref('');
let version = 0;
function limpiar() { if (imagen.value) URL.revokeObjectURL(imagen.value); imagen.value = ''; }
watch(() => `${props.fotoUrl}:${sesion.usuario.value?.id}:${sesion.usuario.value?.rol}:${sesion.usuario.value?.empresa_id}`, async () => {
  const actual = ++version;
  limpiar();
  if (!props.fotoUrl || !sesion.usuario.value) return;
  try {
    const foto = await obtener_foto_producto(props.fotoUrl);
    if (actual === version) imagen.value = URL.createObjectURL(foto);
  } catch { /* A removed or inaccessible photo must not keep the previous image. */ }
}, { immediate: true });
onBeforeUnmount(() => { ++version; limpiar(); });
</script>
