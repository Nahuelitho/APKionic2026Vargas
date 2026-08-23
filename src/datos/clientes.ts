const RETARDO_SIMULADO = 600;

const CLIENTES = [
  {
    id: 1,
    nombre: 'Ana Sosa',
    telefono: '2664-111111',
    direccion: 'San Luis Capital',
  },
  {
    id: 2,
    nombre: 'Jorge Lucero',
    telefono: '2664-222222',
    direccion: 'La Punta',
  },
  {
    id: 3,
    nombre: 'Mariela Quiroga',
    telefono: '2664-333333',
    direccion: 'Juana Koslay',
  },
];

export function obtener_clientes() {
  return new Promise((resolver) => {
    setTimeout(() => {
      resolver({ clientes: CLIENTES });
    }, RETARDO_SIMULADO);
  });
}