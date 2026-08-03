// Sets the browser tab title for the current page.
//
// This is a single-page app, so nothing updates document.title on navigation by
// itself — without this every route shows whatever index.html shipped with.
import { useEffect } from "react";

export const APP_NAME = "ScanStore";

export default function usePageTitle(title) {
  useEffect(() => {
    document.title = title ? `${title} · ${APP_NAME}` : APP_NAME;
  }, [title]);
}
