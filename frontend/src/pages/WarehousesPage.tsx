import { useCallback, useEffect, useState } from "react";
import { api, getErrorMessage } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { PageHeader } from "../components/PageHeader";
import type { Paged, Warehouse } from "../types/api";

const emptyForm = { name: "", location: "" };

export function WarehousesPage() {
  const { canEdit } = useAuth();
  const [rows, setRows] = useState<Warehouse[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [editing, setEditing] = useState<Warehouse | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const { data } = await api.get<Paged<Warehouse>>("/api/warehouses", {
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

  async function createWh(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await api.post("/api/warehouses", form);
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
      await api.put(`/api/warehouses/${editing.id}`, {
        name: form.name,
        location: form.location,
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
    if (!confirm("Delete this warehouse?")) return;
    setError(null);
    try {
      await api.delete(`/api/warehouses/${id}`);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  return (
    <div>
      <PageHeader title="Warehouses" description="Sites and zones where inventory is stored." />
      <div className="toolbar">
        <input
          className="search"
          placeholder="Search name or location…"
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
              <th>Location</th>
              {canEdit ? <th /> : null}
            </tr>
          </thead>
          <tbody>
            {rows.map((w) => (
              <tr key={w.id}>
                <td>{w.name}</td>
                <td>{w.location}</td>
                {canEdit ? (
                  <td className="actions">
                    <button
                      type="button"
                      className="btn link"
                      onClick={() => {
                        setEditing(w);
                        setForm({ name: w.name, location: w.location });
                      }}
                    >
                      Edit
                    </button>
                    <button type="button" className="btn link danger" onClick={() => void remove(w.id)}>
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
          <h2>{editing ? "Edit warehouse" : "Create warehouse"}</h2>
          <form onSubmit={editing ? saveEdit : createWh}>
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
                <span>Location</span>
                <input
                  value={form.location}
                  onChange={(e) => setForm((f) => ({ ...f, location: e.target.value }))}
                  required
                />
              </label>
            </div>
            <div className="form-actions">
              <button className="btn primary" type="submit">
                {editing ? "Save" : "Create"}
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
