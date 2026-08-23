<template>
  <ion-list>
    <ion-item v-for="item in 6" :key="item">
      <ion-label>
        <h3>
          <ion-skeleton-text :animated="true" style="width: 60%" />
        </h3>
        <p>
          <ion-skeleton-text :animated="true" style="width: 85%" />
        </p>
        <p>
          <ion-skeleton-text :animated="true" style="width: 35%" />
        </p>
      </ion-label>
    </ion-item>
  </ion-list>
</template>

<script setup lang="ts">
import {
  IonItem,
  IonLabel,
  IonList,
  IonSkeletonText,
} from '@ionic/vue';
</script>