export type Paged<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type Product = {
  id: string;
  name: string;
  sku: string;
  category: string;
  price: number;
  lowStockThreshold: number;
  rowVersion?: string;
};

export type Warehouse = {
  id: string;
  name: string;
  location: string;
  rowVersion?: string;
};

export type InventoryRow = {
  productId: string;
  productName: string;
  sku: string;
  warehouseId: string;
  warehouseName: string;
  quantity: number;
};

export type StockMovement = {
  id: string;
  productId: string;
  productName: string;
  warehouseId: string;
  warehouseName: string;
  quantityChange: number;
  reason: string;
  timestamp: string;
  movementType: number;
  purchaseOrderId?: string | null;
  salesOrderId?: string | null;
};

export type PurchaseOrder = {
  id: string;
  orderNumber: string;
  status: number;
  createdAt: string;
  completedAt?: string | null;
  lines: PurchaseOrderLine[];
};

export type PurchaseOrderLine = {
  id: string;
  productId: string;
  productName: string;
  warehouseId: string;
  warehouseName: string;
  quantity: number;
};

export type SalesOrder = {
  id: string;
  orderNumber: string;
  status: number;
  createdAt: string;
  completedAt?: string | null;
  lines: SalesOrderLine[];
};

export type SalesOrderLine = {
  id: string;
  productId: string;
  productName: string;
  warehouseId: string;
  warehouseName: string;
  quantity: number;
};

export type DashboardSummary = {
  totalProducts: number;
  totalWarehouses: number;
  lowStockProductCount: number;
  pendingPurchaseOrders: number;
  pendingSalesOrders: number;
};
