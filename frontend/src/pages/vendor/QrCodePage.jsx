// QR Code page: shows the shop's catalog URL and its QR image.
import { useEffect, useState } from "react";
import api from "../../services/api";
import { useAuth } from "../../context/AuthContext";
import usePageTitle from "../../hooks/usePageTitle";

export default function QrCodePage() {
  usePageTitle("QR Code");
  const { user } = useAuth();
  const [qr, setQr] = useState(null);
  useEffect(() => {
    if (!user?.id) return;
    api.get("/shops/qrcode").then((r) => setQr(r.data)).catch(() => {});
  }, [user]);

  return (
    <div>
      <h1 style={{ fontSize: 28, marginBottom: 24 }}>QR Code</h1>
      {!qr ? (
        <div className="card" style={{ textAlign: "center", color: "var(--muted)", padding: 48 }}>
          No shop / QR yet. Create your shop in <strong>Profile</strong> to generate one.
        </div>
      ) : (
        <div className="card" style={{ maxWidth: 360, textAlign: "center" }}>
          {qr.qrImagePath && <img src={qr.qrImagePath} alt="Shop QR" width={220} />}
          <p style={{ marginTop: 14 }}><a href={qr.catalogUrl}>{qr.catalogUrl}</a></p>
        </div>
      )}
    </div>
  );
}
