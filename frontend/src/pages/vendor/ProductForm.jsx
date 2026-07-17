// Add / Edit Product. Fields: name, category, product type, description, brand,
// color (searchable), size, base price, quantity (required), images.
// Persists to Products + Product_Variants + Product_Images + Inventory.
import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import api from "../../services/api";
import { uploadMany } from "../../services/uploads";
import { useAuth } from "../../context/AuthContext";
import { COLORS } from "../../constants/colors";

const initial = {
  productName: "", productType: "", description: "", brand: "",
  color: "", size: "", basePrice: "", quantity: "", categoryId: "",
};

export default function ProductForm() {
  const { user } = useAuth();
  const { id } = useParams(); // present => edit mode
  const isEdit = Boolean(id);
  const [form, setForm] = useState(initial);
  const [categories, setCategories] = useState([]);
  const [images, setImages] = useState([]);
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const navigate = useNavigate();

  const set = (k) => (e) => setForm({ ...form, [k]: e.target.value });

  useEffect(() => {
    if (!user?.id) return;
    api.get(`/categories?vendorId=${user.id}`).then((r) => setCategories(r.data)).catch(() => {});
  }, [user]);

  // Load existing product in edit mode.
  useEffect(() => {
    if (!isEdit) return;
    api.get(`/products/${id}`).then((r) => {
      const p = r.data;
      setForm({
        productName: p.productName || "",
        productType: p.productType || "",
        description: p.description || "",
        brand: p.brand || "",
        color: p.color || "",
        size: p.size || "",
        basePrice: p.basePrice ?? "",
        quantity: p.stockQty ?? "",
        categoryId: p.categoryId ?? "",
      });
    }).catch(() => {});
  }, [id, isEdit]);

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");
    setSubmitting(true);
    try {
      const imageUrls = images.length ? await uploadMany(images, "products") : undefined;
      const payload = {
        categoryId: form.categoryId ? Number(form.categoryId) : null,
        productName: form.productName,
        productType: form.productType,
        description: form.description,
        brand: form.brand,
        color: form.color,
        size: form.size,
        basePrice: Number(form.basePrice),
        quantity: Number(form.quantity),
        imageUrls,
      };
      if (isEdit) {
        await api.put(`/products/${id}`, payload);
      } else {
        await api.post("/products", { vendorId: user?.id, ...payload, imageUrls: imageUrls || [] });
      }
      navigate("/vendor/products");
    } catch (err) {
      setError(err?.response?.data?.message || err.message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div style={{ maxWidth: 640 }}>
      <h1 style={{ fontSize: 28, marginBottom: 24 }}>{isEdit ? "Edit Product" : "Add Product"}</h1>
      <div className="card">
        <form onSubmit={handleSubmit}>
          <F label="Product Name *" value={form.productName} onChange={set("productName")} required />

          <div className="grid" style={{ gridTemplateColumns: "1fr 1fr", gap: 14 }}>
            <label className="field">
              <span>Category</span>
              <select className="input" value={form.categoryId} onChange={set("categoryId")}>
                <option value="">— Select category —</option>
                {categories.map((c) => (
                  <option key={c.categoryId} value={c.categoryId}>{c.categoryName}</option>
                ))}
              </select>
            </label>
            <F label="Product Type (e.g. Saree, Jeans)" value={form.productType} onChange={set("productType")} />
          </div>

          <label className="field">
            <span>Description</span>
            <textarea className="input" rows={3} value={form.description} onChange={set("description")} />
          </label>

          <div className="grid" style={{ gridTemplateColumns: "1fr 1fr", gap: 14 }}>
            <F label="Brand" value={form.brand} onChange={set("brand")} />
            <label className="field">
              <span>Color</span>
              <input
                className="input"
                list="color-options"
                placeholder="Search or type a color…"
                value={form.color}
                onChange={set("color")}
              />
              <datalist id="color-options">
                {COLORS.map((c) => <option key={c} value={c} />)}
              </datalist>
            </label>
          </div>

          <div className="grid" style={{ gridTemplateColumns: "1fr 1fr 1fr", gap: 14 }}>
            <F label="Size" value={form.size} onChange={set("size")} />
            <F label="Base Price *" type="number" min="0" value={form.basePrice} onChange={set("basePrice")} required />
            <F label="Quantity *" type="number" min="0" value={form.quantity} onChange={set("quantity")} required />
          </div>

          <label className="field">
            <span>Product Images {isEdit && "(upload to replace)"}</span>
            <input className="input" type="file" accept="image/*" multiple onChange={(e) => setImages([...e.target.files])} />
          </label>

          {error && <div className="alert alert-error">{error}</div>}
          <div style={{ display: "flex", gap: 10 }}>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              {submitting ? "Saving…" : isEdit ? "Update Product" : "Save Product"}
            </button>
            <button type="button" className="btn btn-ghost" onClick={() => navigate("/vendor/products")}>Cancel</button>
          </div>
        </form>
      </div>
    </div>
  );
}

function F({ label, type = "text", ...props }) {
  return (
    <label className="field">
      <span>{label}</span>
      <input className="input" type={type} {...props} />
    </label>
  );
}
