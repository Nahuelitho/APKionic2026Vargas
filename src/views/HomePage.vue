<template>
  <ion-page>
    <ion-header :translucent="true">
      <ion-toolbar>
        <ion-title>App Multi-tenant</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content :fullscreen="true">
      <ion-header collapse="condense">
        <ion-toolbar>
          <ion-title size="large">App Multi-tenant</ion-title>
        </ion-toolbar>
      </ion-header>

      <div id="container">
        <h1>App Multi-tenant</h1>
        <p>Vargas Nahuel</p>

        <h2>Tiendas disponibles</h2>

        <ProductCard
          v-for="store in stores"
          :key="store.id"
          :title="store.title"
          :category="store.category"
          :description="store.description"
        />
      </div>
    </ion-content>
  </ion-page>
</template>
<script setup lang="ts">
import {
  IonContent,
  IonHeader,
  IonPage,
  IonTitle,
  IonToolbar,
} from "@ionic/vue";

import ProductCard from "@/components/ProductCard.vue";

const stores = [
  {
    id: 1,
    title: "Verdulería Vargas",
    category: "Alimentos frescos",
    description: "Tienda dedicada a frutas, verduras y productos naturales.",
  },
  {
    id: 2,
    title: "Tech Store",
    category: "Tecnología",
    description: "Venta de accesorios, celulares y productos electrónicos.",
  },
  {
    id: 3,
    title: "Café Central",
    category: "Gastronomía",
    description: "Cafetería con bebidas calientes, desayunos y meriendas.",
  },
];
</script>

<!-- Css -->

<style scoped>
#container {
  padding: 24px 16px;
  text-align: center;
}

#container h1 {
  font-size: 26px;
  margin-bottom: 4px;
}

#container p {
  font-size: 16px;
  color: #8c8c8c;
  margin-top: 0;
  margin-bottom: 24px;
}

#container h2 {
  font-size: 20px;
  margin-bottom: 16px;
}
</style>
