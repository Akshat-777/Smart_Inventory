import { useCallback, useEffect, useState } from "react";
import { api, getErrorMessage } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { PageHeader } from "../components/PageHeader";
import type { Paged, Product } from "../types/api";

const emptyForm = {
  name: "",
  sku: "",
  category: "",
  price: "0",
  lowStockThreshold: "10"
};

export function ProductsPage() {
  const { canEdit } = useAuth();
  const [rows, setRows] = useState<Product[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [editing, setEditing] = useState<Product | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const { data } = await api.get<Paged<Product>>("/api/products", {
        params: { page, pageSize, search: search || undefined }
      });
      setRows(data.items);
      setTotalPages(data.totalPages || 1);
    } catch (e) {
      setError(getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, search]);

  useEffect(() => {
    void load();
  }, [load]);

  async function createProduct(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await api.post("/api/products", {
        name: form.name,
        sku: form.sku,
        category: form.category,
        price: Number(form.price),
        lowStockThreshold: Number(form.lowStockThreshold)
      });
      setForm(emptyForm);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function saveEdit(e: React.FormEvent) {
    e.preventDefault();
    if (!editing) return;
    setError(null);
    try {
      await api.put(`/api/products/${editing.id}`, {
        name: form.name,
        sku: form.sku,
        category: form.category,
        price: Number(form.price),
        lowStockThreshold: Number(form.lowStockThreshold),
        rowVersion: editing.rowVersion
      });
      setEditing(null);
      setForm(emptyForm);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  async function remove(id: string) {
    if (!confirm("Delete this product?")) return;
    setError(null);
    try {
      await api.delete(`/api/products/${id}`);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  function startEdit(p: Product) {
    setEditing(p);
    setForm({
      name: p.name,
      sku: p.sku,
      category: p.category,
      price: String(p.price),
      lowStockThreshold: String(p.lowStockThreshold)
    });
  }

  return (
    <div>
      <PageHeader title="Products" description="Define SKUs, pricing, and low-stock thresholds." />
      <div className="toolbar">
        <input
          className="search"
          placeholder="Search name, SKU, category…"
          value={search}
          onChange={(e) => {
            setPage(1);
            setSearch(e.target.value);
          }}
        />
      </div>
      {error ? <div className="alert error">{error}</div> : null}
      {loading ? <div className="page-loading">Loading…</div> : null}

      <div className="table-wrap card">
        <table className="table">
          <thead>
            <tr>
              <th>Name</th>
              <th>SKU</th>
              <th>Category</th>
              <th>Price</th>
              <th>Low stock threshold</th>
              {canEdit ? <th /> : null}
            </tr>
          </thead>
          <tbody>
            {rows.map((p) => (
              <tr key={p.id}>
                <td>{p.name}</td>
                <td className="mono">{p.sku}</td>
                <td>{p.category}</td>
                <td>{p.price.toFixed(2)}</td>
                <td>{p.lowStockThreshold}</td>
                {canEdit ? (
                  <td className="actions">
                    <button type="button" className="btn link" onClick={() => startEdit(p)}>
                      Edit
                    </button>
                    <button type="button" className="btn link danger" onClick={() => void remove(p.id)}>
                      Delete
                    </button>
                  </td>
                ) : null}
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

      {canEdit ? (
        <div className="card form-card">
          <h2>{editing ? "Edit product" : "Create product"}</h2>
          <form onSubmit={editing ? saveEdit : createProduct}>
            <div className="form-grid">
              <label className="field">
                <span>Name</span>
                <input
                  value={form.name}
                  onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                  required
                />
              </label>
              <label className="field">
                <span>SKU</span>
                <input
                  value={form.sku}
                  onChange={(e) => setForm((f) => ({ ...f, sku: e.target.value }))}
                  required
                />
              </label>
              <label className="field">
                <span>Category</span>
                <input
                  value={form.category}
                  onChange={(e) => setForm((f) => ({ ...f, category: e.target.value }))}
                  required
                />
              </label>
              <label className="field">
                <span>Price</span>
                <input
                  type="number"
                  step="0.01"
                  value={form.price}
                  onChange={(e) => setForm((f) => ({ ...f, price: e.target.value }))}
                  required
                />
              </label>
              <label className="field">
                <span>Low stock threshold</span>
                <input
                  type="number"
                  value={form.lowStockThreshold}
                  onChange={(e) => setForm((f) => ({ ...f, lowStockThreshold: e.target.value }))}
                  required
                />
              </label>
            </div>
            <div className="form-actions">
              <button className="btn primary" type="submit">
                {editing ? "Save changes" : "Create"}
              </button>
              {editing ? (
                <button
                  type="button"
                  className="btn ghost"
                  onClick={() => {
                    setEditing(null);
                    setForm(emptyForm);
                  }}
                >
                  Cancel
                </button>
              ) : null}
            </div>
          </form>
        </div>
      ) : null}
    </div>
  );
}
