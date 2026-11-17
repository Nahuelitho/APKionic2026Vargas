<template>
  <div class="aviso" :class="`tipo-${tipo}`" :role="tipo === 'peligro' ? 'alert' : 'status'">
    <ion-icon class="aviso-icono" :icon="icono_efectivo" aria-hidden="true" />
    <div class="aviso-texto">
      <strong v-if="titulo">{{ titulo }}</strong>
      <p>{{ mensaje }}</p>
      <slot />
    </div>
    <ion-button
      v-if="accion"
      class="aviso-accion"
      fill="clear"
      size="small"
      :disabled="accion_deshabilitada"
      @click="emit('accion')"
    >
      {{ accion }}
    </ion-button>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { IonButton, IonIcon } from '@ionic/vue';
import {
  alertCircleOutline,
  checkmarkCircleOutline,
  informationCircleOutline,
  warningOutline,
  cloudOfflineOutline,
} from 'ionicons/icons';

/**
 * Aviso reutilizable (banner/nota). Reemplaza los <ion-text color="danger">
 * sueltos: mismo margen, mismo icono y un mensaje legible.
 */
const props = defineProps({
  tipo: { type: String, default: 'info' }, // info | exito | advertencia | peligro | sin-red
  titulo: { type: String, default: '' },
  mensaje: { type: String, required: true },
  accion: { type: String, default: '' },
  accion_deshabilitada: { type: Boolean, default: false },
  icono: { type: String, default: '' },
});

const emit = defineEmits(['accion']);

const icono_efectivo = computed(() => props.icono || ({
  info: informationCircleOutline,
  exito: checkmarkCircleOutline,
  advertencia: warningOutline,
  peligro: alertCircleOutline,
  'sin-red': cloudOfflineOutline,
}[props.tipo] || informationCircleOutline));
</script>

<style scoped>
.aviso {
  display: flex;
  align-items: flex-start;
  gap: var(--app-espacio-2);
  padding: var(--app-espacio-3);
  border-radius: var(--app-radio-sm);
  border: 1px solid transparent;
  font-size: var(--app-texto-sm);
  line-height: 1.35;
}

.aviso-icono {
  flex: 0 0 auto;
  margin-top: 1px;
  font-size: 20px;
}

.aviso-texto { flex: 1; min-width: 0; }
.aviso-texto strong { display: block; margin-bottom: 2px; font-size: var(--app-texto-sm); }
.aviso-texto p { margin: 0; }

.aviso-accion { margin: -6px -8px -6px 0; }

.tipo-info { color: var(--app-estado-info-texto); background: var(--app-estado-info-fondo); border-color: color-mix(in srgb, var(--app-estado-info-texto) 25%, transparent); }
.tipo-exito { color: var(--app-estado-exito-texto); background: var(--app-estado-exito-fondo); border-color: color-mix(in srgb, var(--app-estado-exito-texto) 25%, transparent); }
.tipo-advertencia,
.tipo-sin-red { color: var(--app-estado-aviso-texto); background: var(--app-estado-aviso-fondo); border-color: color-mix(in srgb, var(--app-estado-aviso-texto) 25%, transparent); }
.tipo-peligro { color: var(--app-estado-peligro-texto); background: var(--app-estado-peligro-fondo); border-color: color-mix(in srgb, var(--app-estado-peligro-texto) 25%, transparent); }
</style>
