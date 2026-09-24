<template>
  <comp-page titulo="Usuarios" :mostrar_actualizar="true" @actualizar="actualizar">
    <ion-card>
      <ion-card-header><ion-card-title>Nuevo usuario</ion-card-title><ion-card-subtitle>El rol puede quedar pendiente</ion-card-subtitle></ion-card-header>
      <ion-card-content>
        <ion-input v-model="nuevo.nombre" label="Nombre" label-placement="stacked" />
        <ion-input v-model="nuevo.email" type="email" label="Email" label-placement="stacked" />
        <ion-input v-model="nuevo.password" type="password" label="Contraseña" label-placement="stacked" />
        <ion-select v-model="nuevo.rol_id" label="Rol" label-placement="stacked" placeholder="Sin rol">
          <ion-select-option :value="null">Sin rol</ion-select-option>
          <ion-select-option v-for="rol in roles" :key="rol.id" :value="rol.id">{{ rol.nombre }}</ion-select-option>
        </ion-select>
        <ion-text v-if="error" color="danger"><p>{{ error }}</p></ion-text>
        <ion-button expand="block" @click="crear">Crear usuario</ion-button>
      </ion-card-content>
    </ion-card>

    <ion-list>
      <ion-item v-for="item in usuarios" :key="item.id">
        <ion-label><h3>{{ item.nombre }}</h3><p>{{ item.email }}</p></ion-label>
        <ion-select :value="item.rol_id" interface="popover" placeholder="Sin rol" @ionChange="cambiar_rol(item.id, $event)">
          <ion-select-option :value="null">Sin rol</ion-select-option>
          <ion-select-option v-for="rol in roles" :key="rol.id" :value="rol.id">{{ rol.nombre }}</ion-select-option>
        </ion-select>
      </ion-item>
    </ion-list>
  </comp-page>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { IonButton, IonCard, IonCardContent, IonCardHeader, IonCardSubtitle, IonCardTitle, IonInput, IonItem, IonLabel, IonList, IonSelect, IonSelectOption, IonText } from '@ionic/vue';
import CompPage from '../components/base/comp-page.vue';
import { asignar_rol, crear_usuario, listar_roles, listar_usuarios, type Rol, type UsuarioAdmin } from '../services/usuarios_service';

const usuarios = ref<UsuarioAdmin[]>([]); const roles = ref<Rol[]>([]); const error = ref('');
const nuevo = reactive({ nombre: '', email: '', password: '', rol_id: null as number | null });
async function cargar() {
  try { [usuarios.value, roles.value] = await Promise.all([listar_usuarios(), listar_roles()]); }
  catch (e: any) { error.value = e.mensaje || 'No se pudieron cargar los usuarios.'; }
}
async function crear() {
  error.value = '';
  try { await crear_usuario(nuevo); Object.assign(nuevo, { nombre: '', email: '', password: '', rol_id: null }); await cargar(); }
  catch (e: any) { error.value = e.mensaje || 'No se pudo crear el usuario.'; }
}
async function cambiar_rol(id: number, event: CustomEvent) {
  try { await asignar_rol(id, event.detail.value ?? null); await cargar(); }
  catch (e: any) { error.value = e.mensaje || 'No se pudo asignar el rol.'; }
}
async function actualizar(event: CustomEvent) { await cargar(); (event.target as HTMLIonRefresherElement).complete(); }
onMounted(cargar);
</script>
