const RETARDO_SIMULADO = 600;

const PEDIDOS = [
  {
    id: 1,
    codigo: 'PED-0001',
    cliente: 'Ana Sosa',
    total: 5200,
    estado: 'Listo',
    color: 'success',
  },
  {
    id: 2,
    codigo: 'PED-0002',
    cliente: 'Jorge Lucero',
    total: 8400,
    estado: 'En preparacion',
    color: 'warning',
  },
  {
    id: 3,
    codigo: 'PED-0003',
    cliente: 'Mariela Quiroga',
    total: 3100,
    estado: 'Cancelado',
    color: 'danger',
  },
];

export function obtener_pedidos() {
  return new Promise((resolver) => {
    setTimeout(() => {
      resolver({ pedidos: PEDIDOS });
    }, RETARDO_SIMULADO);
  });
}