// Loads the Razorpay Checkout script on demand.
//
// It is deliberately not a <script> tag in index.html: every visitor to the
// public catalog would download it, and only vendors on the Plans page ever
// open Checkout.
const SRC = "https://checkout.razorpay.com/v1/checkout.js";

let loader = null;

export function loadRazorpayCheckout() {
  if (window.Razorpay) return Promise.resolve(window.Razorpay);

  // Cached, so two clicks don't inject the script twice.
  loader ??= new Promise((resolve, reject) => {
    const script = document.createElement("script");
    script.src = SRC;
    script.async = true;
    script.onload = () => resolve(window.Razorpay);
    script.onerror = () => {
      // Let a later attempt retry rather than caching the failure forever.
      loader = null;
      reject(new Error("Could not load Razorpay Checkout. Check your connection and try again."));
    };
    document.body.appendChild(script);
  });

  return loader;
}
