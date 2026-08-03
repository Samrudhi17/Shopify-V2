namespace QRShop.API.Services;

// Builds the site's public URLs from AppSettings:PublicBaseUrl (PUBLIC_BASE_URL
// in .env).
//
// Catalog URLs are COMPUTED on read rather than stored, so moving the site from
// localhost to a real domain needs no database changes — only .env. The
// Shops.CatalogUrl / QR_Codes.CatalogUrl columns are still written so existing
// rows and reports stay readable, but nothing reads them back.
public static class PublicUrls
{
    public static string Base(IConfiguration config) =>
        (config["AppSettings:PublicBaseUrl"] ?? "http://localhost").TrimEnd('/');

    public static string Catalog(IConfiguration config, string? slug) =>
        $"{Base(config)}/{slug}";
}
