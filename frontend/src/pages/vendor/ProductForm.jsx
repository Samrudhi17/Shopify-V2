// Add / Edit Product. Fields: name, category, product type, description, brand,
// color (searchable), size, base price, quantity (required), images.
// Persists to Products + Product_Variants + Product_Images + Inventory.
import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import api from "../../services/api";
import { uploadMany } from "../../services/uploads";
import { useAuth } from "../../context/AuthContext";
import { COLORS } from "../../constants/colors";
import { FILE_RULES, formatBytes, validateFile } from "../../constants/validation";

const initial = {
  productName: "", productType: "", description: "", brand: "",
  color: "", size: "", basePrice: "", quantity: "", categoryId: "",
};

// Enough for a few angles of one garment without bloating the catalog page.
const MAX_IMAGES = 6;

export default function ProductForm() {
  const { user } = useAuth();
  const { id } = useParams(); // present => edit mode
  const isEdit = Boolean(id);
  const [form, setForm] = useState(initial);
  const [categories, setCategories] = useState([]);
  // Each entry is { file, url } — url is an object URL used for the thumbnail
  // and revoked when the image is removed, so the blobs don't leak.
  const [images, setImages] = useState([]);
  const [imagesError, setImagesError] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [aiError, setAiError] = useState("");
  const fileInputRef = useRef(null);
  const navigate = useNavigate();

  const set = (k) => (e) => setForm({ ...form, [k]: e.target.value });

  // Ask the API to draft a description from whatever is filled in so far. The
  // result lands in the textarea for the vendor to edit — it is a starting
  // point, not something posted to the catalog unread.
  async function generateDescription() {
    setAiError("");
    setGenerating(true);
    try {
      const { data } = await api.post("/products/generate-description", {
        productName: form.productName,
        productType: form.productType || null,
        brand: form.brand || null,
        color: form.color || null,
        size: form.size || null,
        basePrice: form.basePrice === "" ? null : Number(form.basePrice),
        categoryId: form.categoryId === "" ? null : Number(form.categoryId),
      });
      setForm((prev) => ({ ...prev, description: data.description }));
    } catch (err) {
      setAiError(err?.response?.data?.message || "Could not generate a description.");
    } finally {
      setGenerating(false);
    }
  }

  // Release any remaining preview URLs when the page unmounts. This reads from a
  // ref rather than depending on `images`, because an [images] dependency would
  // re-run the cleanup on every add and revoke URLs that are still on screen.
  const imagesRef = useRef(images);
  imagesRef.current = images;
  useEffect(() => () => { imagesRef.current.forEach((i) => URL.revokeObjectURL(i.url)); }, []);

  // Files are ADDED to the existing selection rather than replacing it, so the
  // vendor can pick a few, then pick a few more. Duplicates (same name + size)
  // and anything past the cap are skipped, with a message saying why.
  function addImages(e) {
    const picked = [...e.target.files];
    // Let the same file be chosen again later if it gets removed first.
    if (fileInputRef.current) fileInputRef.current.value = "";
    if (picked.length === 0) return;

    const problems = [];
    const accepted = [];

    for (const file of picked) {
      const msg = validateFile(file, FILE_RULES.product);
      if (msg) { problems.push(`${file.name}: ${msg}`); continue; }

      const isDuplicate = [...images, ...accepted]
        .some((i) => i.file.name === file.name && i.file.size === file.size);
      if (isDuplicate) { problems.push(`${file.name} is already added.`); continue; }

      if (images.length + accepted.length >= MAX_IMAGES) {
        problems.push(`You can upload up to ${MAX_IMAGES} images.`);
        break;
      }
      accepted.push({ file, url: URL.createObjectURL(file) });
    }

    if (accepted.length) setImages((prev) => [...prev, ...accepted]);
    setImagesError(problems[0] || "");
  }

  function removeImage(index) {
    setImages((prev) => {
      URL.revokeObjectURL(prev[index].url);
      return prev.filter((_, i) => i !== index);
    });
    setImagesError("");
  }

  // The first image is the one shown on cards and the catalog, so "make cover"
  // just moves that image to the front.
  function makeCover(index) {
    setImages((prev) => {
      const next = [...prev];
      const [picked] = next.splice(index, 1);
      return [picked, ...next];
    });
  }

  useEffect(() => {
    if (!user?.id) return;
    api.get(`/categories`).then((r) => setCategories(r.data)).catch(() => {});
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
    if (imagesError) return;
    setSubmitting(true);
    try {
      // Upload in the order shown, so the first thumbnail becomes the cover
      // image (the API marks index 0 as primary).
      const imageUrls = images.length
        ? await uploadMany(images.map((i) => i.file), "products")
        : undefined;
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
            <span style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12 }}>
              <span>Description</span>
              <button
                type="button"
                className="btn btn-outline"
                style={{ padding: "4px 12px", fontSize: 13 }}
                onClick={generateDescription}
                // Needs a name to write about, and the AI call costs money per
                // click, so don't let it fire on an empty form.
                disabled={generating || !form.productName.trim()}
                title={form.productName.trim() ? "Draft a description from the fields above" : "Enter a product name first"}
              >
                {generating ? "Generating…" : "✨ Generate"}
              </button>
            </span>
            <textarea className="input" rows={3} value={form.description} onChange={set("description")} />
            {aiError && <span className="field-error">{aiError}</span>}
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

          <div className="field">
            <span>
              Product Images {isEdit && "(uploading replaces the current set)"}
              {` — up to ${MAX_IMAGES}, max ${formatBytes(FILE_RULES.product.maxBytes)} each`}
            </span>

            {images.length > 0 && (
              <div className="thumb-grid">
                {images.map((img, i) => (
                  <figure className={"thumb" + (i === 0 ? " is-cover" : "")} key={img.url}>
                    <img src={img.url} alt={img.file.name} />
                    <button
                      type="button" className="thumb-remove"
                      onClick={() => removeImage(i)}
                      aria-label={`Remove ${img.file.name}`} title="Remove"
                    >×</button>
                    {i === 0
                      ? <figcaption className="thumb-badge">Cover</figcaption>
                      : (
                        <button type="button" className="thumb-cover-btn" onClick={() => makeCover(i)}>
                          Make cover
                        </button>
                      )}
                  </figure>
                ))}
              </div>
            )}

            {images.length < MAX_IMAGES && (
              <input
                ref={fileInputRef}
                className={"input" + (imagesError ? " is-invalid" : "")}
                type="file" accept={FILE_RULES.product.accept} multiple onChange={addImages}
              />
            )}

            {imagesError
              ? <div className="field-error"><span aria-hidden="true">⚠</span><span>{imagesError}</span></div>
              : images.length > 0
                ? <div className="field-hint">
                    {images.length} of {MAX_IMAGES} added · the first image is the cover shown on your catalog
                  </div>
                : <div className="field-hint">Add photos one by one or several at a time.</div>}
          </div>

          {error && <div className="alert alert-error">{error}</div>}
          <div style={{ display: "flex", gap: 10 }}>
            <button type="submit" className="btn btn-primary" disabled={submitting || Boolean(imagesError)}>
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
