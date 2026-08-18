<template>
  <ion-card>
    <ion-card-header>
      <ion-card-title>{{ title }}</ion-card-title>
      <ion-card-subtitle>{{ category }}</ion-card-subtitle>
    </ion-card-header>

    <ion-card-content>
      {{ description }}
    </ion-card-content>
  </ion-card>
</template>

<script setup lang="ts">
import {
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardSubtitle,
  IonCardContent,
} from "@ionic/vue";

defineProps<{
  title: string;
  category: string;
  description: string;
}>();
</script>