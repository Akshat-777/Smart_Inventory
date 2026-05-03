import { useCallback, useEffect, useState } from "react";
import { api, getErrorMessage } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { PageHeader } from "../components/PageHeader";
import type { Paged, Product, PurchaseOrder, SalesOrder, Warehouse } from "../types/api";

function statusLabel(s: number) {
  const map: Record<number, string> = { 0: "Draft", 1: "Pending", 2: "Completed", 3: "Cancelled" };
  return map[s] ?? String(s);
}

function statusBadgeClass(s: number): string {
  if (s === 0) return "badge badge-draft";
  if (s === 1) return "badge badge-pending";
  if (s === 2) return "badge badge-completed";
  if (s === 3) return "badge badge-cancelled";
  return "badge badge-draft";
}

export function OrdersPage() {
  const { canEdit } = useAuth();
  const [tab, setTab] = useState<"purchase" | "sales">("purchase");
  const [purchase, setPurchase] = useState<PurchaseOrder[]>([]);
  const [sales, setSales] = useState<SalesOrder[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [products, setProducts] = useState<Product[]>([]);
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);

  const [poLines, setPoLines] = useState<{ productId: string; warehouseId: string; quantity: string }[]>([
    { productId: "", warehouseId: "", quantity: "1" }
  ]);
  const [soLines, setSoLines] = useState<{ productId: string; warehouseId: string; quantity: string }[]>([
    { productId: "", warehouseId: "", quantity: "1" }
  ]);

  const loadRefs = useCallback(async () => {
    try {
      const [pr, wh] = await Promise.all([
        api.get<Paged<Product>>("/api/products", { params: { page: 1, pageSize: 500 } }),
        api.get<Paged<Warehouse>>("/api/warehouses", { params: { page: 1, pageSize: 500 } })
      ]);
      setProducts(pr.data.items);
      setWarehouses(wh.data.items);
    } catch {
      /* ignore */
    }
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      if (tab === "purchase") {
        const { data } = await api.get<Paged<PurchaseOrder>>("/api/orders/purchase", {
          params: { page, pageSize }
        });
        setPurchase(data.items);
        setTotalPages(data.totalPages || 1);
      } else {
        const { data } = await api.get<Paged<SalesOrder>>("/api/orders/sales", {
          params: { page, pageSize }
        });
        setSales(data.items);
        setTotalPages(data.totalPages || 1);
      }
    } catch (e) {
      setError(getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, tab]);

  useEffect(() => {
    void loadRefs();
  }, [loadRefs]);

  useEffect(() => {
    void load();
  }, [load]);

  async function createPurchase(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    const lines = poLines
      .filter((l) => l.productId && l.warehouseId)
      .map((l) => ({
        productId: l.productId,
        warehouseId: l.warehouseId,
        quantity: Number(l.quantity)
      }));
    if (lines.length === 0) {
      setError("Add at least one valid line.");
      return;
    }
    try {
      await api.post("/api/orders/purchase", { lines });
      setPoLines([{ productId: "", warehouseId: "", quantity: "1" }]);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function createSales(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    const lines = soLines
      .filter((l) => l.productId && l.warehouseId)
      .map((l) => ({
        productId: l.productId,
        warehouseId: l.warehouseId,
        quantity: Number(l.quantity)
      }));
    if (lines.length === 0) {
      setError("Add at least one valid line.");
      return;
    }
    try {
      await api.post("/api/orders/sales", { lines });
      setSoLines([{ productId: "", warehouseId: "", quantity: "1" }]);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function completePurchase(id: string) {
    setError(null);
    try {
      await api.post(`/api/orders/purchase/${id}/complete`);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function fulfillSales(id: string) {
    setError(null);
    try {
      await api.post(`/api/orders/sales/${id}/fulfill`);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  return (
    <div>
      <PageHeader
        title="Orders"
        description="Raise purchase orders to receive stock and sales orders to ship—from draft through fulfillment."
      />
      <div className="tabs">
        <button type="button" className={tab === "purchase" ? "tab active" : "tab"} onClick={() => { setTab("purchase"); setPage(1); }}>
          Purchase orders
        </button>
        <button type="button" className={tab === "sales" ? "tab active" : "tab"} onClick={() => { setTab("sales"); setPage(1); }}>
          Sales orders
        </button>
      </div>
      {error ? <div className="alert error">{error}</div> : null}
      {loading ? <div className="page-loading">Loading…</div> : null}

      {canEdit && tab === "purchase" ? (
        <form className="card form-card" onSubmit={createPurchase}>
          <h2>Create purchase order</h2>
          {poLines.map((line, idx) => (
            <div key={idx} className="line-row">
              <select
                value={line.productId}
                onChange={(e) => {
                  const next = [...poLines];
                  next[idx] = { ...next[idx], productId: e.target.value };
                  setPoLines(next);
                }}
                required
              >
                <option value="">Product</option>
                {products.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </select>
              <select
                value={line.warehouseId}
                onChange={(e) => {
                  const next = [...poLines];
                  next[idx] = { ...next[idx], warehouseId: e.target.value };
                  setPoLines(next);
                }}
                required
              >
                <option value="">Warehouse</option>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name}
                  </option>
                ))}
              </select>
              <input
                type="number"
                min={1}
                value={line.quantity}
                onChange={(e) => {
                  const next = [...poLines];
                  next[idx] = { ...next[idx], quantity: e.target.value };
                  setPoLines(next);
                }}
              />
            </div>
          ))}
          <div className="form-actions">
            <button type="button" className="btn ghost" onClick={() => setPoLines([...poLines, { productId: "", warehouseId: "", quantity: "1" }])}>
              Add line
            </button>
            <button className="btn primary" type="submit">
              Create PO
            </button>
          </div>
        </form>
      ) : null}

      {canEdit && tab === "sales" ? (
        <form className="card form-card" onSubmit={createSales}>
          <h2>Create sales order</h2>
          {soLines.map((line, idx) => (
            <div key={idx} className="line-row">
              <select
                value={line.productId}
                onChange={(e) => {
                  const next = [...soLines];
                  next[idx] = { ...next[idx], productId: e.target.value };
                  setSoLines(next);
                }}
                required
              >
                <option value="">Product</option>
                {products.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </select>
              <select
                value={line.warehouseId}
                onChange={(e) => {
                  const next = [...soLines];
                  next[idx] = { ...next[idx], warehouseId: e.target.value };
                  setSoLines(next);
                }}
                required
              >
                <option value="">Warehouse</option>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name}
                  </option>
                ))}
              </select>
              <input
                type="number"
                min={1}
                value={line.quantity}
                onChange={(e) => {
                  const next = [...soLines];
                  next[idx] = { ...next[idx], quantity: e.target.value };
                  setSoLines(next);
                }}
              />
            </div>
          ))}
          <div className="form-actions">
            <button type="button" className="btn ghost" onClick={() => setSoLines([...soLines, { productId: "", warehouseId: "", quantity: "1" }])}>
              Add line
            </button>
            <button className="btn primary" type="submit">
              Create SO
            </button>
          </div>
        </form>
      ) : null}

      {tab === "purchase" ? (
        <div className="table-wrap card">
          <table className="table">
            <thead>
              <tr>
                <th>Order</th>
                <th>Status</th>
                <th>Created</th>
                <th>Lines</th>
                {canEdit ? <th /> : null}
              </tr>
            </thead>
            <tbody>
              {purchase.map((o) => (
                <tr key={o.id}>
                  <td>{o.orderNumber}</td>
                  <td>
                    <span className={statusBadgeClass(o.status)}>{statusLabel(o.status)}</span>
                  </td>
                  <td>{new Date(o.createdAt).toLocaleString()}</td>
                  <td>{o.lines.length}</td>
                  {canEdit ? (
                    <td className="actions">
                      <button
                        type="button"
                        className="btn primary small"
                        disabled={o.status !== 1}
                        onClick={() => void completePurchase(o.id)}
                      >
                        Receive / complete
                      </button>
                    </td>
                  ) : null}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="table-wrap card">
          <table className="table">
            <thead>
              <tr>
                <th>Order</th>
                <th>Status</th>
                <th>Created</th>
                <th>Lines</th>
                {canEdit ? <th /> : null}
              </tr>
            </thead>
            <tbody>
              {sales.map((o) => (
                <tr key={o.id}>
                  <td>{o.orderNumber}</td>
                  <td>
                    <span className={statusBadgeClass(o.status)}>{statusLabel(o.status)}</span>
                  </td>
                  <td>{new Date(o.createdAt).toLocaleString()}</td>
                  <td>{o.lines.length}</td>
                  {canEdit ? (
                    <td className="actions">
                      <button
                        type="button"
                        className="btn primary small"
                        disabled={o.status !== 1}
                        onClick={() => void fulfillSales(o.id)}
                      >
                        Fulfill
                      </button>
                    </td>
                  ) : null}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

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
