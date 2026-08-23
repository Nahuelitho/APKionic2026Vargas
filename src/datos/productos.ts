const RETARDO_SIMULADO = 600;

const PRODUCTOS = [
  {
    id: 1,
    nombre: 'Manzanas',
    descripcion: 'Frutas frescas por kilo',
    precio: 1200,
    stock: true,
  },
  {
    id: 2,
    nombre: 'Lechuga',
    descripcion: 'Verdura de hoja fresca',
    precio: 800,
    stock: true,
  },
  {
    id: 3,
    nombre: 'Tomate',
    descripcion: 'Tomate redondo seleccionado',
    precio: 1500,
    stock: true,
  },
  {
    id: 4,
    nombre: 'Bananas',
    descripcion: 'Banana ecuatoriana por kilo',
    precio: 1800,
    stock: false,
  },
];

export function obtener_productos() {
  return new Promise((resolver) => {
    setTimeout(() => {
      resolver({ productos: PRODUCTOS });
    }, RETARDO_SIMULADO);
  });
}