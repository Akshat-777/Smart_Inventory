import { useCallback, useEffect, useState } from "react";
import { api, getErrorMessage } from "../api/client";
import { PageHeader } from "../components/PageHeader";
import type { InventoryRow, Paged, Product, Warehouse } from "../types/api";

export function InventoryPage() {
  const [rows, setRows] = useState<InventoryRow[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(15);
  const [totalPages, setTotalPages] = useState(1);
  const [search, setSearch] = useState("");
  const [productId, setProductId] = useState<string>("");
  const [warehouseId, setWarehouseId] = useState<string>("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadFilters = useCallback(async () => {
    try {
      const [pr, wh] = await Promise.all([
        api.get<Paged<Product>>("/api/products", { params: { page: 1, pageSize: 500 } }),
        api.get<Paged<Warehouse>>("/api/warehouses", { params: { page: 1, pageSize: 500 } })
      ]);
      setProducts(pr.data.items);
      setWarehouses(wh.data.items);
    } catch {
      /* ignore filter load errors */
    }
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const { data } = await api.get<Paged<InventoryRow>>("/api/inventory", {
        params: {
          page,
          pageSize,
          search: search || undefined,
          productId: productId || undefined,
          warehouseId: warehouseId || undefined
        }
      });
      setRows(data.items);
      setTotalPages(data.totalPages || 1);
    } catch (e) {
      setError(getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, search, productId, warehouseId]);

  useEffect(() => {
    void loadFilters();
  }, [loadFilters]);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <div>
      <PageHeader
        title="Inventory"
        description="On-hand quantities by product and warehouse—filter or search to drill in."
      />
      <div className="toolbar filters">
        <input
          className="search"
          placeholder="Search product or warehouse…"
          value={search}
          onChange={(e) => {
            setPage(1);
            setSearch(e.target.value);
          }}
        />
        <select value={productId} onChange={(e) => { setPage(1); setProductId(e.target.value); }}>
          <option value="">All products</option>
          {products.map((p) => (
            <option key={p.id} value={p.id}>
              {p.name}
            </option>
          ))}
        </select>
        <select value={warehouseId} onChange={(e) => { setPage(1); setWarehouseId(e.target.value); }}>
          <option value="">All warehouses</option>
          {warehouses.map((w) => (
            <option key={w.id} value={w.id}>
              {w.name}
            </option>
          ))}
        </select>
      </div>
      {error ? <div className="alert error">{error}</div> : null}
      {loading ? <div className="page-loading">Loading…</div> : null}
      <div className="table-wrap card">
        <table className="table">
          <thead>
            <tr>
              <th>Product</th>
              <th>SKU</th>
              <th>Warehouse</th>
              <th>Quantity</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={`${r.productId}-${r.warehouseId}`}>
                <td>{r.productName}</td>
                <td className="mono">{r.sku}</td>
                <td>{r.warehouseName}</td>
                <td>{r.quantity}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="pager">
        <button type="button" className="btn ghost" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
          Prev
        </button>
        <span className="muted">
          Page {page} / {totalPages}
        </span>
        <button
          type="button"
          className="btn ghost"
          disabled={page >= totalPages}
          onClick={() => setPage((p) => p + 1)}
        >
          Next
        </button>
      </div>
    </div>
  );
}
