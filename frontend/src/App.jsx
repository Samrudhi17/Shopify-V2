import { BrowserRouter, Routes, Route } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import ProtectedRoute from "./components/ProtectedRoute";

// Public / auth
import Home from "./pages/public/Home";
import Shops from "./pages/public/Shops";
import Login from "./pages/auth/Login";
import Register from "./pages/auth/Register";
import ForgotPassword from "./pages/auth/ForgotPassword";

// Admin
import AdminLayout from "./pages/admin/AdminLayout";
import AdminDashboard from "./pages/admin/AdminDashboard";
import AdminVendors from "./pages/admin/AdminVendors";
import AdminShops from "./pages/admin/AdminShops";
import AdminReports from "./pages/admin/AdminReports";
import AdminSettings from "./pages/admin/AdminSettings";

// Vendor
import VendorLayout from "./pages/vendor/VendorLayout";
import VendorDashboard from "./pages/vendor/VendorDashboard";
import Profile from "./pages/vendor/Profile";
import Categories from "./pages/vendor/Categories";
import Products from "./pages/vendor/Products";
import ProductForm from "./pages/vendor/ProductForm";
import Inventory from "./pages/vendor/Inventory";
import QrCodePage from "./pages/vendor/QrCodePage";
import Reports from "./pages/vendor/Reports";
import Plans from "./pages/vendor/Plans";
import Settings from "./pages/vendor/Settings";

// Public customer catalog
import ShopCatalog from "./pages/shop/ShopCatalog";

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Public site */}
          <Route path="/" element={<Home />} />
          {/* Static path, so it always wins over the /:shopSlug catch-all below. */}
          <Route path="/shops" element={<Shops />} />
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/forgot-password" element={<ForgotPassword />} />

          {/* Admin (nested under sidebar layout) */}
          <Route
            path="/admin"
            element={
              <ProtectedRoute role="Admin">
                <AdminLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<AdminDashboard />} />
            <Route path="vendors" element={<AdminVendors />} />
            <Route path="shops" element={<AdminShops />} />
            <Route path="reports" element={<AdminReports />} />
            <Route path="settings" element={<AdminSettings />} />
          </Route>

          {/* Vendor (nested under sidebar layout) */}
          <Route
            path="/vendor"
            element={
              <ProtectedRoute role="Vendor">
                <VendorLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<VendorDashboard />} />
            <Route path="profile" element={<Profile />} />
            <Route path="categories" element={<Categories />} />
            <Route path="products" element={<Products />} />
            <Route path="products/new" element={<ProductForm />} />
            <Route path="products/:id/edit" element={<ProductForm />} />
            <Route path="inventory" element={<Inventory />} />
            <Route path="qrcode" element={<QrCodePage />} />
            <Route path="reports" element={<Reports />} />
            <Route path="plans" element={<Plans />} />
            <Route path="settings" element={<Settings />} />
          </Route>

          {/* Public customer catalog — only this page is accessible to customers */}
          <Route path="/:shopSlug" element={<ShopCatalog />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
