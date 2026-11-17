<template>
  <span class="estado" :class="`tono-${definicion.tono}`" :title="definicion.descripcion">
    <ion-icon :icon="definicion.icono" aria-hidden="true" />
    <span>{{ definicion.texto }}</span>
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { IonIcon } from '@ionic/vue';
import { definir_estado_pedido, ESTADO_SIN_ENVIAR, type DefinicionEstado } from '../../config/estados';

/**
 * Chip de estado. Siempre muestra texto + icono + color, nunca solo color.
 * Sin prop `definicion` resuelve el estado de pedido por nombre.
 */
const props = defineProps({
  estado: { type: String, default: '' },
  definicion: { type: Object as () => DefinicionEstado | null, default: null },
  sin_enviar: { type: Boolean, default: false },
});

const definicion = computed<DefinicionEstado>(() =>
  props.sin_enviar ? ESTADO_SIN_ENVIAR : props.definicion || definir_estado_pedido(props.estado),
);
</script>

<style scoped>
.estado {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 3px 10px;
  border-radius: var(--app-radio-pill);
  font-size: var(--app-texto-xs);
  font-weight: 700;
  line-height: 1.5;
  white-space: nowrap;
}

.estado ion-icon {
  font-size: 14px;
}

.tono-exito { color: var(--app-estado-exito-texto); background: var(--app-estado-exito-fondo); }
.tono-aviso { color: var(--app-estado-aviso-texto); background: var(--app-estado-aviso-fondo); }
.tono-peligro { color: var(--app-estado-peligro-texto); background: var(--app-estado-peligro-fondo); }
.tono-neutral { color: var(--app-estado-neutral-texto); background: var(--app-estado-neutral-fondo); }
.tono-info { color: var(--app-estado-info-texto); background: var(--app-estado-info-fondo); }
</style>
