<template>
  <section class="lista-seccion">
    <header v-if="titulo || $slots.cabecera" class="lista-cabecera">
      <slot name="cabecera">
        <div>
          <h2 class="app-titulo-seccion">{{ titulo }}</h2>
          <p v-if="subtitulo" class="app-texto-suave">{{ subtitulo }}</p>
        </div>
        <slot name="acciones" />
      </slot>
    </header>

    <!-- Mientras carga y no hay datos se muestra el esqueleto, nunca un vacio. -->
    <comp-esqueleto v-if="cargando && !items" :filas="filas_esqueleto" />

    <comp-aviso
      v-else-if="error"
      tipo="peligro"
      titulo="No se pudo cargar"
      :mensaje="error"
      :accion="reintentar ? 'Reintentar' : ''"
      @accion="emit('reintentar')"
    />

    <comp-aviso
      v-else-if="!items"
      :tipo="vacio_tipo"
      :icono="vacio_icono"
      :mensaje="texto_vacio"
    />

    <slot v-else />

    <!-- Si hay datos viejos y la recarga falla, el aviso no tapa la lista. -->
    <comp-aviso v-if="error && items" tipo="advertencia" :mensaje="error" />
  </section>
</template>

<script setup lang="ts">
import CompEsqueleto from './comp-esqueleto.vue';
import CompAviso from './comp-aviso.vue';

/**
 * Contenedor de lista con los cuatro estados de toda pantalla:
 * cargando, error, vacio y con datos. Evita repetir la misma logica.
 */
const props = defineProps({
  titulo: { type: String, default: '' },
  subtitulo: { type: String, default: '' },
  cargando: { type: Boolean, default: false },
  error: { type: String, default: '' },
  items: { type: Number, default: 0 },
  texto_vacio: { type: String, default: 'No hay datos para mostrar.' },
  vacio_tipo: { type: String, default: 'info' },
  vacio_icono: { type: String, default: '' },
  reintentar: { type: Boolean, default: true },
  filas_esqueleto: { type: Number, default: 5 },
});

const emit = defineEmits(['reintentar']);
void props;
</script>

<style scoped>
.lista-seccion {
  display: flex;
  flex-direction: column;
  gap: var(--app-espacio-3);
}

.lista-cabecera {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: var(--app-espacio-3);
  flex-wrap: wrap;
}

.lista-cabecera p { margin: 2px 0 0; }
</style>
