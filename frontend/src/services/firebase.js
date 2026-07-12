// Firebase configuration.
// Replace these placeholder values with your Firebase project's config
// (Project Settings → General → Your apps → SDK setup and configuration).
import { initializeApp } from "firebase/app";
import { getAuth } from "firebase/auth";
import { getStorage } from "firebase/storage";

const firebaseConfig = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY,
  authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID,
  storageBucket: import.meta.env.VITE_FIREBASE_STORAGE_BUCKET,
  messagingSenderId: import.meta.env.VITE_FIREBASE_MESSAGING_SENDER_ID,
  appId: import.meta.env.VITE_FIREBASE_APP_ID,
};

// Firebase is only usable once the config is filled in (.env). Until then we
// avoid initializing so the rest of the app (Home, routing) still renders
// instead of crashing with "auth/invalid-api-key".
export const isFirebaseConfigured = Boolean(firebaseConfig.apiKey);

let auth = null;
let storage = null;

if (isFirebaseConfigured) {
  const app = initializeApp(firebaseConfig);
  auth = getAuth(app);
  storage = getStorage(app);
} else {
  // eslint-disable-next-line no-console
  console.warn(
    "[Firebase] Not configured. Fill in VITE_FIREBASE_* in frontend/.env to enable Login/Register."
  );
}

export { auth, storage };
