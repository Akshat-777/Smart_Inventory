import { useEffect, useState } from "react";
import { api, getErrorMessage } from "../api/client";
import { PageHeader } from "../components/PageHeader";
import type { LowStockItem } from "../types/api";

export function LowStockPage() {
  const [items, setItems] = useState<LowStockItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const { data } = await api.get<LowStockItem[]>("/api/products/low-stock");
        if (!cancelled) setItems(data);
      } catch (e) {
        if (!cancelled) setError(getErrorMessage(e));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div>
      <PageHeader 
        title="Low Stock" 
        description="Products that are at or below their configured stock threshold." 
      />
      
      {error ? <div className="alert error">{error}</div> : null}
      {loading ? <div className="page-loading">Loading…</div> : null}

      {!loading && !error && items.length === 0 ? (
        <div className="alert">All products have sufficient stock levels!</div>
      ) : null}

      {!loading && !error && items.length > 0 ? (
        <div className="table-wrap card">
          <table className="table">
            <thead>
              <tr>
                <th>Product Name</th>
                <th>SKU</th>
                <th>Current Quantity</th>
                <th>Threshold</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.productId} className="warn-row">
                  <td>{item.productName}</td>
                  <td className="mono">{item.sku}</td>
                  <td style={{ color: "var(--danger)" }}>
                    <strong>{item.totalQuantityAcrossWarehouses}</strong>
                  </td>
                  <td className="muted">{item.lowStockThreshold}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </div>
  );
}
