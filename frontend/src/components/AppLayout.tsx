import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import {
  IconClipboard,
  IconDashboard,
  IconLayers,
  IconPackage,
  IconWarehouse
} from "./icons";

export function AppLayout() {
  const { email, logout } = useAuth();

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <div className="sidebar-brand-mark" aria-hidden>
            IM
          </div>
          <div>
            <div className="sidebar-brand-text">Inventory</div>
            <div className="sidebar-brand-tag">Control hub</div>
          </div>
        </div>
        <nav className="nav">
          <NavLink to="/" end className={({ isActive }) => `nav-link${isActive ? " active" : ""}`}>
            <IconDashboard />
            Dashboard
          </NavLink>
          <NavLink to="/products" className={({ isActive }) => `nav-link${isActive ? " active" : ""}`}>
            <IconPackage />
            Products
          </NavLink>
          <NavLink to="/warehouses" className={({ isActive }) => `nav-link${isActive ? " active" : ""}`}>
            <IconWarehouse />
            Warehouses
          </NavLink>
          <NavLink to="/inventory" className={({ isActive }) => `nav-link${isActive ? " active" : ""}`}>
            <IconLayers />
            Inventory
          </NavLink>
          <NavLink to="/orders" className={({ isActive }) => `nav-link${isActive ? " active" : ""}`}>
            <IconClipboard />
            Orders
          </NavLink>
        </nav>
        <div className="sidebar-footer">
          <div className="sidebar-user small">{email}</div>
          <button type="button" className="btn ghost" onClick={logout}>
            Log out
          </button>
        </div>
      </aside>
      <main className="main">
        <Outlet />
      </main>
    </div>
  );
}
