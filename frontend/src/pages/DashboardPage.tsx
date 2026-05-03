import { useEffect, useState } from "react";
import { api, getErrorMessage } from "../api/client";
import { PageHeader } from "../components/PageHeader";
import type { DashboardSummary } from "../types/api";

export function DashboardPage() {
  const [data, setData] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const { data: json } = await api.get<DashboardSummary>("/api/dashboard/summary");
        if (!cancelled) setData(json);
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

  if (loading) return <div className="page-loading">Loading dashboard…</div>;
  if (error) return <div className="alert error">{error}</div>;
  if (!data) return null;

  return (
    <div>
      <PageHeader title="Dashboard" description="Live counts across catalog, locations, and open orders." />
      <div className="stat-grid">
        <div className="stat-card card stat-products">
          <div className="stat-label">Products</div>
          <div className="stat-value">{data.totalProducts}</div>
        </div>
        <div className="stat-card card stat-warehouses">
          <div className="stat-label">Warehouses</div>
          <div className="stat-value">{data.totalWarehouses}</div>
        </div>
        <div className="stat-card card stat-warn">
          <div className="stat-label">Low stock (SKUs)</div>
          <div className="stat-value">{data.lowStockProductCount}</div>
        </div>
        <div className="stat-card card stat-orders">
          <div className="stat-label">Pending purchase orders</div>
          <div className="stat-value">{data.pendingPurchaseOrders}</div>
        </div>
        <div className="stat-card card stat-orders">
          <div className="stat-label">Pending sales orders</div>
          <div className="stat-value">{data.pendingSalesOrders}</div>
        </div>
      </div>
    </div>
  );
}
