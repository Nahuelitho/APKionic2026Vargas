<template>
  <comp-page titulo="Usuarios" :mostrar_actualizar="true" @actualizar="actualizar">
    <ion-card v-if="global">
      <ion-card-content>
        <ion-select v-model="empresa_id" label="Ambito de membresias" label-placement="stacked">
          <ion-select-option :value="null">Global (superadmin / usuario comun)</ion-select-option>
          <ion-select-option v-for="empresa in empresas" :key="empresa.id" :value="empresa.id">{{ empresa.nombre_empresa }}</ion-select-option>
        </ion-select>
        <p>El rol se asigna solo al ambito seleccionado. Quitar una membresia no elimina las otras.</p>
      </ion-card-content>
    </ion-card>
    <ion-card>
      <ion-card-header><ion-card-title>Nuevo usuario</ion-card-title><ion-card-subtitle>{{ global ? 'Sin membresia: usuario comun' : 'Selecciona un rol de tu empresa' }}</ion-card-subtitle></ion-card-header>
      <ion-card-content>
        <ion-input v-model="nuevo.nombre" label="Nombre" label-placement="stacked" />
        <ion-input v-model="nuevo.email" type="email" label="Email" label-placement="stacked" />
        <ion-input v-model="nuevo.password" type="password" label="Contraseña" label-placement="stacked" />
        <ion-select v-model="nuevo.rol_id" label="Rol" label-placement="stacked" placeholder="Sin rol">
          <ion-select-option v-if="global" :value="null">Sin membresia</ion-select-option>
          <ion-select-option v-for="rol in roles" :key="rol.id" :value="rol.id">{{ rol.nombre }}</ion-select-option>
        </ion-select>
        <ion-text v-if="error" color="danger"><p>{{ error }}</p></ion-text>
        <ion-button expand="block" @click="crear">Crear usuario</ion-button>
      </ion-card-content>
    </ion-card>

    <ion-list>
      <ion-item v-for="item in usuarios" :key="item.id">
        <ion-label><h3>{{ item.nombre }}</h3><p>{{ item.email }}</p></ion-label>
        <ion-select :value="rol_en_ambito(item)" interface="popover" placeholder="Sin membresia" @ionChange="cambiar_rol(item.id, $event)">
          <ion-select-option v-if="global" :value="null">Sin membresia</ion-select-option>
          <ion-select-option v-for="rol in roles" :key="rol.id" :value="rol.id">{{ rol.nombre }}</ion-select-option>
        </ion-select>
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { IonButton, IonCard, IonCardContent, IonCardHeader, IonCardSubtitle, IonCardTitle, IonInput, IonItem, IonLabel, IonList, IonSelect, IonSelectOption, IonText, onIonViewWillEnter } from '@ionic/vue';
import CompPage from '../components/base/comp-page.vue';
import { asignar_rol, crear_usuario, listar_roles, listar_usuarios, type Rol, type UsuarioAdmin } from '../services/usuarios_service';
import { sesion } from '../services/auth_service';
import { obtener_empresas, type Empresa } from '../services/empresas_service';

const usuarios = ref<UsuarioAdmin[]>([]); const todos_roles = ref<Rol[]>([]); const error = ref('');
const empresas = ref<Empresa[]>([]), empresa_id = ref<number | null>(null);
const global = computed(() => sesion.usuario.value?.rol === 'superadmin');
const roles = computed(() => todos_roles.value.filter(r => global.value && empresa_id.value === null ? r.codigo === 'superadmin' : r.codigo !== 'superadmin'));
const rol_en_ambito = (item: UsuarioAdmin) => item.membresias.find(m => m.empresa_id === (global.value ? empresa_id.value : sesion.usuario.value?.empresa_id))?.rol_id ?? null;
const nuevo = reactive({ nombre: '', email: '', password: '', rol_id: null as number | null });
watch(empresa_id, () => { nuevo.rol_id = null; });
async function cargar() {
  try { [usuarios.value, todos_roles.value, empresas.value] = await Promise.all([listar_usuarios(), listar_roles(), obtener_empresas()]); }
  catch (e: any) { error.value = e.mensaje || 'No se pudieron cargar los usuarios.'; }
}
async function crear() {
  error.value = '';
  try { await crear_usuario({ ...nuevo, empresa_id: global.value ? empresa_id.value : sesion.usuario.value?.empresa_id }); Object.assign(nuevo, { nombre: '', email: '', password: '', rol_id: null }); await cargar(); }
  catch (e: any) { error.value = e.mensaje || 'No se pudo crear el usuario.'; }
}
async function cambiar_rol(id: number, event: CustomEvent) {
  try { await asignar_rol(id, event.detail.value ?? null, global.value ? empresa_id.value : sesion.usuario.value?.empresa_id); await cargar(); }
  catch (e: any) { error.value = e.mensaje || 'No se pudo asignar el rol.'; }
}
async function actualizar(event: CustomEvent) { await cargar(); (event.target as HTMLIonRefresherElement).complete(); }
onIonViewWillEnter(() => { usuarios.value = []; empresa_id.value = null; void cargar(); });
</script>
