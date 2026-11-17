<template>
  <ion-list class="lista" :class="{ 'lista-inset': inset }">
    <ion-item v-for="item in filas" :key="item">
      <ion-label>
        <h3>
          <ion-skeleton-text :animated="true" style="width: 55%" />
        </h3>
        <p>
          <ion-skeleton-text :animated="true" style="width: 85%" />
        </p>
        <p v-if="variante === 'detalle'">
          <ion-skeleton-text :animated="true" style="width: 65%" />
        </p>
        <p>
          <ion-skeleton-text :animated="true" style="width: 30%" />
        </p>
      </ion-label>
      <ion-badge slot="end" color="light">
        <ion-skeleton-text :animated="true" style="width: 36px" />
      </ion-badge>
    </ion-item>
  </ion-list>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import {
  IonBadge,
  IonItem,
  IonLabel,
  IonList,
  IonSkeletonText,
} from '@ionic/vue';

/**
 * Esqueleto de lista. `filas` controla cuantas tarjetas fantasma se ven
 * y `variante` si imita una fila simple o un detalle con mas lineas.
 */
const props = defineProps({
  filas: { type: Number, default: 5 },
  variante: { type: String, default: 'lista' }, // lista | detalle
  inset: { type: Boolean, default: true },
});

const filas = computed(() => Array.from({ length: Math.max(1, props.filas) }, (_, i) => i));
</script>

<style scoped>
.lista { margin: 0; }
.lista-inset { --ion-item-background: transparent; }
ion-badge { --padding-start: 6px; --padding-end: 6px; }
</style>
