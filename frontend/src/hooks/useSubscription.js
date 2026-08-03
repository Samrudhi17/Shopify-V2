// The signed-in vendor's current subscription term.
//
// `refresh` is exposed so the Plans page can pull the new expiry straight after
// a payment verifies, instead of making the vendor reload to see what they paid
// for.
import { useCallback, useEffect, useState } from "react";
import api from "../services/api";
import { useAuth } from "../context/AuthContext";

export default function useSubscription() {
  const { user } = useAuth();
  const [subscription, setSubscription] = useState(null);
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    if (user?.role !== "Vendor") {
      setSubscription(null);
      setLoading(false);
      return;
    }

    try {
      const { data } = await api.get("/subscriptions/me");
      setSubscription(data);
    } catch {
      setSubscription(null);
    } finally {
      setLoading(false);
    }
  }, [user]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  return { subscription, loading, refresh };
}
